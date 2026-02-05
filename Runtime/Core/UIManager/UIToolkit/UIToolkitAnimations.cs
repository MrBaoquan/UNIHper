using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UNIHper.UI
{
    /// <summary>
    /// UI Toolkit 动画基类
    /// 提供常用的 UI 动画效果
    /// </summary>
    public static class UIToolkitAnimations
    {
        #region Fade Animations

        /// <summary>
        /// 淡入动画
        /// </summary>
        public static async Task FadeIn(VisualElement element, float duration, CancellationToken cancellationToken)
        {
            element.style.opacity = 0;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                element.style.opacity = t;

                await Task.Yield();
            }

            element.style.opacity = 1;
        }

        /// <summary>
        /// 淡出动画
        /// </summary>
        public static async Task FadeOut(VisualElement element, float duration, CancellationToken cancellationToken)
        {
            element.style.opacity = 1;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Clamp01(elapsed / duration);
                element.style.opacity = t;

                await Task.Yield();
            }

            element.style.opacity = 0;
        }

        #endregion

        #region Scale Animations

        /// <summary>
        /// 缩放进入动画
        /// </summary>
        public static async Task ScaleIn(VisualElement element, float duration, CancellationToken cancellationToken)
        {
            element.style.scale = new Scale(Vector2.zero);
            element.style.opacity = 0;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                // 使用 EaseOutBack 缓动
                var scale = EaseOutBack(t);
                element.style.scale = new Scale(new Vector2(scale, scale));
                element.style.opacity = t;

                await Task.Yield();
            }

            element.style.scale = new Scale(Vector2.one);
            element.style.opacity = 1;
        }

        /// <summary>
        /// 缩放退出动画
        /// </summary>
        public static async Task ScaleOut(VisualElement element, float duration, CancellationToken cancellationToken)
        {
            element.style.scale = new Scale(Vector2.one);
            element.style.opacity = 1;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Clamp01(elapsed / duration);

                element.style.scale = new Scale(new Vector2(t, t));
                element.style.opacity = t;

                await Task.Yield();
            }

            element.style.scale = new Scale(Vector2.zero);
            element.style.opacity = 0;
        }

        #endregion

        #region Slide Animations

        /// <summary>
        /// 从底部滑入
        /// </summary>
        public static async Task SlideInFromBottom(
            VisualElement element,
            float distance,
            float duration,
            CancellationToken cancellationToken
        )
        {
            element.style.translate = new Translate(0, distance, 0);
            element.style.opacity = 0;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutCubic(t);

                element.style.translate = new Translate(0, distance * (1 - eased), 0);
                element.style.opacity = t;

                await Task.Yield();
            }

            element.style.translate = new Translate(0, 0, 0);
            element.style.opacity = 1;
        }

        /// <summary>
        /// 向底部滑出
        /// </summary>
        public static async Task SlideOutToBottom(
            VisualElement element,
            float distance,
            float duration,
            CancellationToken cancellationToken
        )
        {
            element.style.translate = new Translate(0, 0, 0);
            element.style.opacity = 1;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseInCubic(t);

                element.style.translate = new Translate(0, distance * eased, 0);
                element.style.opacity = 1 - t;

                await Task.Yield();
            }

            element.style.translate = new Translate(0, distance, 0);
            element.style.opacity = 0;
        }

        /// <summary>
        /// 从右侧滑入
        /// </summary>
        public static async Task SlideInFromRight(
            VisualElement element,
            float distance,
            float duration,
            CancellationToken cancellationToken
        )
        {
            element.style.translate = new Translate(distance, 0, 0);
            element.style.opacity = 0;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutCubic(t);

                element.style.translate = new Translate(distance * (1 - eased), 0, 0);
                element.style.opacity = t;

                await Task.Yield();
            }

            element.style.translate = new Translate(0, 0, 0);
            element.style.opacity = 1;
        }

        #endregion

        #region Easing Functions

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        private static float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        #endregion
    }
}
