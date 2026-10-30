namespace Raidline.Infrastructure.Ticking
{
    /// <summary>
    /// Data passed to every <see cref="ITickable"/> in a tick.
    /// Object ownership is per object (Fish-Net NetworkObject), so it is not part of this context.
    /// </summary>
    public readonly struct TickContext
    {
        /// <summary>Tick number from the tick source (Fish-Net LocalTick in the game).</summary>
        public readonly uint Tick;

        /// <summary>Seconds per tick (1/60).</summary>
        public readonly float DeltaTime;

        /// <summary>This process runs the server (true in host mode).</summary>
        public readonly bool IsServer;

        /// <summary>This process runs a client (true in host mode).</summary>
        public readonly bool IsClient;

        /// <summary>
        /// The tick is being re-simulated during prediction reconciliation.
        /// Systems with side effects outside prediction (sound, VFX, UI events) must skip them.
        /// Always false for ticks coming from a tick source; replicate-driven code sets it itself.
        /// </summary>
        public readonly bool IsReplaying;

        public TickContext(uint tick, float deltaTime, bool isServer, bool isClient, bool isReplaying)
        {
            Tick = tick;
            DeltaTime = deltaTime;
            IsServer = isServer;
            IsClient = isClient;
            IsReplaying = isReplaying;
        }

        /// <summary>Context for a local (non-networked) tick: acts as both server and client.</summary>
        public static TickContext Local(uint tick, float deltaTime)
        {
            return new TickContext(tick, deltaTime, true, true, false);
        }

        public override string ToString()
        {
            return "Tick " + Tick + (IsServer ? " S" : string.Empty) + (IsClient ? " C" : string.Empty) + (IsReplaying ? " replay" : string.Empty);
        }
    }
}
