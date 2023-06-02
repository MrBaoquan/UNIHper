using System.Text;
using UnityEngine;

namespace UNIHper
{
    public static class ParserExtension
    {
        public static Color Parse2Color(this string colorString)
        {
            if (colorString.StartsWith("#"))
            {
                colorString = colorString.Substring(1);
            }
            if (colorString.Length == 6)
            {
                colorString = colorString + "FF";
            }
            if (colorString.Length != 8)
            {
                return Color.white;
            }
            byte r = byte.Parse(
                colorString.Substring(0, 2),
                System.Globalization.NumberStyles.HexNumber
            );
            byte g = byte.Parse(
                colorString.Substring(2, 2),
                System.Globalization.NumberStyles.HexNumber
            );
            byte b = byte.Parse(
                colorString.Substring(4, 2),
                System.Globalization.NumberStyles.HexNumber
            );
            byte a = byte.Parse(
                colorString.Substring(6, 2),
                System.Globalization.NumberStyles.HexNumber
            );
            return new Color32(r, g, b, a);
        }

        public static byte[] ToUTF8Bytes(this string content)
        {
            return Encoding.UTF8.GetBytes(content);
        }
    }
}
