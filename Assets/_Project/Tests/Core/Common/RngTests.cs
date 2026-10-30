using System;
using NUnit.Framework;
using Raidline.Core.Common;

namespace Raidline.Core.Tests.Common
{
    public sealed class RngTests
    {
        [Test]
        public void NextUInt_Seed42Stream54_MatchesPcg32Reference()
        {
            // Reference output of pcg32_srandom_r(42, 54) from the PCG paper's demo.
            var rng = new Rng(42UL, 54UL);
            uint[] expected = { 0xa15c02b7u, 0x7b47f409u, 0xba1d3330u, 0x83d2f293u, 0xbfa4784bu, 0xcbed606eu };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(rng.NextUInt(), Is.EqualTo(expected[i]), "value #" + i);
            }
        }

        [Test]
        public void NextUInt_SameSeed_ProducesSameSequence()
        {
            var a = new Rng(12345UL);
            var b = new Rng(12345UL);

            for (int i = 0; i < 1000; i++)
            {
                Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()));
            }
        }

        [Test]
        public void NextUInt_DifferentStreams_ProduceDifferentSequences()
        {
            var a = new Rng(7UL, 1UL);
            var b = new Rng(7UL, 2UL);
            int equal = 0;

            for (int i = 0; i < 100; i++)
            {
                if (a.NextUInt() == b.NextUInt())
                {
                    equal++;
                }
            }

            Assert.That(equal, Is.LessThan(3));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(7)]
        [TestCase(1000)]
        public void NextInt_AnyBound_StaysInRange(int bound)
        {
            var rng = new Rng(99UL);

            for (int i = 0; i < 10000; i++)
            {
                int v = rng.NextInt(bound);
                Assert.That(v, Is.GreaterThanOrEqualTo(0).And.LessThan(bound));
            }
        }

        [Test]
        public void NextInt_TenBuckets_RoughlyUniform()
        {
            var rng = new Rng(2024UL);
            var counts = new int[10];
            const int samples = 100000;

            for (int i = 0; i < samples; i++)
            {
                counts[rng.NextInt(10)]++;
            }

            for (int i = 0; i < counts.Length; i++)
            {
                Assert.That(counts[i], Is.InRange(9400, 10600), "bucket " + i);
            }
        }

        [Test]
        public void RangeInt_NegativeRange_StaysInRange()
        {
            var rng = new Rng(5UL);

            for (int i = 0; i < 10000; i++)
            {
                Assert.That(rng.Range(-5, 3), Is.InRange(-5, 2));
            }
        }

        [Test]
        public void RangeInt_FullWidthMinusOne_DoesNotThrow()
        {
            var rng = new Rng(5UL);

            Assert.DoesNotThrow(() => rng.Range(int.MinValue, int.MaxValue));
        }

        [Test]
        public void NextFloat_ManySamples_InUnitInterval()
        {
            var rng = new Rng(3UL);

            for (int i = 0; i < 100000; i++)
            {
                float f = rng.NextFloat();
                Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void RangeFloat_EqualBounds_ReturnsBound()
        {
            var rng = new Rng(3UL);

            Assert.That(rng.Range(2.5f, 2.5f), Is.EqualTo(2.5f));
        }

        [TestCase(0f, false)]
        [TestCase(-1f, false)]
        [TestCase(float.NaN, false)]
        [TestCase(1f, true)]
        [TestCase(5f, true)]
        public void Chance_EdgeProbabilities_AreFixed(float probability, bool expected)
        {
            var rng = new Rng(11UL);

            for (int i = 0; i < 1000; i++)
            {
                Assert.That(rng.Chance(probability), Is.EqualTo(expected));
            }
        }

        [Test]
        public void Chance_Quarter_HitsAboutQuarter()
        {
            var rng = new Rng(77UL);
            int hits = 0;
            const int samples = 40000;

            for (int i = 0; i < samples; i++)
            {
                if (rng.Chance(0.25f))
                {
                    hits++;
                }
            }

            Assert.That(hits / (double)samples, Is.InRange(0.24, 0.26));
        }

        [Test]
        public void Fork_SameParentState_GivesSameChild()
        {
            var childA = new Rng(500UL).Fork(1UL);
            var childB = new Rng(500UL).Fork(1UL);

            for (int i = 0; i < 100; i++)
            {
                Assert.That(childA.NextUInt(), Is.EqualTo(childB.NextUInt()));
            }
        }

        [Test]
        public void Fork_DifferentSalts_GiveDifferentChildren()
        {
            var parentA = new Rng(500UL);
            var parentB = new Rng(500UL);
            var childA = parentA.Fork(1UL);
            var childB = parentB.Fork(2UL);

            Assert.That(childA.NextULong(), Is.Not.EqualTo(childB.NextULong()));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            var rng = new Rng(1UL);

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(-3));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextUInt(0u));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Range(5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Range(5, 1));
        }
    }
}
