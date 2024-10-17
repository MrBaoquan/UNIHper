using System.IO;
using UnityEngine.UI;
using UnityEngine;

namespace UNIHper
{
    public static class BuiltInTypeExtension
    {
        /// <summary>
        /// note: dont use this method in Update(), it will cause memory increase fast
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static Texture2D ToTexture2D(this Texture texture)
        {
            RenderTexture _rt = new RenderTexture(texture.width, texture.height, 0);
            Graphics.Blit(texture, _rt);

            Texture2D _texture2D = new Texture2D(texture.width, texture.height);
            RenderTexture.active = _rt;
            _texture2D.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            _texture2D.Apply();

            RenderTexture.active = null;
            _rt.Release();
            return _texture2D;
        }

        public static Sprite ToSprite(this Texture2D texture2D)
        {
            return texture2D.ToSprite(
                new Rect(0, 0, texture2D.width, texture2D.height),
                Vector2.one * 0.5f
            );
        }

        public static Sprite ToSprite(this Texture2D texture2D, Rect rect)
        {
            return texture2D.ToSprite(rect, Vector2.one * 0.5f);
        }

        public static Sprite ToSprite(this Texture2D texture2D, Vector2 pivot)
        {
            return texture2D.ToSprite(new Rect(0, 0, texture2D.width, texture2D.height), pivot);
        }

        public static Sprite ToSprite(this Texture2D texture2D, Rect rect, Vector2 pivot)
        {
            return Sprite.Create(texture2D, rect, pivot);
        }

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

        public static bool SaveToFile(this Texture2D texture2D, string savePath, int quality = 90)
        {
            try
            {
                var _bytes = texture2D.EncodeToJPG(90);
                var _saveDir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(_saveDir))
                {
                    Directory.CreateDirectory(_saveDir);
                }
                File.WriteAllBytes(savePath, _bytes);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
    }
}
