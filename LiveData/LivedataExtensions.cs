using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveData
{
    public static class LivedataExtensions
    {
        /// <summary>
        /// Convert async function (Task) to an observable LiveData object
        /// </summary>
        /// <typeparam name="T">LiveData type</typeparam>
        /// <param name="task">Task to convert</param>
        /// <returns>LiveData</returns>
        public static LiveData<T> ToLiveData<T>(this Task<T> task)
        {
            LiveData<T> liveData = new LiveData<T>();
            Task.Run(async () => liveData.Value = await task);

            return liveData;
        }


        /// <summary>
        /// Block the thread until the first non-null
        /// value is retrieved from the LiveData.
        /// </summary>
        /// <typeparam name="T">LiveData type</typeparam>
        /// <param name="liveData">LiveData</param>
        /// <param name="timeout">Max waiting time</param>
        /// <returns>LiveData value</returns>
        public static T BlockingGet<T>(this LiveData<T> liveData, int timeout = 5000)
        {
            object value = null;

            var task = Task.Run(async () =>
            {
                while (value == null)
                {
                    await Task.Delay(300);
                    value = liveData.Value;
                }
            });

            task.Wait(timeout);
            return (T) value;
        }

        /// <summary>
        /// Remove an item (and notify the change) from an IList LiveData
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <typeparam name="T">IList item type</typeparam>
        /// <param name="liveData">LiveData</param>
        /// <param name="item">Item to remove</param>
        public static void Remove<Z, T>(this LiveData<Z> liveData, T item) where Z : IList<T>
        {
            liveData.Value?.Remove(item);
            liveData.ForceNotify();
        }

        /// <summary>
        /// Prepend an item (and notify the change) to an IList LiveData
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <typeparam name="T">IList item type</typeparam>
        /// <param name="liveData">LiveData</param>
        /// <param name="item">Item to prepend</param>
        public static void Prepend<T>(this LiveData<IList<T>> liveData, T item)
        {
            liveData.Value?.Insert(0, item);
            liveData.ForceNotify();
        }

        /// <summary>
        /// Append an item (and notify the change) to an IList LiveData
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <typeparam name="T">IList item type</typeparam>
        /// <param name="liveData">LiveData</param>
        /// <param name="item">Item to append</param>
        public static void Append<Z, T>(this LiveData<Z> liveData, T item) where Z : IList<T>
        {
            liveData.Value?.Add(item);
            liveData.ForceNotify();
        }
    }
}
