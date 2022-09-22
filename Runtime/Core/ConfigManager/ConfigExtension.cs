namespace UNIHper {

    public static class ConfigExtension {
        public static void Serialize<T> (this T Self) {
            Managements.Config.Serialize<T> ();
        }
    }

}