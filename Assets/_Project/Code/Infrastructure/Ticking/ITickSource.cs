namespace Raidline.Infrastructure.Ticking
{
    /// <summary>
    /// Something that produces simulation ticks: Fish-Net's TimeManager in the game
    /// (Raidline.Gameplay.Networking.NetworkTickSource) or <see cref="LocalTickSource"/> in tests and tools.
    /// For every tick it raises <see cref="PrePhysicsTick"/>, then steps physics, then raises <see cref="PostPhysicsTick"/>.
    /// </summary>
    public interface ITickSource
    {
        /// <summary>Seconds per tick.</summary>
        double TickDelta { get; }

        /// <summary>Raised at the start of a tick, before the physics step.</summary>
        event TickCallback PrePhysicsTick;

        /// <summary>Raised after the physics step of the same tick.</summary>
        event TickCallback PostPhysicsTick;
    }
}
