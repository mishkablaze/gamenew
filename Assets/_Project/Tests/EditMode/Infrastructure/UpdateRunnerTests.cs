using System.Collections.Generic;
using NUnit.Framework;
using Raidline.Infrastructure.Ticking;
using UnityEngine;

namespace Raidline.Tests.EditMode.Infrastructure
{
    public sealed class UpdateRunnerTests
    {
        private sealed class Counter : IFrameUpdatable, ILateFrameUpdatable
        {
            public readonly List<float> Updates = new List<float>();
            public readonly List<float> LateUpdates = new List<float>();
            public System.Action OnUpdate;

            public void FrameUpdate(float deltaTime)
            {
                Updates.Add(deltaTime);
                OnUpdate?.Invoke();
            }

            public void LateFrameUpdate(float deltaTime)
            {
                LateUpdates.Add(deltaTime);
            }
        }

        private GameObject _go;
        private UpdateRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("UpdateRunnerTest");
            _runner = _go.AddComponent<UpdateRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void RunUpdate_Registered_ReceivesDeltaTime()
        {
            var counter = new Counter();
            _runner.Register((IFrameUpdatable)counter);
            _runner.Register((ILateFrameUpdatable)counter);

            _runner.RunUpdate(0.016f);
            _runner.RunLateUpdate(0.02f);

            Assert.That(counter.Updates, Is.EqualTo(new[] { 0.016f }));
            Assert.That(counter.LateUpdates, Is.EqualTo(new[] { 0.02f }));
            Assert.That(_runner.UpdateCount, Is.EqualTo(1));
            Assert.That(_runner.LateUpdateCount, Is.EqualTo(1));
        }

        [Test]
        public void Unregister_DuringUpdate_LaterOneSkipped()
        {
            var first = new Counter();
            var second = new Counter();
            first.OnUpdate = () => _runner.Unregister((IFrameUpdatable)second);
            _runner.Register((IFrameUpdatable)first);
            _runner.Register((IFrameUpdatable)second);

            _runner.RunUpdate(0.01f);

            Assert.That(first.Updates.Count, Is.EqualTo(1));
            Assert.That(second.Updates, Is.Empty);
        }

        [Test]
        public void Register_Duplicate_ReturnsFalse()
        {
            var counter = new Counter();

            Assert.That(_runner.Register((IFrameUpdatable)counter), Is.True);
            Assert.That(_runner.Register((IFrameUpdatable)counter), Is.False);
        }
    }
}
