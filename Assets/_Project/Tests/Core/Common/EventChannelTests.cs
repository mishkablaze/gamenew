using System;
using System.Collections.Generic;
using NUnit.Framework;
using Raidline.Core.Common;
#if UNITY_5_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
#endif

namespace Raidline.Core.Tests.Common
{
    public sealed class EventChannelTests
    {
        private struct TestEvent
        {
            public int Value;

            public TestEvent(int value)
            {
                Value = value;
            }
        }

        [Test]
        public void Publish_SeveralListeners_CalledInSubscriptionOrder()
        {
            var channel = new EventChannel<TestEvent>();
            var calls = new List<string>();
            channel.Subscribe((in TestEvent e) => calls.Add("a" + e.Value));
            channel.Subscribe((in TestEvent e) => calls.Add("b" + e.Value));

            channel.Publish(new TestEvent(7));

            Assert.That(calls, Is.EqualTo(new[] { "a7", "b7" }));
        }

        [Test]
        public void Subscribe_SameListenerTwice_SecondReturnsFalse()
        {
            var channel = new EventChannel<TestEvent>();
            int calls = 0;
            EventListener<TestEvent> listener = (in TestEvent e) => calls++;

            Assert.That(channel.Subscribe(listener), Is.True);
            Assert.That(channel.Subscribe(listener), Is.False);
            channel.Publish(default);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(channel.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void Unsubscribe_Listener_StopsReceiving()
        {
            var channel = new EventChannel<TestEvent>();
            int calls = 0;
            EventListener<TestEvent> listener = (in TestEvent e) => calls++;
            channel.Subscribe(listener);

            Assert.That(channel.Unsubscribe(listener), Is.True);
            Assert.That(channel.Unsubscribe(listener), Is.False);
            channel.Publish(default);

            Assert.That(calls, Is.EqualTo(0));
        }

        [Test]
        public void Publish_ListenerUnsubscribesLaterOne_LaterOneNotCalled()
        {
            var channel = new EventChannel<TestEvent>();
            int laterCalls = 0;
            EventListener<TestEvent> later = (in TestEvent e) => laterCalls++;
            channel.Subscribe((in TestEvent e) => channel.Unsubscribe(later));
            channel.Subscribe(later);

            channel.Publish(default);

            Assert.That(laterCalls, Is.EqualTo(0));
            Assert.That(channel.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_ListenerSubscribesAnother_NewOneCalledFromNextPublish()
        {
            var channel = new EventChannel<TestEvent>();
            int newCalls = 0;
            EventListener<TestEvent> added = (in TestEvent e) => newCalls++;
            channel.Subscribe((in TestEvent e) => channel.Subscribe(added));

            channel.Publish(default);
            Assert.That(newCalls, Is.EqualTo(0));

            channel.Publish(default);
            Assert.That(newCalls, Is.EqualTo(1));
        }

        [Test]
        public void Publish_NestedPublish_DeliversBothEvents()
        {
            var channel = new EventChannel<TestEvent>();
            var values = new List<int>();
            channel.Subscribe((in TestEvent e) =>
            {
                values.Add(e.Value);
                if (e.Value == 1)
                {
                    channel.Publish(new TestEvent(2));
                }
            });

            channel.Publish(new TestEvent(1));

            Assert.That(values, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Publish_ListenerThrows_ExceptionPropagatesAndChannelStillWorks()
        {
            var channel = new EventChannel<TestEvent>();
            bool shouldThrow = true;
            int calls = 0;
            channel.Subscribe((in TestEvent e) =>
            {
                if (shouldThrow)
                {
                    throw new InvalidOperationException("boom");
                }
            });
            channel.Subscribe((in TestEvent e) => calls++);

            Assert.Throws<InvalidOperationException>(() => channel.Publish(default));
            shouldThrow = false;
            channel.Publish(default);

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Clear_WithListeners_RemovesAll()
        {
            var channel = new EventChannel<TestEvent>();
            int calls = 0;
            channel.Subscribe((in TestEvent e) => calls++);

            channel.Clear();
            channel.Publish(default);

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(channel.ListenerCount, Is.EqualTo(0));
        }

#if UNITY_5_3_OR_NEWER
        private static int s_sink;

        private static void CountingListener(in TestEvent e)
        {
            s_sink += e.Value;
        }

        [Test]
        public void Publish_SteadyState_DoesNotAllocate()
        {
            var channel = new EventChannel<TestEvent>();
            channel.Subscribe(CountingListener);
            var evt = new TestEvent(1);
            channel.Publish(in evt); // warm up

            Assert.That(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    channel.Publish(in evt);
                }
            }, Is.Not.AllocatingGCMemory());
        }
#endif
    }
}
