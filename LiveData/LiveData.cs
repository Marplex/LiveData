using System;
using System.Collections.Generic;
using System.ComponentModel;

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
        /// Used to store every handler listening to Value changes
        /// </summary>
        private readonly List<PropertyChangedEventHandler> emitters = new List<PropertyChangedEventHandler>();

        /// <summary>
        /// Instantiate a LiveData with an initial value
        /// </summary>
        /// <param name="initialValue">Initial value</param>
        public LiveData(T initialValue) => Value = initialValue;
        public LiveData() { }
        public LiveData<T> WithDefaultValue(T value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Notify the value property even if it's not changed
        /// </summary>
        public void ForceNotify()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
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

            //Emit the first time
            res.Invoke(Value);

            return handler;
        }

        /// <summary>
        /// Dispose an <see cref="PropertyChangedEventHandler"/>
        /// </summary>
        /// <param name="disposable">Event to dispose</param>
        public void Dispose(PropertyChangedEventHandler disposable)
        {
            PropertyChanged -= disposable;
            emitters.Remove(disposable);
        }

        /// <summary>
        /// Dispose all attached event emitters (Map, MapAsync and Observe)
        /// </summary>
        public void DisposeAll()
        {
            emitters.ForEach(emitter => PropertyChanged -= emitter);
            emitters.Clear();
        }


        public event PropertyChangedEventHandler PropertyChanged;

    }
}
