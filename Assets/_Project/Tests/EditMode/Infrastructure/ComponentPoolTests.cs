using NUnit.Framework;
using Raidline.Infrastructure.Pooling;
using UnityEngine;

namespace Raidline.Tests.EditMode.Infrastructure
{
    public sealed class ComponentPoolTests
    {
        private GameObject _prefab;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("PooledPrefab");
            _prefab.AddComponent<BoxCollider>();
            _root = new GameObject("PoolRoot");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_prefab);
        }

        [Test]
        public void Get_ReturnsActiveInstanceUnderRoot()
        {
            var pool = new ComponentPool<BoxCollider>(_prefab.GetComponent<BoxCollider>(), _root.transform);

            BoxCollider item = pool.Get();

            Assert.That(item.gameObject.activeSelf, Is.True);
            Assert.That(item.transform.parent, Is.EqualTo(_root.transform));
            Assert.That(item.gameObject, Is.Not.SameAs(_prefab));
        }

        [Test]
        public void Release_DeactivatesAndReparents()
        {
            var pool = new ComponentPool<BoxCollider>(_prefab.GetComponent<BoxCollider>(), _root.transform);
            BoxCollider item = pool.Get();
            item.transform.SetParent(null);

            pool.Release(item);

            Assert.That(item.gameObject.activeSelf, Is.False);
            Assert.That(item.transform.parent, Is.EqualTo(_root.transform));
            Assert.That(pool.Get(), Is.SameAs(item));
        }

        [Test]
        public void DestroyInactive_RemovesPooledObjects()
        {
            var pool = new ComponentPool<BoxCollider>(_prefab.GetComponent<BoxCollider>(), _root.transform);
            pool.Prewarm(3);
            Assert.That(_root.transform.childCount, Is.EqualTo(3));

            pool.DestroyInactive();

            Assert.That(_root.transform.childCount, Is.EqualTo(0));
            Assert.That(pool.CountInactive, Is.EqualTo(0));
        }
    }
}
