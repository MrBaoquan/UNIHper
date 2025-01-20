using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UNIHper
{
    public static class TextureExtension
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

        public static List<Vector2Int> GetPixelsAboveBrightness(
            this Texture2D texture,
            float brightnessThreshold
        )
        {
            List<Vector2Int> pixels = new List<Vector2Int>();

            if (texture == null)
            {
                Debug.LogError("Texture2D is null.");
                return pixels;
            }

            // 获取所有像素
            Color[] colors = texture.GetPixels();

            // 遍历所有像素
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    // 获取当前像素颜色
                    Color color = colors[y * texture.width + x];

                    // 计算亮度值
                    float brightness = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;

                    // 判断亮度是否超过阈值
                    if (brightness > brightnessThreshold)
                    {
                        pixels.Add(new Vector2Int(x, y));
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// 判断指定像素的亮度是否大于给定阈值
        /// </summary>
        /// <param name="texture">目标Texture2D</param>
        /// <param name="x">像素的X坐标</param>
        /// <param name="y">像素的Y坐标</param>
        /// <param name="threshold">亮度阈值 (0到1之间)</param>
        /// <returns>如果亮度大于阈值，返回true；否则返回false</returns>
        public static bool IsPixelBrightnessGreaterThan(
            this Texture2D texture,
            int x,
            int y,
            float threshold
        )
        {
            // 确保坐标在纹理范围内
            if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            {
                Debug.LogWarning("坐标超出纹理范围");
                return false;
            }

            // 获取指定像素的颜色
            Color pixelColor = texture.GetPixel(x, y);

            // 计算亮度 (使用感知亮度公式)
            float brightness =
                pixelColor.r * 0.299f + pixelColor.g * 0.587f + pixelColor.b * 0.114f;

            // 比较亮度与阈值
            return brightness > threshold;
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

        public static bool IsBlackOrTransparent(this Texture2D texture)
        {
            // 获取纹理的所有像素颜色
            Color[] pixels = texture.GetPixels();

            foreach (var pixel in pixels)
            {
                // 检查像素是否是黑色或透明
                if (pixel.r != 0f || pixel.g != 0f || pixel.b != 0f || pixel.a != 0f)
                {
                    return false; // 如果有一个像素不是黑色或透明，返回 false
                }
            }

            return true; // 如果所有像素都是黑色或透明，返回 true
        }
    }
}
