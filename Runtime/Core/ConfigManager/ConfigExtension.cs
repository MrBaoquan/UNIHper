namespace UNIHper
{
    public static class ConfigExtension
    {
        public static void Save<T>(this T Self)
            where T : UConfig
        {
            Managements.Config.Save<T>();
        }
    }
}
