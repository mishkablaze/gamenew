using System;
using NUnit.Framework;
using Raidline.Core.Timing;

namespace Raidline.Core.Tests.Timing
{
    public sealed class TickClockTests
    {
        [Test]
        public void Advance_SixtyFramesOfOneSixtieth_ProducesSixtyTicks()
        {
            var clock = new TickClock(60d);
            int ticks = 0;

            for (int i = 0; i < 60; i++)
            {
                ticks += clock.Advance(1d / 60d);
            }

            Assert.That(ticks, Is.EqualTo(60));
            Assert.That(clock.TotalTicks, Is.EqualTo(60UL));
            Assert.That(clock.DroppedTicks, Is.EqualTo(0UL));
        }

        [Test]
        public void Advance_OneHourAt144Fps_TickCountMatchesTime()
        {
            var clock = new TickClock(60d);
            long ticks = 0;
            const int frames = 144 * 3600;

            for (int i = 0; i < frames; i++)
            {
                ticks += clock.Advance(1d / 144d);
            }

            Assert.That(ticks, Is.InRange(60L * 3600 - 1, 60L * 3600));
        }

        [Test]
        public void Advance_HalfTickFrames_TickEveryOtherFrame()
        {
            var clock = new TickClock(60d);

            Assert.That(clock.Advance(1d / 120d), Is.EqualTo(0));
            Assert.That(clock.Alpha, Is.EqualTo(0.5d).Within(1e-6));
            Assert.That(clock.Advance(1d / 120d), Is.EqualTo(1));
            Assert.That(clock.Alpha, Is.EqualTo(0d).Within(1e-6));
        }

        [Test]
        public void Advance_LongFrame_CapsTicksAndCountsDropped()
        {
            var clock = new TickClock(60d, 4);

            int ticks = clock.Advance(1d);

            Assert.That(ticks, Is.EqualTo(4));
            Assert.That(clock.DroppedTicks, Is.EqualTo(56UL));
            Assert.That(clock.Alpha, Is.LessThan(1d));
        }

        [Test]
        public void Advance_ThirtyFps_TwoTicksPerFrame()
        {
            var clock = new TickClock(60d);

            for (int i = 0; i < 30; i++)
            {
                Assert.That(clock.Advance(1d / 30d), Is.EqualTo(2), "frame " + i);
            }
        }

        [TestCase(0d)]
        [TestCase(-0.5d)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void Advance_InvalidDelta_ProducesNoTicks(double delta)
        {
            var clock = new TickClock(60d);

            Assert.That(clock.Advance(delta), Is.EqualTo(0));
            Assert.That(clock.Alpha, Is.EqualTo(0d));
        }

        [Test]
        public void Reset_AfterTicks_ClearsState()
        {
            var clock = new TickClock(60d);
            clock.Advance(1d);
            clock.Advance(0.004d);

            clock.Reset();

            Assert.That(clock.TotalTicks, Is.EqualTo(0UL));
            Assert.That(clock.DroppedTicks, Is.EqualTo(0UL));
            Assert.That(clock.Alpha, Is.EqualTo(0d));
        }

        [Test]
        public void Constructor_Properties_AreConsistent()
        {
            var clock = new TickClock(50d, 3);

            Assert.That(clock.TickRate, Is.EqualTo(50d));
            Assert.That(clock.TickDelta, Is.EqualTo(0.02d).Within(1e-12));
            Assert.That(clock.MaxTicksPerAdvance, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickClock(0d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickClock(-60d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickClock(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickClock(60d, 0));
        }
    }
}
