namespace Raidline.Core.Common
{
    /// <summary>
    /// Typed event channel for struct events: no boxing and no allocations when publishing.
    /// Listeners may subscribe or unsubscribe from inside a handler (see <see cref="SafeIterationList{T}"/>):
    /// a listener removed during a publish is not called later in that publish,
    /// a listener added during a publish starts receiving events from the next publish.
    /// Exceptions thrown by a listener propagate to the publisher; remaining listeners of that publish are skipped.
    /// </summary>
    public sealed class EventChannel<T> where T : struct
    {
        private readonly SafeIterationList<EventListener<T>> _listeners;

        public EventChannel(int initialCapacity = 4)
        {
            _listeners = new SafeIterationList<EventListener<T>>(initialCapacity);
        }

        /// <summary>Number of subscribed listeners.</summary>
        public int ListenerCount => _listeners.Count;

        /// <summary>Subscribes a listener once. Returns false if it was already subscribed.</summary>
        public bool Subscribe(EventListener<T> listener) => _listeners.Add(listener);

        /// <summary>Unsubscribes a listener. Returns false if it was not subscribed.</summary>
        public bool Unsubscribe(EventListener<T> listener) => _listeners.Remove(listener);

        /// <summary>Calls every listener in subscription order.</summary>
        public void Publish(in T evt)
        {
            int count = _listeners.BeginIteration();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    EventListener<T> listener = _listeners[i];
                    if (listener != null)
                    {
                        listener(in evt);
                    }
                }
            }
            finally
            {
                _listeners.EndIteration();
            }
        }

        /// <summary>Removes all listeners (end of raid).</summary>
        public void Clear() => _listeners.Clear();
    }
}
