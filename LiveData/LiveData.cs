using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace LiveData
{

    /// <summary>
    /// An observable live object that automatically notifies values changes
    /// </summary>
    /// <typeparam name="T">Type of the store value</typeparam>
    public class LiveData<T> : INotifyPropertyChanged
    {

        private T _value;

        /// <summary>
        /// Stored value
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        /// <summary>
        /// Used to store events to invoke when a <see cref="Map{M}(Func{T, M}, bool)"/> function is called
        /// </summary>
        private readonly List<PropertyChangedEventHandler> emitters = new List<PropertyChangedEventHandler>();

        /// <summary>
        /// Instantiate a LiveData with an initial value
        /// </summary>
        /// <param name="initialValue">Initial value</param>
        public LiveData(T initialValue)
        {
            Value = initialValue;
        }

        public LiveData<T> WithDefaultValue(T value)
        {
            Value = value;
            return this;
        }

        public LiveData() { }

        

        /// <summary>
        /// Notify the value property even if it's not changed
        /// </summary>
        public void ForceNotify()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }


        /// <summary>
        /// Transform one LiveData into another while mapping emitted values.
        /// </summary>
        /// <typeparam name="M">LiveData final value type</typeparam>
        /// <param name="mapper">Map function</param>
        /// <param name="emitNullItems">If true, the new LiveData will also receive null values</param>
        /// <returns>Mapped LiveData</returns>
        public LiveData<M> Map<M>(Func<T, M> mapper, bool emitNullItems = false)
        {
            LiveData<M> mapped = new LiveData<M>();
            var emitter = Observe(it =>
            {
                if (!emitNullItems && it == null) return;
                mapped.Value = mapper.Invoke(it);
            });

            emitters.Add(emitter);

            //Notify value the first time
            if (Value != null)
            {
                mapped.Value = mapper.Invoke(Value);
            }

            return mapped;
        }

        /// <summary>
        /// Async version of <see cref="Map{M}(Func{T, M}, bool)"/>
        /// </summary>
        /// <typeparam name="M">LiveData final value type</typeparam>
        /// <param name="mapper">Map function</param>
        /// <param name="emitNullItems">If true, the new LiveData will also receive null values</param>
        /// <returns>Mapped LiveData</returns>
        public LiveData<M> MapAsync<M>(Func<T, Task<M>> mapper, bool emitNullItems = false)
        {
            LiveData<M> mapped = new LiveData<M>();
            var emitter = Observe(async (it) =>
            {
                if (!emitNullItems && it == null) return;
                mapped.Value = await mapper.Invoke(it);
            });

            emitters.Add(emitter);

            //Notify value the first time
            if (Value != null)
            {
                Task.Run(async () => mapped.Value = await mapper.Invoke(Value));
            }

            return mapped;
        }

        /// <summary>
        /// Observe value changes
        /// </summary>
        /// <param name="res">Callback</param>
        /// <returns>EventHandler, can be disposed with <see cref="Dispose(PropertyChangedEventHandler)"/></returns>
        public PropertyChangedEventHandler Observe(Action<T> res)
        {
            void handler(object s, PropertyChangedEventArgs p) => res.Invoke(Value);

            PropertyChanged += handler;
            emitters.Add(handler);

            return handler;
        }

        /// <summary>
        /// Dispose an <see cref="PropertyChangedEventHandler"/>
        /// </summary>
        /// <param name="disposable">Event to dispose</param>
        public void Dispose(PropertyChangedEventHandler disposable)
        {
            PropertyChanged -= disposable;
        }

        /// <summary>
        /// Dispose all attached event emitters (Map, MapAsync and Observe)
        /// </summary>
        public void DisposeAll()
        {
            emitters.ForEach(emitter => PropertyChanged -= emitter);
        }


        public event PropertyChangedEventHandler PropertyChanged;

    }
}
