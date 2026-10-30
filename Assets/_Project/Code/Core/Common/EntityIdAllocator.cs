using System;

namespace Raidline.Core.Common
{
    /// <summary>
    /// Hands out unique, increasing <see cref="EntityId"/> values starting at 1.
    /// One allocator per raid, owned by the server.
    /// </summary>
    public sealed class EntityIdAllocator
    {
        private int _last;

        /// <summary>Number of ids handed out so far.</summary>
        public int Count => _last;

        /// <summary>Returns the next unused id.</summary>
        /// <exception cref="InvalidOperationException">All positive int values are used.</exception>
        public EntityId Next()
        {
            if (_last == int.MaxValue)
            {
                throw new InvalidOperationException("EntityId space exhausted.");
            }

            _last++;
            return new EntityId(_last);
        }

        /// <summary>Forgets all handed-out ids (start of a new raid).</summary>
        public void Reset()
        {
            _last = 0;
        }
    }
}
