using System.IO;
using UnityEngine.UI;
using UnityEngine;
using UniRx;

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

        // public static Texture2D ResizeWithRenderTexture(
        //     this Texture2D source,
        //     int targetWidth,
        //     int targetHeight
        // )
        // {
        //     // 创建 RenderTexture
        //     RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24);
        //     RenderTexture.active = rt;

        //     // 将源纹理拷贝到 RenderTexture
        //     Graphics.Blit(source, rt);

        //     // 创建目标 Texture2D
        //     Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);

        //     // 从 RenderTexture 读取像素数据
        //     result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        //     result.Apply();

        //     // 释放 RenderTexture
        //     RenderTexture.active = null;
        //     rt.Release();

        //     return result;
        // }

        public static Texture2D Resize(
            this Texture texture,
            int width,
            int height,
            FilterMode filterMode = FilterMode.Bilinear
        )
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default,
                1
            );
            temporary.filterMode = FilterMode.Bilinear;
            RenderTexture.active = temporary;
            GL.Clear(clearDepth: false, clearColor: true, new Color(1f, 1f, 1f, 0f));
            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            Graphics.Blit(texture, temporary);
            Texture2D texture2D = new Texture2D(
                width,
                height,
                TextureFormat.ARGB32,
                mipChain: true,
                linear: false
            );
            texture2D.filterMode = filterMode;
            texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;
            return texture2D;
        }

        public static Texture2D MakeSquare(this Texture2D source, Color fillColor = default(Color))
        {
            // 获取原始图片的宽度和高度
            int originalWidth = source.width;
            int originalHeight = source.height;

            // 计算正方形的边长（取宽度和高度的最大值）
            int squareSize = Mathf.Max(originalWidth, originalHeight);

            // 创建一个新的正方形纹理
            Texture2D squareTexture = new Texture2D(squareSize, squareSize);

            // 初始化整个纹理为填充颜色
            Color[] fillPixels = new Color[squareSize * squareSize];
            for (int i = 0; i < fillPixels.Length; i++)
            {
                fillPixels[i] = fillColor;
            }
            squareTexture.SetPixels(fillPixels);

            // 计算原始图片的偏移位置，使其居中
            int xOffset = (squareSize - originalWidth) / 2;
            int yOffset = (squareSize - originalHeight) / 2;

            // 将原始图片的像素复制到正方形纹理的居中位置
            squareTexture.SetPixels(
                xOffset,
                yOffset,
                originalWidth,
                originalHeight,
                source.GetPixels()
            );

            // 应用更改
            squareTexture.Apply();

            return squareTexture;
        }

        public static Texture2D AddPadding(
            this Texture2D source,
            int padding,
            Color fillColor = default(Color)
        )
        {
            // 获取原始图片的宽度和高度
            int originalWidth = source.width;
            int originalHeight = source.height;

            // 计算新纹理的宽度和高度
            int paddedWidth = originalWidth + 2 * padding;
            int paddedHeight = originalHeight + 2 * padding;

            // 创建一个新的纹理，大小为添加 padding 后的尺寸
            Texture2D paddedTexture = new Texture2D(paddedWidth, paddedHeight);

            // 初始化整个纹理为填充颜色
            Color[] fillPixels = new Color[paddedWidth * paddedHeight];
            for (int i = 0; i < fillPixels.Length; i++)
            {
                fillPixels[i] = fillColor;
            }
            paddedTexture.SetPixels(fillPixels);

            // 计算源图片在新纹理中的起始偏移位置
            int xOffset = padding;
            int yOffset = padding;

            // 将源图片的像素复制到新纹理的居中区域
            paddedTexture.SetPixels(
                xOffset,
                yOffset,
                originalWidth,
                originalHeight,
                source.GetPixels()
            );

            // 应用更改
            paddedTexture.Apply();

            return paddedTexture;
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
