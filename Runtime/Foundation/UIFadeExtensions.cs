using System;
using UnityEngine;
using DG.Tweening;
using UniRx;

namespace UNIHper
{
    /// <summary>
    /// UI 渐变动画扩展方法
    /// </summary>
    public static class UIFadeExtensions
    {
        #region FadeIn - 渐变显示

        /// <summary>
        /// 渐变显示元素 (先激活，alpha从0渐变到1)
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="duration">动画时长，默认0.5秒</param>
        /// <param name="ease">缓动类型</param>
        /// <returns>Tween对象，可用于链式调用</returns>
        public static Tween FadeIn(this GameObject target, float duration = 0.5f, Ease ease = Ease.OutQuad)
        {
            return target.transform.FadeIn(duration, ease);
        }

        /// <summary>
        /// 渐变显示元素 (先激活，alpha从0渐变到1)
        /// </summary>
        public static Tween FadeIn(this Transform target, float duration = 0.5f, Ease ease = Ease.OutQuad)
        {
            // 先取消该对象上正在进行的渐变动画
            var canvasGroup = target.GetOrAddComponent<CanvasGroup>();
            DOTween.Kill(canvasGroup);

            target.gameObject.SetActive(true);
            canvasGroup.alpha = 0;
            return canvasGroup.DOFade(1f, duration).SetEase(ease).SetTarget(canvasGroup);
        }

        /// <summary>
        /// 渐变显示元素 (先激活，alpha从0渐变到1)
        /// </summary>
        public static Tween FadeIn(this Component target, float duration = 0.5f, Ease ease = Ease.OutQuad)
        {
            return target.transform.FadeIn(duration, ease);
        }

        /// <summary>
        /// 渐变显示元素，返回Observable (动画完成后发出信号)
        /// </summary>
        public static IObservable<Unit> FadeInAsObservable(this GameObject target, float duration = 0.5f, Ease ease = Ease.OutQuad)
        {
            return target.transform.FadeInAsObservable(duration, ease);
        }

        /// <summary>
        /// 渐变显示元素，返回Observable (动画完成后发出信号)
        /// </summary>
        public static IObservable<Unit> FadeInAsObservable(this Transform target, float duration = 0.5f, Ease ease = Ease.OutQuad)
        {
            return Observable.Create<Unit>(observer =>
            {
                var tween = target.FadeIn(duration, ease);
                tween.OnComplete(() =>
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                });

                return Disposable.Create(() => tween.Kill());
            });
        }

        /// <summary>
        /// 渐变显示元素，返回Observable (动画完成后发出信号)
        /// </summary>
        public static IObservable<Unit> FadeInAsObservable(this Component target, float duration = 0.5f, Ease ease = Ease.OutQuad)
        {
            return target.transform.FadeInAsObservable(duration, ease);
        }

        #endregion

        #region FadeOut - 渐变隐藏

        /// <summary>
        /// 渐变隐藏元素 (alpha从当前值渐变到0，然后隐藏)
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="duration">动画时长，默认0.5秒</param>
        /// <param name="ease">缓动类型</param>
        /// <returns>Tween对象，可用于链式调用</returns>
        public static Tween FadeOut(this GameObject target, float duration = 0.5f, Ease ease = Ease.InQuad)
        {
            return target.transform.FadeOut(duration, ease);
        }

        /// <summary>
        /// 渐变隐藏元素 (alpha从当前值渐变到0，然后隐藏)
        /// </summary>
        public static Tween FadeOut(this Transform target, float duration = 0.5f, Ease ease = Ease.InQuad)
        {
            // 先取消该对象上正在进行的渐变动画
            var canvasGroup = target.GetOrAddComponent<CanvasGroup>();
            DOTween.Kill(canvasGroup);

            return canvasGroup
                .DOFade(0f, duration)
                .SetEase(ease)
                .SetTarget(canvasGroup)
                .OnComplete(() => target.gameObject.SetActive(false));
        }

        /// <summary>
        /// 渐变隐藏元素 (alpha从当前值渐变到0，然后隐藏)
        /// </summary>
        public static Tween FadeOut(this Component target, float duration = 0.5f, Ease ease = Ease.InQuad)
        {
            return target.transform.FadeOut(duration, ease);
        }

        /// <summary>
        /// 渐变隐藏元素，返回Observable (动画完成后发出信号)
        /// </summary>
        public static IObservable<Unit> FadeOutAsObservable(this GameObject target, float duration = 0.5f, Ease ease = Ease.InQuad)
        {
            return target.transform.FadeOutAsObservable(duration, ease);
        }

        /// <summary>
        /// 渐变隐藏元素，返回Observable (动画完成后发出信号)
        /// </summary>
        public static IObservable<Unit> FadeOutAsObservable(this Transform target, float duration = 0.5f, Ease ease = Ease.InQuad)
        {
            return Observable.Create<Unit>(observer =>
            {
                var tween = target.FadeOut(duration, ease);
                tween.OnComplete(() =>
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                });

                return Disposable.Create(() => tween.Kill());
            });
        }

        /// <summary>
        /// 渐变隐藏元素，返回Observable (动画完成后发出信号)
        /// </summary>
        public static IObservable<Unit> FadeOutAsObservable(this Component target, float duration = 0.5f, Ease ease = Ease.InQuad)
        {
            return target.transform.FadeOutAsObservable(duration, ease);
        }

        #endregion

        #region Helper

        /// <summary>
        /// 获取或添加组件
        /// </summary>
        private static T GetOrAddComponent<T>(this Transform target)
            where T : Component
        {
            var component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.gameObject.AddComponent<T>();
            }
            return component;
        }

        #endregion
    }
}
