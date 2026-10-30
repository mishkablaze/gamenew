using System;
using Raidline.Core.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Raidline.Infrastructure.Pooling
{
    /// <summary>
    /// Pool of prefab instances (effects, decals, audio sources, UI elements).
    /// Items handed out are active; returned items are deactivated and parented under <see cref="Root"/>.
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly ObjectPool<T> _pool;
        private readonly T _prefab;

        /// <param name="prefab">Prefab component to clone.</param>
        /// <param name="root">Parent for inactive items (created by the caller, e.g. under the AppRoot).</param>
        /// <param name="initialCapacity">Initial capacity of the inactive stack.</param>
        public ComponentPool(T prefab, Transform root, int initialCapacity = 16)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;
            Root = root;
            _pool = new ObjectPool<T>(Create, OnGet, OnRelease, initialCapacity);
        }

        /// <summary>Parent of inactive items.</summary>
        public Transform Root { get; }

        public int CountActive => _pool.CountActive;

        public int CountInactive => _pool.CountInactive;

        /// <summary>Instantiates items up front (do this during loading, not in gameplay).</summary>
        public void Prewarm(int count) => _pool.Prewarm(count);

        /// <summary>Takes an active instance from the pool.</summary>
        public T Get() => _pool.Get();

        /// <summary>Returns an instance; it is deactivated and moved under <see cref="Root"/>.</summary>
        public void Release(T item) => _pool.Release(item);

        /// <summary>Destroys inactive instances (scene unload).</summary>
        public void DestroyInactive()
        {
            _pool.Clear(DestroyItem);
        }

        private static void DestroyItem(T item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(item.gameObject);
            }
            else
            {
                Object.DestroyImmediate(item.gameObject);
            }
        }

        private T Create()
        {
            T instance = Object.Instantiate(_prefab, Root);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private static void OnGet(T item)
        {
            item.gameObject.SetActive(true);
        }

        private void OnRelease(T item)
        {
            item.gameObject.SetActive(false);
            if (item.transform.parent != Root)
            {
                item.transform.SetParent(Root, false);
            }
        }
    }
}
