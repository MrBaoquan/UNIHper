using System;
using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;

namespace UNIHper
{
    public static class TextureExtension
    {
        //
        // Summary:
        //     Crops a Texture2D into a new Texture2D.
        public static Texture2D CropTexture(this Texture texture, Rect source)
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture renderTexture = (
                RenderTexture.active = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default,
                    8
                )
            );
            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            GL.Clear(clearDepth: false, clearColor: true, new Color(1f, 1f, 1f, 0f));
            Graphics.Blit(texture, renderTexture);
            Texture2D texture2D = new Texture2D((int)source.width, (int)source.height, TextureFormat.ARGB32, mipChain: true, linear: false);
            texture2D.filterMode = FilterMode.Point;
            texture2D.ReadPixels(source, 0, 0);
            texture2D.Apply();
            GL.sRGBWrite = sRGBWrite;
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(renderTexture);
            return texture2D;
        }

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

        // 将 Texture2D 拷贝到 RenderTexture，可以传入现有的 RenderTexture 或者自动创建一个新的
        public static RenderTexture ToRenderTexture(this Texture2D source, RenderTexture target = null)
        {
            // 如果没有传入目标 RenderTexture，则自动创建一个新的
            if (target == null)
            {
                target = new RenderTexture(source.width, source.height, 24);
            }

            // 确保 RenderTexture 是激活状态
            RenderTexture.active = target;

            // 使用 Graphics.Blit 将源纹理拷贝到目标 RenderTexture
            Graphics.Blit(source, target);

            // 恢复 RenderTexture.active 为 null
            RenderTexture.active = null;

            // 返回目标 RenderTexture
            return target;
        }

        public static Texture2D Clone(this Texture2D source)
        {
            // 创建一个新的Texture2D对象，它具有相同的宽度、高度和格式
            Texture2D newTexture = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1);
            // 复制原始纹理的像素到新纹理
            Graphics.CopyTexture(source, newTexture);
            // 应用像素更改
            newTexture.Apply();
            return newTexture;
        }

        public static Texture2D Resize(this Texture texture, int width, int height, FilterMode filterMode = FilterMode.Bilinear)
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
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: true, linear: false);
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
            squareTexture.SetPixels(xOffset, yOffset, originalWidth, originalHeight, source.GetPixels());

            // 应用更改
            squareTexture.Apply();

            return squareTexture;
        }

        public static List<Vector2Int> GetPixelsAboveBrightness(this Texture2D texture, float brightnessThreshold)
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
        public static bool IsPixelBrightnessGreaterThan(this Texture2D texture, int x, int y, float threshold)
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
            float brightness = pixelColor.r * 0.299f + pixelColor.g * 0.587f + pixelColor.b * 0.114f;

            // 比较亮度与阈值
            return brightness > threshold;
        }

        public static Texture2D AddPadding(this Texture2D source, int padding, Color fillColor = default(Color))
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
            paddedTexture.SetPixels(xOffset, yOffset, originalWidth, originalHeight, source.GetPixels());

            // 应用更改
            paddedTexture.Apply();

            return paddedTexture;
        }

        public static Sprite ToSprite(this Texture2D texture2D)
        {
            return texture2D.ToSprite(new Rect(0, 0, texture2D.width, texture2D.height), Vector2.one * 0.5f);
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
            var _newSp = Sprite.Create(texture2D, rect, pivot);
            _newSp.name = texture2D.name;
            return _newSp;
        }

        public static bool SaveToFile(this Texture2D texture2D, string savePath)
        {
            try
            {
                var _bytes = texture2D.EncodeToPNG();
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

        public static void Clear(this Texture2D texture, Color color)
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
        }

        /// <summary>
        /// 在目标纹理的指定像素坐标处绘制一个指定的画笔纹理。
        /// 画笔纹理将以给定的缩放比例进行缩放，并以画笔中心与目标坐标对齐。
        /// 此方法内部做了边界判断，避免因绘制区域超出目标纹理边界而报错。
        /// 其中使用简单的 Alpha 混合，若画笔像素 alpha 值低 (<0.01) 则不进行绘制。
        /// </summary>
        /// <param name="canvas">目标纹理（必须是可读写的）</param>
        /// <param name="brush">需要绘制的画笔纹理</param>
        /// <param name="centerX">目标纹理中绘制中心点 X 坐标</param>
        /// <param name="centerY">目标纹理中绘制中心点 Y 坐标</param>
        /// <param name="scale">画笔纹理的缩放比例（1 表示原始大小，大于 1 放大，小于 1 缩小）</param>
        public static void DrawTexture2D(this Texture2D canvas, Texture2D brush, int centerX, int centerY, float scale)
        {
            // 计算按缩放后画笔纹理的尺寸（四舍五入取整像素）
            int brushScaledWidth = Mathf.RoundToInt(brush.width * scale);
            int brushScaledHeight = Mathf.RoundToInt(brush.height * scale);

            // 计算绘制时在目标纹理上的起始像素（以画笔中心对齐目标中心点）
            int startX = centerX - brushScaledWidth / 2;
            int startY = centerY - brushScaledHeight / 2;

            // 遍历缩放后的画笔纹理的每个像素
            for (int x = 0; x < brushScaledWidth; x++)
            {
                for (int y = 0; y < brushScaledHeight; y++)
                {
                    // 目标纹理对应的像素坐标
                    int canvasX = startX + x;
                    int canvasY = startY + y;

                    // 检查是否在目标纹理范围内（防止越界）
                    if (canvasX < 0 || canvasY < 0 || canvasX >= canvas.width || canvasY >= canvas.height)
                        continue;

                    // 计算在画笔纹理中的归一化坐标（使用 GetPixelBilinear 可实现缩放后平滑采样）
                    float u = (float)x / (float)brushScaledWidth;
                    float v = (float)y / (float)brushScaledHeight;

                    // 从画笔纹理采样颜色
                    Color brushColor = brush.GetPixelBilinear(u, v);

                    // 如果画笔像素几乎全透明，则跳过绘制
                    if (brushColor.a < 0.01f)
                        continue;

                    // 从目标纹理获取原始颜色
                    Color canvasColor = canvas.GetPixel(canvasX, canvasY);
                    // 简单使用 blend 方式进行混合：根据画笔像素的 alpha 值进行线性插值
                    Color finalColor = Color.Lerp(canvasColor, brushColor, brushColor.a);
                    canvas.SetPixel(canvasX, canvasY, finalColor);
                }
            }
            // 将所有 SetPixel 调用应用到纹理上
            canvas.Apply();
        }

        /// <summary>
        /// 在Texture2D上绘制一个圆点
        /// </summary>
        /// <param name="texture">目标纹理，需要是可读写的Texture2D</param>
        /// <param name="centerX">圆心X（像素坐标）</param>
        /// <param name="centerY">圆心Y（像素坐标）</param>
        /// <param name="radius">圆半径（像素）</param>
        /// <param name="color">绘制颜色</param>
        public static void DrawCircle(this Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            if (texture == null)
            {
                Debug.LogError("DrawCircle: texture is null");
                return;
            }

            int texWidth = texture.width;
            int texHeight = texture.height;

            // 遍历圆心附近的矩形区域，以减少计算量
            int xStart = Mathf.Max(centerX - radius, 0);
            int xEnd = Mathf.Min(centerX + radius, texWidth - 1);
            int yStart = Mathf.Max(centerY - radius, 0);
            int yEnd = Mathf.Min(centerY + radius, texHeight - 1);

            int radiusSquared = radius * radius;

            // 遍历目标区域的像素
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    // 计算当前像素和圆心的距离平方
                    int dx = x - centerX;
                    int dy = y - centerY;
                    int distSquared = dx * dx + dy * dy;

                    if (distSquared <= radiusSquared)
                    {
                        // 该像素在圆内，绘制颜色
                        texture.SetPixel(x, y, color);
                    }
                }
            }

            texture.Apply();
        }

        /// <summary>
        /// 镜像Texture2D，支持水平和垂直镜像，可同时进行
        /// </summary>
        /// <param name="texture">目标纹理，必须是可读写的</param>
        /// <param name="bHorizontal">是否水平镜像（左右翻转）</param>
        /// <param name="bVertical">是否垂直镜像（上下翻转）</param>
        /// <returns>返回镜像后的纹理自身</returns>
        public static Texture2D Mirror(this Texture2D texture, bool bHorizontal, bool bVertical)
        {
            if (texture == null)
            {
                Debug.LogError("Mirror: texture is null");
                return null;
            }

            int width = texture.width;
            int height = texture.height;
            Color[] pixels = texture.GetPixels();
            Color[] mirroredPixels = new Color[pixels.Length];

            for (int y = 0; y < height; y++)
            {
                int newY = bVertical ? (height - 1 - y) : y;

                for (int x = 0; x < width; x++)
                {
                    int newX = bHorizontal ? (width - 1 - x) : x;

                    mirroredPixels[newY * width + newX] = pixels[y * width + x];
                }
            }

            texture.SetPixels(mirroredPixels);
            texture.Apply();

            return texture;
        }

        /// <summary>
        /// 在Texture2D上绘制矩形，支持边框和填充，绘制完成返回该纹理自身
        /// </summary>
        /// <param name="texture">目标纹理，要求可读写</param>
        /// <param name="x">矩形左下角X坐标（像素）</param>
        /// <param name="y">矩形左下角Y坐标（像素）</param>
        /// <param name="width">矩形宽度（像素）</param>
        /// <param name="height">矩形高度（像素）</param>
        /// <param name="color">绘制颜色</param>
        /// <param name="lineWidth">线宽（像素），用于绘制边框时，默认为1</param>
        /// <param name="filled">是否填充矩形，true为填充，false为只绘制边框</param>
        /// <returns>返回绘制后的Texture2D本身</returns>
        public static Texture2D DrawRect(
            this Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            Color color,
            int lineWidth = 1,
            bool filled = false
        )
        {
            if (texture == null)
            {
                Debug.LogError("DrawRect: texture is null");
                return null;
            }

            int texWidth = texture.width;
            int texHeight = texture.height;

            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning("DrawRect: width and height must be positive");
                return texture;
            }

            if (lineWidth < 1)
                lineWidth = 1;

            int xMin = Mathf.Clamp(x, 0, texWidth - 1);
            int yMin = Mathf.Clamp(y, 0, texHeight - 1);
            int xMax = Mathf.Clamp(x + width, 0, texWidth);
            int yMax = Mathf.Clamp(y + height, 0, texHeight);

            if (filled)
            {
                for (int py = yMin; py < yMax; py++)
                {
                    for (int px = xMin; px < xMax; px++)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
            else
            {
                for (int lw = 0; lw < lineWidth; lw++)
                {
                    int yTop = yMax - 1 - lw;
                    int yBottom = yMin + lw;

                    if (yTop >= 0 && yTop < texHeight)
                    {
                        for (int px = xMin; px < xMax; px++)
                        {
                            texture.SetPixel(px, yTop, color);
                        }
                    }

                    if (yBottom >= 0 && yBottom < texHeight)
                    {
                        for (int px = xMin; px < xMax; px++)
                        {
                            texture.SetPixel(px, yBottom, color);
                        }
                    }
                }

                for (int lw = 0; lw < lineWidth; lw++)
                {
                    int xLeft = xMin + lw;
                    int xRight = xMax - 1 - lw;

                    if (xLeft >= 0 && xLeft < texWidth)
                    {
                        for (int py = yMin; py < yMax; py++)
                        {
                            texture.SetPixel(xLeft, py, color);
                        }
                    }

                    if (xRight >= 0 && xRight < texWidth)
                    {
                        for (int py = yMin; py < yMax; py++)
                        {
                            texture.SetPixel(xRight, py, color);
                        }
                    }
                }
            }

            texture.Apply();

            return texture;
        }

        /// <summary>
        /// 异步将RenderTexture转换为Texture2D，采用AsyncGPUReadback无阻塞读取，返回IObservable
        /// </summary>
        /// <param name="rt">待转换的RenderTexture</param>
        /// <returns>转换成功返回Texture2D</returns>
        public static IObservable<Texture2D> ToTexture2DAsync(this RenderTexture rt, Texture2D texture2D)
        {
            return Observable.Create<Texture2D>(observer =>
            {
                if (rt == null)
                {
                    observer.OnError(new ArgumentNullException(nameof(rt), "RenderTexture不能为空"));
                    return Disposable.Empty;
                }

                // 对格式做简单限制，后续可扩展多格式支持
                if (
                    rt.format != RenderTextureFormat.ARGB32
                    && rt.format != RenderTextureFormat.Default
                    && rt.format != RenderTextureFormat.DefaultHDR
                )
                {
                    observer.OnError(new NotSupportedException($"当前RenderTexture格式{rt.format}可能不支持直接读回"));
                    return Disposable.Empty;
                }

                // 发起异步GPU读回请求
                AsyncGPUReadback.Request(
                    rt,
                    0,
                    request =>
                    {
                        if (request.hasError)
                        {
                            observer.OnError(new Exception("AsyncGPUReadback读取出现错误"));
                            return;
                        }

                        var data = request.GetData<byte>();

                        // 创建Texture2D，格式固定用RGBA32，可能根据RenderTexture格式调整
                        // texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                        texture2D.LoadRawTextureData(data);
                        texture2D.Apply();

                        observer.OnNext(texture2D);
                        observer.OnCompleted();
                    }
                );

                // 无需特别清理动作，返回空Disposable
                return Disposable.Empty;
            });
        }
    }
}
