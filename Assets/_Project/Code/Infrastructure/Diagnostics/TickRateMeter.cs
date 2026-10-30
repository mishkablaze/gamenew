using Raidline.Infrastructure.Ticking;
using UnityEngine;

namespace Raidline.Infrastructure.Diagnostics
{
    /// <summary>
    /// Measures how many ticks per second actually run (for the perf HUD and boot checks).
    /// Register it in any phase; it reads real time, so it is a diagnostic, not gameplay logic.
    /// </summary>
    public sealed class TickRateMeter : ITickable
    {
        private const double Window = 1d;

        private double _windowStart = -1d;
        private int _ticksInWindow;

        /// <summary>Ticks counted in the last full one-second window.</summary>
        public float TicksPerSecond { get; private set; }

        /// <summary>Total ticks seen.</summary>
        public ulong TotalTicks { get; private set; }

        public void Tick(in TickContext ctx)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (_windowStart < 0d)
            {
                _windowStart = now;
            }

            TotalTicks++;
            _ticksInWindow++;
            double elapsed = now - _windowStart;
            if (elapsed >= Window)
            {
                TicksPerSecond = (float)(_ticksInWindow / elapsed);
                _ticksInWindow = 0;
                _windowStart = now;
            }
        }
    }
}
