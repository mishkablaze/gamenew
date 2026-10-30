namespace Raidline.Infrastructure.Ticking
{
    /// <summary>
    /// A gameplay system that runs once per simulation tick in its <see cref="TickPhase"/>.
    /// The only place where gameplay logic may live (AGENTS.md §3).
    /// </summary>
    public interface ITickable
    {
        void Tick(in TickContext ctx);
    }
}
