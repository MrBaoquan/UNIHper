using System.IO;
using UnityEngine.UI;
using UnityEngine;
using UniRx;

namespace UNIHper
{
    public static class BuiltInTypeExtension
    {
        public static Vector2 ToVector2(this Vector3 InVector3)
        {
            return new Vector2(InVector3.x, InVector3.y);
        }

        public static Vector3 ToVector3(this Vector2 InVector2)
        {
            return new Vector3(InVector2.x, InVector2.y, 0);
        }

        public static Vector3 ToVector3(this Vector2 InVector2, float InZ)
        {
            return new Vector3(InVector2.x, InVector2.y, InZ);
        }
    }
}
