using System;
using NUnit.Framework;
using Raidline.Core.Common;

namespace Raidline.Core.Tests.Common
{
    public sealed class ObjectPoolTests
    {
        private sealed class Thing
        {
            public bool Active;
        }

        [Test]
        public void Get_EmptyPool_CreatesNew()
        {
            var pool = new ObjectPool<Thing>(() => new Thing());

            Thing item = pool.Get();

            Assert.That(item, Is.Not.Null);
            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountActive, Is.EqualTo(1));
        }

        [Test]
        public void Release_ThenGet_ReusesSameInstance()
        {
            var pool = new ObjectPool<Thing>(() => new Thing());
            Thing first = pool.Get();

            pool.Release(first);
            Thing second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(pool.CountAll, Is.EqualTo(1));
        }

        [Test]
        public void Prewarm_CreatesInactiveItems()
        {
            int created = 0;
            var pool = new ObjectPool<Thing>(() =>
            {
                created++;
                return new Thing();
            });

            pool.Prewarm(5);
            for (int i = 0; i < 5; i++)
            {
                pool.Get();
            }

            Assert.That(created, Is.EqualTo(5));
            Assert.That(pool.CountInactive, Is.EqualTo(0));
            Assert.That(pool.CountActive, Is.EqualTo(5));
        }

        [Test]
        public void Callbacks_GetAndRelease_AreInvoked()
        {
            var pool = new ObjectPool<Thing>(
                () => new Thing(),
                onGet: t => t.Active = true,
                onRelease: t => t.Active = false);

            Thing item = pool.Get();
            Assert.That(item.Active, Is.True);

            pool.Release(item);
            Assert.That(item.Active, Is.False);
        }

        [Test]
        public void Release_Null_Throws()
        {
            var pool = new ObjectPool<Thing>(() => new Thing());

            Assert.Throws<ArgumentNullException>(() => pool.Release(null));
        }

#if DEBUG
        [Test]
        public void Release_Twice_ThrowsInDebug()
        {
            var pool = new ObjectPool<Thing>(() => new Thing());
            Thing item = pool.Get();
            pool.Release(item);

            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
        }
#endif

        [Test]
        public void Get_FactoryReturnsNull_Throws()
        {
            var pool = new ObjectPool<Thing>(() => null);

            Assert.Throws<InvalidOperationException>(() => pool.Get());
        }

        [Test]
        public void Constructor_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<Thing>(null));
        }

        [Test]
        public void Clear_DropsInactiveItems()
        {
            var pool = new ObjectPool<Thing>(() => new Thing());
            pool.Prewarm(3);
            Thing active = pool.Get();

            pool.Clear();

            Assert.That(pool.CountInactive, Is.EqualTo(0));
            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(active, Is.Not.Null);
        }

        [Test]
        public void Clear_WithDiscardCallback_CalledForEachInactive()
        {
            var pool = new ObjectPool<Thing>(() => new Thing());
            pool.Prewarm(4);
            int discarded = 0;

            pool.Clear(t => discarded++);

            Assert.That(discarded, Is.EqualTo(4));
        }
    }
}
