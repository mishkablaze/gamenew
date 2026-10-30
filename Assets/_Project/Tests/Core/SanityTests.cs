using NUnit.Framework;

namespace Raidline.Core.Tests
{
    /// <summary>
    /// Smoke test: proves the Core test assembly runs both in Unity (EditMode)
    /// and through tools/test-core.sh (dotnet). Only NUnit APIs, no UnityEngine.
    /// </summary>
    public sealed class SanityTests
    {
        [Test]
        public void Arithmetic_Basic_Works()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
        }

        [TestCase(1, 1, 2)]
        [TestCase(-3, 3, 0)]
        public void Add_TableCases_ReturnsSum(int a, int b, int expected)
        {
            Assert.That(a + b, Is.EqualTo(expected));
        }
    }
}
