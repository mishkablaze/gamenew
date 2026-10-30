using System;

namespace Raidline.Core.Common
{
    /// <summary>
    /// Identifier of a gameplay entity (character, world item, container, door, extract).
    /// <see cref="None"/> (value 0) is never assigned to a real entity.
    /// Networked objects map EntityId to NetworkObject.ObjectId on the server (ARCHITECTURE §6).
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        /// <summary>The "no entity" value.</summary>
        public static readonly EntityId None = default;

        /// <summary>Raw value; positive for valid ids.</summary>
        public readonly int Value;

        public EntityId(int value)
        {
            Value = value;
        }

        /// <summary>True for any id produced by an allocator (value &gt; 0).</summary>
        public bool IsValid => Value > 0;

        public bool Equals(EntityId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is EntityId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(EntityId left, EntityId right) => left.Value == right.Value;

        public static bool operator !=(EntityId left, EntityId right) => left.Value != right.Value;

        public override string ToString() => IsValid ? "E#" + Value : "E#none";
    }
}
