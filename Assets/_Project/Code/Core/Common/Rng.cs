using System;

namespace Raidline.Core.Common
{
    /// <summary>
    /// Deterministic pseudo-random generator (PCG32, XSH-RR variant by M. O'Neill).
    /// The same seed and stream always produce the same sequence on every platform,
    /// which gameplay relies on (seeded loot, recoil spread, server-side rolls).
    /// Use this instead of UnityEngine.Random in all gameplay code (CONVENTIONS §4).
    /// Not thread-safe; give each system or job its own instance (see <see cref="Fork"/>).
    /// </summary>
    public sealed class Rng
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong DefaultStream = 0xDA3E39CB94B95BDBUL;
        private const float FloatUnit = 1f / 16777216f; // 2^-24

        private ulong _state;
        private readonly ulong _increment;

        /// <summary>Creates a generator. Equal (seed, stream) pairs give equal sequences.</summary>
        /// <param name="seed">Starting state.</param>
        /// <param name="stream">Sequence selector; different streams are independent.</param>
        public Rng(ulong seed, ulong stream = DefaultStream)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0UL;
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        /// <summary>Next uniformly distributed 32-bit value.</summary>
        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>Next uniformly distributed 64-bit value.</summary>
        public ulong NextULong()
        {
            ulong high = NextUInt();
            ulong low = NextUInt();
            return (high << 32) | low;
        }

        /// <summary>Uniform value in [0, bound) without modulo bias.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bound"/> is 0.</exception>
        public uint NextUInt(uint bound)
        {
            if (bound == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "Bound must be positive.");
            }

            // Reject the low values that would make the modulo biased.
            uint threshold = unchecked(0u - bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                {
                    return r % bound;
                }
            }
        }

        /// <summary>Uniform int in [0, maxExclusive).</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxExclusive"/> &lt;= 0.</exception>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be positive.");
            }

            return (int)NextUInt((uint)maxExclusive);
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        /// <exception cref="ArgumentOutOfRangeException">Empty range.</exception>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Range is empty.");
            }

            long span = (long)maxExclusive - minInclusive;
            if (span > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Range is wider than 2^32-1.");
            }

            return (int)(minInclusive + (long)NextUInt((uint)span));
        }

        /// <summary>Uniform float in [0, 1) with 24 bits of precision.</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * FloatUnit;
        }

        /// <summary>Uniform float in [min, max). Returns min when min == max.</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>True with the given probability; p &lt;= 0 never, p &gt;= 1 always.</summary>
        public bool Chance(float probability)
        {
            if (probability <= 0f || float.IsNaN(probability))
            {
                return false;
            }

            if (probability >= 1f)
            {
                return true;
            }

            return NextFloat() < probability;
        }

        /// <summary>
        /// Creates an independent child generator, e.g. one per system from the raid seed.
        /// Advances this generator by two steps.
        /// </summary>
        /// <param name="salt">Distinguishes children forked at the same point.</param>
        public Rng Fork(ulong salt)
        {
            ulong seed = NextULong();
            return new Rng(seed ^ salt, salt);
        }
    }
}
