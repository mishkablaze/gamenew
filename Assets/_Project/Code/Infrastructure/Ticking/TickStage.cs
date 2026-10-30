namespace Raidline.Infrastructure.Ticking
{
    /// <summary>Which half of a tick a phase belongs to.</summary>
    public enum TickStage
    {
        PrePhysics = 0,
        PostPhysics = 1,
    }
}
