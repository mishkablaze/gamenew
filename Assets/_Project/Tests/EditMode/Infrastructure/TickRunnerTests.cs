using System;
using System.Collections.Generic;
using NUnit.Framework;
using Raidline.Infrastructure.Ticking;

namespace Raidline.Tests.EditMode.Infrastructure
{
    public sealed class TickRunnerTests
    {
        private sealed class Recorder : ITickable
        {
            private readonly string _name;
            private readonly List<string> _log;
            public Action<TickContext> OnTick;

            public Recorder(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Tick(in TickContext ctx)
            {
                _log.Add(_name + ":" + ctx.Tick);
                OnTick?.Invoke(ctx);
            }
        }

        [Test]
        public void Step_AllPhases_RunInOrderAroundPhysics()
        {
            var log = new List<string>();
            var source = new LocalTickSource(physicsStep: dt => log.Add("physics"));
            var runner = new TickRunner();
            runner.Attach(source);
            // Register in reverse order to prove that order comes from phases, not registration.
            for (int phase = TickPhases.Count - 1; phase >= 0; phase--)
            {
                runner.Register((TickPhase)phase, new Recorder(((TickPhase)phase).ToString(), log));
            }

            source.Step();

            Assert.That(log, Is.EqualTo(new[]
            {
                "Input:1", "Characters:1", "Weapons:1", "physics",
                "Ballistics:1", "Damage:1", "AI:1", "Raid:1",
            }));
            Assert.That(runner.CompletedTicks, Is.EqualTo(1UL));
            Assert.That(runner.LastTick, Is.EqualTo(1u));
        }

        [Test]
        public void Register_SamePhaseTwice_SecondReturnsFalse()
        {
            var runner = new TickRunner();
            var recorder = new Recorder("a", new List<string>());

            Assert.That(runner.Register(TickPhase.AI, recorder), Is.True);
            Assert.That(runner.Register(TickPhase.AI, recorder), Is.False);
            Assert.That(runner.CountIn(TickPhase.AI), Is.EqualTo(1));
        }

        [Test]
        public void Register_DifferentPhases_Allowed()
        {
            var runner = new TickRunner();
            var recorder = new Recorder("a", new List<string>());

            Assert.That(runner.Register(TickPhase.Input, recorder), Is.True);
            Assert.That(runner.Register(TickPhase.Raid, recorder), Is.True);
        }

        [Test]
        public void Register_Null_Throws()
        {
            var runner = new TickRunner();

            Assert.Throws<ArgumentNullException>(() => runner.Register(TickPhase.Input, null));
        }

        [Test]
        public void Register_DuringTick_RunsFromNextTick()
        {
            var log = new List<string>();
            var source = new LocalTickSource();
            var runner = new TickRunner();
            runner.Attach(source);
            var late = new Recorder("late", log);
            var first = new Recorder("first", log);
            first.OnTick = ctx => runner.Register(TickPhase.Characters, late);
            runner.Register(TickPhase.Characters, first);

            source.Step();
            source.Step();

            Assert.That(log, Is.EqualTo(new[] { "first:1", "first:2", "late:2" }));
        }

        [Test]
        public void Unregister_DuringTick_SkipsRestOfPhase()
        {
            var log = new List<string>();
            var source = new LocalTickSource();
            var runner = new TickRunner();
            runner.Attach(source);
            var victim = new Recorder("victim", log);
            var killer = new Recorder("killer", log);
            killer.OnTick = ctx => runner.Unregister(TickPhase.Damage, victim);
            runner.Register(TickPhase.Damage, killer);
            runner.Register(TickPhase.Damage, victim);

            source.Step();

            Assert.That(log, Is.EqualTo(new[] { "killer:1" }));
            Assert.That(runner.CountIn(TickPhase.Damage), Is.EqualTo(1));
        }

        [Test]
        public void Detach_AfterAttach_StopsRunning()
        {
            var log = new List<string>();
            var source = new LocalTickSource();
            var runner = new TickRunner();
            runner.Attach(source);
            runner.Register(TickPhase.Input, new Recorder("a", log));

            runner.Detach();
            source.Step();

            Assert.That(log, Is.Empty);
            Assert.That(runner.Source, Is.Null);
        }

        [Test]
        public void Attach_SecondSource_DetachesFirst()
        {
            var log = new List<string>();
            var first = new LocalTickSource();
            var second = new LocalTickSource();
            var runner = new TickRunner();
            runner.Register(TickPhase.Input, new Recorder("a", log));

            runner.Attach(first);
            runner.Attach(second);
            first.Step();
            second.Step();

            Assert.That(log, Is.EqualTo(new[] { "a:1" }));
            Assert.That(runner.Source, Is.SameAs(second));
        }
    }
}
