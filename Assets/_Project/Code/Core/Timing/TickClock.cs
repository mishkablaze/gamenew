using System;

namespace Raidline.Core.Timing
{
    /// <summary>
    /// Fixed-step accumulator: turns variable frame times into a whole number of fixed ticks.
    /// Used by LocalTickSource (tests, tools without network). In the game the tick comes
    /// from Fish-Net's TimeManager (ARCHITECTURE §5, D-007).
    /// <para>Protects against the "spiral of death": at most <see cref="MaxTicksPerAdvance"/>
    /// ticks are produced per call; the backlog beyond that is dropped and counted in <see cref="DroppedTicks"/>.</para>
    /// </summary>
    public sealed class TickClock
    {
        // Absorbs floating point error so that N frames of exactly 1/rate give exactly N ticks.
        private const double Epsilon = 1e-9;

        private double _accumulator;

        /// <param name="tickRate">Ticks per second (default 60).</param>
        /// <param name="maxTicksPerAdvance">Upper bound of ticks per <see cref="Advance"/> call (default 4).</param>
        /// <exception cref="ArgumentOutOfRangeException">Non-positive arguments.</exception>
        public TickClock(double tickRate = 60d, int maxTicksPerAdvance = 4)
        {
            if (!(tickRate > 0d) || double.IsInfinity(tickRate))
            {
                throw new ArgumentOutOfRangeException(nameof(tickRate), "Tick rate must be positive and finite.");
            }

            if (maxTicksPerAdvance <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTicksPerAdvance), "Must be positive.");
            }

            TickRate = tickRate;
            TickDelta = 1d / tickRate;
            MaxTicksPerAdvance = maxTicksPerAdvance;
        }

        /// <summary>Ticks per second.</summary>
        public double TickRate { get; }

        /// <summary>Seconds per tick.</summary>
        public double TickDelta { get; }

        /// <summary>Upper bound of ticks produced by one <see cref="Advance"/> call.</summary>
        public int MaxTicksPerAdvance { get; }

        /// <summary>Ticks produced since creation or <see cref="Reset"/>.</summary>
        public ulong TotalTicks { get; private set; }

        /// <summary>Ticks skipped because a frame was too long.</summary>
        public ulong DroppedTicks { get; private set; }

        /// <summary>Progress towards the next tick in [0, 1): interpolation factor for visuals.</summary>
        public double Alpha
        {
            get
            {
                double alpha = _accumulator / TickDelta;
                return alpha < 0d ? 0d : (alpha >= 1d ? 0.999999d : alpha);
            }
        }

        /// <summary>
        /// Adds frame time and returns how many ticks to run now (0..MaxTicksPerAdvance).
        /// Zero, negative, NaN and infinite frame times produce no ticks.
        /// </summary>
        public int Advance(double deltaSeconds)
        {
            if (!(deltaSeconds > 0d) || double.IsInfinity(deltaSeconds))
            {
                return 0;
            }

            _accumulator += deltaSeconds;

            int ticks = 0;
            while (_accumulator + Epsilon >= TickDelta && ticks < MaxTicksPerAdvance)
            {
                _accumulator -= TickDelta;
                ticks++;
            }

            if (_accumulator + Epsilon >= TickDelta)
            {
                double backlog = Math.Floor((_accumulator + Epsilon) / TickDelta);
                DroppedTicks += (ulong)backlog;
                _accumulator -= backlog * TickDelta;
            }

            if (_accumulator < 0d)
            {
                _accumulator = 0d;
            }

            TotalTicks += (ulong)ticks;
            return ticks;
        }

        /// <summary>Clears accumulated time and counters.</summary>
        public void Reset()
        {
            _accumulator = 0d;
            TotalTicks = 0;
            DroppedTicks = 0;
        }
    }
}
