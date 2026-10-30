namespace Raidline.Infrastructure.Logging
{
    /// <summary>Area of the game a log line belongs to; printed as a prefix.</summary>
    public enum LogCategory
    {
        General = 0,
        Boot = 1,
        Net = 2,
        Tick = 3,
        Input = 4,
        Gameplay = 5,
        AI = 6,
        UI = 7,
        Save = 8,
        Audio = 9,
    }
}
