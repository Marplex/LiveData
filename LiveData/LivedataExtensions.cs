using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace LiveData
{
    public static class LivedataExtensions
    {
        #region Converters
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
        #endregion

        #region Transformations

        /// <summary>
        /// Combine two LiveData
        /// </summary>
        /// <typeparam name="T">Type of LiveData</typeparam>
        /// <param name="first"></param>
        /// <param name="second">LiveData to combine with</param>
        /// <param name="mapper">Tells how to merge the two LiveData</param>
        /// <param name="emitNullValues">If true, also emit null values on the resulting LiveData</param>
        /// <returns></returns>
        public static LiveData<F> CombineWith<T, M, F>(this LiveData<T> first, LiveData<M> second, Func<T, M, F> mapper, bool emitNullValues = false)
        {
            LiveData<F> combined = new LiveData<F>();

            //Listen to first
            first.Observe(it =>
            {
                if (!emitNullValues && (it == null || second.Value == null)) return;
                combined.Value = mapper(it, second.Value);
            });

            //Listen to second
            second.Observe(it =>
            {
                if (!emitNullValues && (it == null || first.Value == null)) return;
                combined.Value = mapper(first.Value, it);
            });

            return combined;
        }

        /// <summary>
        /// Debounce LiveData emitted values
        /// </summary>
        /// <typeparam name="T">Source LiveData value type</typeparam>
        /// <param name="liveData"></param>
        /// <param name="timeInMillis">Debounce time</param>
        /// <param name="emitNullValues">If true, the new LiveData will receive null values</param>
        /// <returns></returns>
        public static LiveData<T> Debounce<T>(this LiveData<T> liveData, int timeInMillis, bool emitNullValues = false)
        {
            LiveData<T> debounced = new LiveData<T>();
            CancellationTokenSource cancelTokenSource = null;

            liveData.Observe(async it =>
            {
                if (!emitNullValues && it == null) return;

                cancelTokenSource.Cancel();
                cancelTokenSource = new CancellationTokenSource();

                await Task.Delay(timeInMillis, cancelTokenSource.Token)
                    .ContinueWith(t => {
                        if (t.IsCompleted && !t.IsCanceled) debounced.Value = it;
                    });
            });

            return debounced;
        }

        /// <summary>
        /// Delay LiveData emitted values
        /// </summary>
        /// <typeparam name="T">Source LiveData value type</typeparam>
        /// <param name="liveData"></param>
        /// <param name="timeInMillis">Delay time</param>
        /// <param name="emitNullValues">If true, the new LiveData will receive null values</param>
        /// <returns></returns>
        public static LiveData<T> Delay<T>(
            this LiveData<T> liveData,
            int timeInMillis,
            bool emitNullValues = false)
        {

            LiveData<T> delayed = new LiveData<T>();

            liveData.Observe(async it =>
            {
                if (!emitNullValues && it == null) return;

                await Task.Delay(timeInMillis);
                delayed.Value = it;
            });

            return delayed;
        }

        /// <summary>
        /// Map one livedata to another livedata
        /// </summary>
        /// <typeparam name="M">LiveData final value type</typeparam>
        /// <typeparam name="T">Source LiveData value type</typeparam>
        /// <param name="liveData"></param>
        /// <param name="mapper">Map source livedata to destination livedata</param>
        /// <param name="emitNullValues">If true, the new LiveData will receive null values</param>
        /// <returns></returns>
        public static LiveData<M> SwitchMap<M, T>(
            this LiveData<T> liveData,
            Func<T, LiveData<M>> mapper,
            bool emitNullValues = false)
        {

            LiveData<M> mapped = new LiveData<M>();

            LiveData<M> mappedLiveData = null;
            PropertyChangedEventHandler mappedHandler = null;

            liveData.Observe(it =>
            {
                if (!emitNullValues && it == null) return;
                if (mappedHandler != null && mappedLiveData != null) mappedLiveData.Dispose(mappedHandler);

                mappedLiveData = mapper.Invoke(it);
                mapped.Value = mappedLiveData.Value;
                mappedHandler = mappedLiveData.Observe(x => mapped.Value = x);
            });

            return mapped;
        }

        /// <summary>
        /// Transform one LiveData into another while mapping emitted values.
        /// </summary>
        /// <typeparam name="M">LiveData final value type</typeparam>
        /// <typeparam name="T">Source LiveData value type</typeparam>
        /// <param name="liveData"></param>
        /// <param name="mapper">Map source livedata values to destination types</param>
        /// <param name="emitNullValues">If true, the new LiveData will receive null values</param>
        /// <returns></returns>
        public static LiveData<M> Map<M, T>(
            this LiveData<T> liveData,
            Func<T, M> mapper,
            bool emitNullValues = false)
        {

            LiveData<M> mapped = new LiveData<M>();

            liveData.Observe(it =>
            {
                if (!emitNullValues && it == null) return;
                mapped.Value = mapper.Invoke(it);
            });

            return mapped;
        }


        /// <summary>
        /// Async version of <see cref="Map{M, T}(LiveData{T}, Func{T, M}, bool)"/> 
        /// </summary>
        /// <typeparam name="M">LiveData final value type</typeparam>
        /// <typeparam name="T">Source LiveData value type</typeparam>
        /// <param name="liveData"></param>
        /// <param name="mapper">Map source livedata values to destination types</param>
        /// <param name="emitNullValues">If true, the new LiveData will receive null values</param>
        /// <returns></returns>
        public static LiveData<M> MapAsync<M, T>(
            this LiveData<T> liveData,
            Func<T, Task<M>> mapper,
            bool emitNullValues = false)
        {

            LiveData<M> mapped = new LiveData<M>();

            liveData.Observe(async (it) =>
            {
                if (!emitNullValues && it == null) return;
                mapped.Value = await mapper.Invoke(it);
            });

            return mapped;
        }

        #endregion

        #region Utils
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
        #endregion

        #region List utils
        /// <summary>
        /// Remove an item (and notify the change) from an IList LiveData
        /// </summary>
        /// <typeparam name="Z"></typeparam>
        /// <typeparam name="T">IList item type</typeparam>
        /// <param name="liveData">LiveData</param>
        /// <param name="item">Item to remove</param>
        public static void Remove<Z, T>(this LiveData<Z> liveData, T item) where Z : IList<T>
        {
            liveData.Value.Remove(item);
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
            liveData.Value.Insert(0, item);
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
            liveData.Value.Add(item);
            liveData.ForceNotify();
        }
        #endregion
    }
}
