namespace UNIHper {

    public static class ConfigExtension {
        public static void Serialize<T> (this T Self) where T : UConfig {
            Managements.Config.Serialize<T> ();
        }
    }

}