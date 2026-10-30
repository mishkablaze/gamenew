using System;
using System.Collections.Generic;

namespace Raidline.Core.Common
{
    /// <summary>
    /// Ordered list of listeners/systems that can be modified while it is being iterated,
    /// without allocations in steady state.
    /// <para>Rules while iterating (between <see cref="BeginIteration"/> and <see cref="EndIteration"/>):</para>
    /// <list type="bullet">
    /// <item>removed items are skipped for the rest of the pass (their slot reads as null);</item>
    /// <item>added items are not visited in the current pass; they are appended in <see cref="EndIteration"/>.</item>
    /// </list>
    /// Iterations may nest (e.g. an event published from inside a handler of the same channel).
    /// Usage: <c>int n = list.BeginIteration(); try { for (i &lt; n) { var x = list[i]; if (x != null) ... } } finally { list.EndIteration(); }</c>
    /// </summary>
    public sealed class SafeIterationList<T> where T : class
    {
        private T[] _items;
        private int _count;
        private int _iterationDepth;
        private bool _hasHoles;
        private readonly List<T> _pendingAdds = new List<T>(4);

        public SafeIterationList(int initialCapacity = 8)
        {
            _items = new T[Math.Max(1, initialCapacity)];
        }

        /// <summary>Number of live items, including ones waiting to be appended.</summary>
        public int Count
        {
            get
            {
                int live = 0;
                for (int i = 0; i < _count; i++)
                {
                    if (_items[i] != null)
                    {
                        live++;
                    }
                }

                return live + _pendingAdds.Count;
            }
        }

        /// <summary>True while at least one iteration is in progress.</summary>
        public bool IsIterating => _iterationDepth > 0;

        /// <summary>Item at a slot of the current pass; null if it was removed during the pass.</summary>
        public T this[int index] => _items[index];

        /// <summary>Adds an item once. Returns false if it is already present.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        public bool Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (Contains(item))
            {
                return false;
            }

            if (_iterationDepth > 0)
            {
                _pendingAdds.Add(item);
                return true;
            }

            Append(item);
            return true;
        }

        /// <summary>Removes an item. Returns false if it was not present.</summary>
        public bool Remove(T item)
        {
            if (item == null)
            {
                return false;
            }

            int pending = _pendingAdds.IndexOf(item);
            if (pending >= 0)
            {
                _pendingAdds.RemoveAt(pending);
                return true;
            }

            int index = IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            if (_iterationDepth > 0)
            {
                _items[index] = null;
                _hasHoles = true;
                return true;
            }

            Array.Copy(_items, index + 1, _items, index, _count - index - 1);
            _count--;
            _items[_count] = null;
            return true;
        }

        /// <summary>True if the item is present (or waiting to be appended).</summary>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0 || _pendingAdds.Contains(item);
        }

        /// <summary>Removes everything. During iteration the remaining slots read as null.</summary>
        public void Clear()
        {
            _pendingAdds.Clear();
            if (_iterationDepth > 0)
            {
                for (int i = 0; i < _count; i++)
                {
                    _items[i] = null;
                }

                _hasHoles = true;
                return;
            }

            Array.Clear(_items, 0, _count);
            _count = 0;
            _hasHoles = false;
        }

        /// <summary>Starts a pass. Returns the number of slots to visit.</summary>
        public int BeginIteration()
        {
            _iterationDepth++;
            return _count;
        }

        /// <summary>Ends a pass; the outermost pass compacts removed slots and appends pending items.</summary>
        /// <exception cref="InvalidOperationException">No pass is in progress.</exception>
        public void EndIteration()
        {
            if (_iterationDepth == 0)
            {
                throw new InvalidOperationException("EndIteration without BeginIteration.");
            }

            _iterationDepth--;
            if (_iterationDepth > 0)
            {
                return;
            }

            if (_hasHoles)
            {
                Compact();
            }

            if (_pendingAdds.Count > 0)
            {
                for (int i = 0; i < _pendingAdds.Count; i++)
                {
                    Append(_pendingAdds[i]);
                }

                _pendingAdds.Clear();
            }
        }

        private int IndexOf(T item)
        {
            for (int i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_items[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Append(T item)
        {
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[_count] = item;
            _count++;
        }

        private void Compact()
        {
            int write = 0;
            for (int read = 0; read < _count; read++)
            {
                T item = _items[read];
                if (item != null)
                {
                    _items[write] = item;
                    write++;
                }
            }

            Array.Clear(_items, write, _count - write);
            _count = write;
            _hasHoles = false;
        }
    }
}
