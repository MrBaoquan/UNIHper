namespace UNIHper
{
    public static class ParserExtension
    {
        public static float Parse2Float(this string _value){
            if(_value==string.Empty){
                return 0f;
            }
            return float.Parse(_value);
        }

        public static int Parse2Int(this string _value){
            if(_value==string.Empty){
                return 0;
            }
            return int.Parse(_value);
        }

        public static byte[] ToBytes(this string _value){
            return System.Text.Encoding.ASCII.GetBytes(_value);
        }
    }
}