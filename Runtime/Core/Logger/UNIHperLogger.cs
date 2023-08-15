namespace UNIHper
{
    internal class UNIHperLogger
    {
        public static void Log(string message)
        {
            if (!UNIHperSettings.ShowDebugLog)
                return;
            UnityEngine.Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            if (!UNIHperSettings.ShowDebugLog)
                return;
            UnityEngine.Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
