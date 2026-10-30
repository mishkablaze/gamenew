namespace Raidline.Infrastructure.Ticking
{
    /// <summary>
    /// Fixed order of gameplay systems inside one simulation tick (ARCHITECTURE §5).
    /// Phases before <see cref="TickStage.PostPhysics"/> run before the physics step,
    /// the rest after it (so ballistics sees colliders already moved this tick).
    /// </summary>
    public enum TickPhase
    {
        /// <summary>Input snapshot into CharacterInputFrame. Pre-physics.</summary>
        Input = 0,

        /// <summary>Movement (Replicate/Reconcile), stamina, stance. Pre-physics.</summary>
        Characters = 1,

        /// <summary>Firing, reloading, recoil; spawns projectiles. Pre-physics.</summary>
        Weapons = 2,

        /// <summary>Projectile movement, raycasts, hits (server). Post-physics.</summary>
        Ballistics = 3,

        /// <summary>Damage application, health effects (server). Post-physics.</summary>
        Damage = 4,

        /// <summary>Bot brains, within budget and LOD (server). Post-physics.</summary>
        AI = 5,

        /// <summary>Raid timer, extraction, spawns (server). Post-physics.</summary>
        Raid = 6,
    }
}
