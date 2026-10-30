using NUnit.Framework;
using Raidline.Core.Common;

namespace Raidline.Core.Tests.Common
{
    public sealed class EntityIdAndResultTests
    {
        private enum TestError
        {
            None = 0,
            NoRoom = 1,
        }

        [Test]
        public void EntityId_None_IsInvalid()
        {
            Assert.That(EntityId.None.IsValid, Is.False);
            Assert.That(default(EntityId), Is.EqualTo(EntityId.None));
        }

        [Test]
        public void Allocator_Next_StartsAtOneAndIncrements()
        {
            var allocator = new EntityIdAllocator();

            EntityId first = allocator.Next();
            EntityId second = allocator.Next();

            Assert.That(first.Value, Is.EqualTo(1));
            Assert.That(second.Value, Is.EqualTo(2));
            Assert.That(first.IsValid && second.IsValid, Is.True);
            Assert.That(first != second, Is.True);
            Assert.That(allocator.Count, Is.EqualTo(2));
        }

        [Test]
        public void Allocator_Reset_StartsOver()
        {
            var allocator = new EntityIdAllocator();
            allocator.Next();
            allocator.Next();

            allocator.Reset();

            Assert.That(allocator.Next().Value, Is.EqualTo(1));
        }

        [Test]
        public void EntityId_SameValue_EqualAndSameHash()
        {
            var a = new EntityId(42);
            var b = new EntityId(42);

            Assert.That(a == b, Is.True);
            Assert.That(a.Equals((object)b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.ToString(), Is.EqualTo("E#42"));
        }

        [Test]
        public void Result_OkAndFail_ReportState()
        {
            Result<TestError> ok = Result<TestError>.Ok();
            Result<TestError> fail = Result<TestError>.Fail(TestError.NoRoom);

            Assert.That(ok.IsSuccess, Is.True);
            Assert.That(ok.IsFailure, Is.False);
            Assert.That(fail.IsSuccess, Is.False);
            Assert.That(fail.Error, Is.EqualTo(TestError.NoRoom));
            Assert.That(fail.ToString(), Is.EqualTo("Fail(NoRoom)"));
        }
    }
}
