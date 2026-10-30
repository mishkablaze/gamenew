using System;
using System.Collections.Generic;
using NUnit.Framework;
using Raidline.Core.Common;

namespace Raidline.Core.Tests.Common
{
    public sealed class SafeIterationListTests
    {
        private sealed class Item
        {
            public readonly string Name;

            public Item(string name)
            {
                Name = name;
            }
        }

        private static List<string> Visit(SafeIterationList<Item> list, Action<Item> onVisit = null)
        {
            var visited = new List<string>();
            int count = list.BeginIteration();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Item item = list[i];
                    if (item == null)
                    {
                        continue;
                    }

                    visited.Add(item.Name);
                    onVisit?.Invoke(item);
                }
            }
            finally
            {
                list.EndIteration();
            }

            return visited;
        }

        [Test]
        public void AddRemove_OutsideIteration_KeepsOrder()
        {
            var list = new SafeIterationList<Item>(1);
            var a = new Item("a");
            var b = new Item("b");
            var c = new Item("c");
            list.Add(a);
            list.Add(b);
            list.Add(c);

            list.Remove(b);

            Assert.That(Visit(list), Is.EqualTo(new[] { "a", "c" }));
            Assert.That(list.Count, Is.EqualTo(2));
        }

        [Test]
        public void Add_Duplicate_ReturnsFalse()
        {
            var list = new SafeIterationList<Item>();
            var a = new Item("a");

            Assert.That(list.Add(a), Is.True);
            Assert.That(list.Add(a), Is.False);
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_Null_Throws()
        {
            var list = new SafeIterationList<Item>();

            Assert.Throws<ArgumentNullException>(() => list.Add(null));
        }

        [Test]
        public void Remove_Missing_ReturnsFalse()
        {
            var list = new SafeIterationList<Item>();

            Assert.That(list.Remove(new Item("x")), Is.False);
            Assert.That(list.Remove(null), Is.False);
        }

        [Test]
        public void Remove_DuringIteration_LaterItemSkippedThenCompacted()
        {
            var list = new SafeIterationList<Item>();
            var a = new Item("a");
            var b = new Item("b");
            var c = new Item("c");
            list.Add(a);
            list.Add(b);
            list.Add(c);

            List<string> visited = Visit(list, item =>
            {
                if (item == a)
                {
                    list.Remove(b);
                }
            });

            Assert.That(visited, Is.EqualTo(new[] { "a", "c" }));
            Assert.That(Visit(list), Is.EqualTo(new[] { "a", "c" }));
        }

        [Test]
        public void Add_DuringIteration_AppendedAfterPass()
        {
            var list = new SafeIterationList<Item>();
            var a = new Item("a");
            var d = new Item("d");
            list.Add(a);

            List<string> visited = Visit(list, item => list.Add(d));

            Assert.That(visited, Is.EqualTo(new[] { "a" }));
            Assert.That(list.Contains(d), Is.True);
            Assert.That(Visit(list), Is.EqualTo(new[] { "a", "d" }));
        }

        [Test]
        public void AddThenRemove_DuringIteration_NeverAppended()
        {
            var list = new SafeIterationList<Item>();
            var a = new Item("a");
            var d = new Item("d");
            list.Add(a);

            Visit(list, item =>
            {
                list.Add(d);
                list.Remove(d);
            });

            Assert.That(Visit(list), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void RemoveThenAdd_DuringIteration_MovesToEnd()
        {
            var list = new SafeIterationList<Item>();
            var a = new Item("a");
            var b = new Item("b");
            list.Add(a);
            list.Add(b);

            Visit(list, item =>
            {
                if (item == a)
                {
                    list.Remove(a);
                    list.Add(a);
                }
            });

            Assert.That(Visit(list), Is.EqualTo(new[] { "b", "a" }));
        }

        [Test]
        public void NestedIteration_CompactsOnlyAfterOutermost()
        {
            var list = new SafeIterationList<Item>();
            var a = new Item("a");
            var b = new Item("b");
            list.Add(a);
            list.Add(b);

            int outer = list.BeginIteration();
            int inner = list.BeginIteration();
            list.Remove(a);
            list.EndIteration();

            Assert.That(list[0], Is.Null, "slot stays empty until the outer pass ends");
            list.EndIteration();

            Assert.That(outer, Is.EqualTo(2));
            Assert.That(inner, Is.EqualTo(2));
            Assert.That(Visit(list), Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void Clear_DuringIteration_EmptiesAfterPass()
        {
            var list = new SafeIterationList<Item>();
            list.Add(new Item("a"));
            list.Add(new Item("b"));

            List<string> visited = Visit(list, item => list.Clear());

            Assert.That(visited, Is.EqualTo(new[] { "a" }));
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void EndIteration_WithoutBegin_Throws()
        {
            var list = new SafeIterationList<Item>();

            Assert.Throws<InvalidOperationException>(() => list.EndIteration());
        }

        [Test]
        public void Add_ManyItems_GrowsCapacity()
        {
            var list = new SafeIterationList<Item>(1);
            for (int i = 0; i < 100; i++)
            {
                list.Add(new Item(i.ToString()));
            }

            Assert.That(list.Count, Is.EqualTo(100));
            Assert.That(Visit(list).Count, Is.EqualTo(100));
        }
    }
}
