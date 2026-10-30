namespace Raidline.Infrastructure.Ticking
{
    /// <summary>Per-frame update for presentation, UI and camera — never gameplay logic.</summary>
    public interface IFrameUpdatable
    {
        void FrameUpdate(float deltaTime);
    }
}
