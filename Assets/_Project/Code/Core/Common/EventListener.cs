namespace Raidline.Core.Common
{
    /// <summary>Handler for <see cref="EventChannel{T}"/>; the event is passed by readonly reference.</summary>
    public delegate void EventListener<T>(in T evt) where T : struct;
}
