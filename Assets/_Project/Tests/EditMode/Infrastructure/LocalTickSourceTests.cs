using System.Collections.Generic;
using NUnit.Framework;
using Raidline.Infrastructure.Ticking;

namespace Raidline.Tests.EditMode.Infrastructure
{
    public sealed class LocalTickSourceTests
    {
        [Test]
        public void Advance_ThirtyFpsFrame_RaisesTwoTicksWithIncreasingNumbers()
        {
            var source = new LocalTickSource(60d);
            var ticks = new List<uint>();
            var deltas = new List<float>();
            source.PrePhysicsTick += (in TickContext ctx) =>
            {
                ticks.Add(ctx.Tick);
                deltas.Add(ctx.DeltaTime);
                Assert.That(ctx.IsServer && ctx.IsClient, Is.True);
                Assert.That(ctx.IsReplaying, Is.False);
            };

            int produced = source.Advance(1d / 30d);

            Assert.That(produced, Is.EqualTo(2));
            Assert.That(ticks, Is.EqualTo(new[] { 1u, 2u }));
            Assert.That(deltas, Has.All.EqualTo(1f / 60f).Within(1e-6f));
            Assert.That(source.CurrentTick, Is.EqualTo(2u));
        }

        [Test]
        public void Step_PreAndPost_ShareTickNumber()
        {
            var source = new LocalTickSource();
            uint pre = 0;
            uint post = 0;
            source.PrePhysicsTick += (in TickContext ctx) => pre = ctx.Tick;
            source.PostPhysicsTick += (in TickContext ctx) => post = ctx.Tick;

            source.Step();
            source.Step();

            Assert.That(pre, Is.EqualTo(2u));
            Assert.That(post, Is.EqualTo(2u));
        }

        [Test]
        public void FrameUpdate_ViaInterface_AdvancesClock()
        {
            var source = new LocalTickSource(60d);
            int ticks = 0;
            source.PostPhysicsTick += (in TickContext ctx) => ticks++;

            ((IFrameUpdatable)source).FrameUpdate(0.05f);

            Assert.That(ticks, Is.EqualTo(3));
        }
    }
}
