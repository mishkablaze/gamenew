namespace Raidline.Infrastructure.Ticking
{
    /// <summary>Callback for tick source events; the context is passed by readonly reference.</summary>
    public delegate void TickCallback(in TickContext ctx);
}
