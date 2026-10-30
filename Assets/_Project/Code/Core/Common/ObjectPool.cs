using System;
using System.Collections.Generic;

namespace Raidline.Core.Common
{
    /// <summary>
    /// Pool of reusable objects to avoid allocations in hot paths (CONVENTIONS §4).
    /// For Unity components use Raidline.Infrastructure.Pooling.ComponentPool.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _inactive;
        private readonly Func<T> _create;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        /// <param name="create">Creates a new instance when the pool is empty.</param>
        /// <param name="onGet">Optional: called on every item handed out.</param>
        /// <param name="onRelease">Optional: called on every item returned (reset state here).</param>
        /// <param name="initialCapacity">Initial size of the inactive stack.</param>
        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null, int initialCapacity = 16)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onGet = onGet;
            _onRelease = onRelease;
            _inactive = new Stack<T>(Math.Max(0, initialCapacity));
        }

        /// <summary>Items waiting in the pool.</summary>
        public int CountInactive => _inactive.Count;

        /// <summary>Items ever created by this pool.</summary>
        public int CountAll { get; private set; }

        /// <summary>Items currently handed out.</summary>
        public int CountActive => CountAll - _inactive.Count;

        /// <summary>Creates items up front so that later <see cref="Get"/> calls don't allocate.</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T item = CreateItem();
                _onRelease?.Invoke(item);
                _inactive.Push(item);
            }
        }

        /// <summary>Takes an item from the pool, creating one if it is empty.</summary>
        public T Get()
        {
            T item = _inactive.Count > 0 ? _inactive.Pop() : CreateItem();
            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>Returns an item to the pool.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Debug builds: the item is already in the pool.</exception>
        public void Release(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
#if DEBUG
            if (_inactive.Contains(item))
            {
                throw new InvalidOperationException("Item released to the pool twice.");
            }
#endif
            _onRelease?.Invoke(item);
            _inactive.Push(item);
        }

        /// <summary>Drops all inactive items (handed-out items are not tracked).</summary>
        /// <param name="onDiscard">Optional: called for each dropped item (e.g. destroy its GameObject).</param>
        public void Clear(Action<T> onDiscard = null)
        {
            if (onDiscard != null)
            {
                foreach (T item in _inactive)
                {
                    onDiscard(item);
                }
            }

            CountAll -= _inactive.Count;
            _inactive.Clear();
        }

        private T CreateItem()
        {
            T item = _create();
            if (item == null)
            {
                throw new InvalidOperationException("Pool factory returned null.");
            }

            CountAll++;
            return item;
        }
    }
}
