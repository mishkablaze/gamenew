using System;
using Raidline.Core.Timing;

namespace Raidline.Infrastructure.Ticking
{
    /// <summary>
    /// Tick source without networking, driven by frame time through <see cref="TickClock"/>.
    /// For EditMode/PlayMode tests and editor tools. In the game ticks come from Fish-Net
    /// (Raidline.Gameplay.Networking.NetworkTickSource).
    /// Acts as both server and client (<see cref="TickContext.Local"/>).
    /// </summary>
    public sealed class LocalTickSource : ITickSource, IFrameUpdatable
    {
        private readonly TickClock _clock;
        private readonly Action<float> _physicsStep;
        private uint _tick;

        /// <param name="tickRate">Ticks per second.</param>
        /// <param name="maxTicksPerFrame">Spiral-of-death guard (see <see cref="TickClock"/>).</param>
        /// <param name="physicsStep">Optional physics step called between the two stages, e.g. Physics.Simulate.</param>
        public LocalTickSource(double tickRate = 60d, int maxTicksPerFrame = 4, Action<float> physicsStep = null)
        {
            _clock = new TickClock(tickRate, maxTicksPerFrame);
            _physicsStep = physicsStep;
        }

        public event TickCallback PrePhysicsTick;

        public event TickCallback PostPhysicsTick;

        public double TickDelta => _clock.TickDelta;

        /// <summary>Interpolation factor between the last two ticks.</summary>
        public double Alpha => _clock.Alpha;

        /// <summary>Number of the last produced tick (first tick is 1).</summary>
        public uint CurrentTick => _tick;

        /// <summary>Feeds frame time; raises zero or more ticks. Returns how many.</summary>
        public int Advance(double deltaSeconds)
        {
            int ticks = _clock.Advance(deltaSeconds);
            for (int i = 0; i < ticks; i++)
            {
                Step();
            }

            return ticks;
        }

        /// <summary>Raises exactly one tick regardless of time (for tests).</summary>
        public void Step()
        {
            _tick++;
            float dt = (float)_clock.TickDelta;
            var ctx = TickContext.Local(_tick, dt);
            PrePhysicsTick?.Invoke(in ctx);
            _physicsStep?.Invoke(dt);
            PostPhysicsTick?.Invoke(in ctx);
        }

        void IFrameUpdatable.FrameUpdate(float deltaTime)
        {
            Advance(deltaTime);
        }
    }
}
