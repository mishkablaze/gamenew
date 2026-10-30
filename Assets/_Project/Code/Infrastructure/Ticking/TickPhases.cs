namespace Raidline.Infrastructure.Ticking
{
    /// <summary>Helpers for <see cref="TickPhase"/>.</summary>
    public static class TickPhases
    {
        /// <summary>Number of phases.</summary>
        public const int Count = 7;

        /// <summary>First phase that runs after the physics step.</summary>
        public const TickPhase FirstPostPhysics = TickPhase.Ballistics;

        /// <summary>Stage of a phase.</summary>
        public static TickStage StageOf(TickPhase phase)
        {
            return phase < FirstPostPhysics ? TickStage.PrePhysics : TickStage.PostPhysics;
        }
    }
}
