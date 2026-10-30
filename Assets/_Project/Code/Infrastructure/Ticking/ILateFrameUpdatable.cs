namespace Raidline.Infrastructure.Ticking
{
    /// <summary>Per-frame late update (camera follow, interpolation of visuals).</summary>
    public interface ILateFrameUpdatable
    {
        void LateFrameUpdate(float deltaTime);
    }
}
