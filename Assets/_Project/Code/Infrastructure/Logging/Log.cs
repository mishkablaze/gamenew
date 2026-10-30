using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Raidline.Infrastructure.Logging
{
    /// <summary>
    /// The only logging entry point (CONVENTIONS §2). Stateless wrapper over UnityEngine.Debug.
    /// <see cref="Dev"/> calls are compiled only in the editor, development builds and builds with
    /// the RAIDLINE_DEV define — in release builds the call and its argument evaluation disappear.
    /// Never log from hot paths, even with Dev: string building allocates.
    /// </summary>
    public static class Log
    {
        /// <summary>Diagnostic message; stripped from release builds.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("RAIDLINE_DEV")]
        public static void Dev(LogCategory category, string message)
        {
            Debug.Log(Format(category, message));
        }

        /// <summary>Important information that also matters in release builds (rare).</summary>
        public static void Info(LogCategory category, string message)
        {
            Debug.Log(Format(category, message));
        }

        /// <summary>Something unexpected that the game can recover from.</summary>
        public static void Warn(LogCategory category, string message)
        {
            Debug.LogWarning(Format(category, message));
        }

        /// <summary>Something is broken.</summary>
        public static void Error(LogCategory category, string message)
        {
            Debug.LogError(Format(category, message));
        }

        /// <summary>Logs an exception with a category prefix.</summary>
        public static void Exception(LogCategory category, System.Exception exception)
        {
            Debug.LogError(Format(category, exception.GetType().Name + ": " + exception.Message));
            Debug.LogException(exception);
        }

        private static string Format(LogCategory category, string message)
        {
            return "[" + category + "] " + message;
        }
    }
}
