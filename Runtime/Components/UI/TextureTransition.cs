using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using UnityEngine.Events;

namespace UNIHper
{
    public class TextureTransition : MonoBehaviour
    {
        public enum TransitionType
        {
            None = 0, // 无过渡，直接显示目标
            Fade = 1, // 淡入淡出
            Black = 2, // 黑色过渡
            White = 3, // 白色过渡
            Horizontal = 4, // 水平切换
            Vertical = 5, // 垂直切换
            Circle = 6, // 圆形扩展
            Zoom = 7 // 缩放渐变
        }

        public Texture CurrentTexture { get; private set; }

        public Texture TargetTexture { get; private set; }

        private float fade = 0f;
        private TransitionType transitionType = TransitionType.Fade;
        private float duration = 1f;
        private Ease ease = Ease.InOutCubic;

        private Material material;
        private Tween currentTween;

        private static readonly int FadeID = Shader.PropertyToID("_Fade");
        private static readonly int FromTexID = Shader.PropertyToID("_FromTex");
        private static readonly int ToTexID = Shader.PropertyToID("_ToTex");
        private static readonly int TransitionTypeID = Shader.PropertyToID("_TransitionType");

        private void initMat()
        {
            if (material != null)
                return;
            material = new Material(Shader.Find("UNIHper/Unlit/TextureTransition"));
            if (GetComponent<RawImage>())
            {
                var _rawImage = GetComponent<RawImage>();
                _rawImage.material = material;
                TargetTexture = _rawImage.texture;
            }
            else if (GetComponent<Image>() != null)
            {
                var _image = GetComponent<Image>();
                _image.material = material;
                TargetTexture = _image.sprite.texture;
            }

            material.SetTexture(FromTexID, TargetTexture);
            fade = 0f;
            material.SetFloat(FadeID, fade);
        }

        void Awake()
        {
            initMat();
        }

        /// <summary>
        /// 立即设置过渡类型（0-7）
        /// </summary>
        public TextureTransition SetTransitionType(TransitionType type)
        {
            transitionType = type;
            material?.SetInt(TransitionTypeID, (int)transitionType);
            return this;
        }

        /// <summary>
        /// 当前过渡是否在进行中
        /// </summary>
        public bool IsTransitioning => currentTween != null && currentTween.IsActive();

        public TextureTransition SetEase(Ease easeType)
        {
            this.ease = easeType;
            return this;
        }

        public TextureTransition SetDuration(float duration)
        {
            this.duration = duration;
            return this;
        }

        /// <summary>
        /// 切换到新纹理，自动处理from与to纹理与过渡动画
        /// 如果还在过渡，会终止当前过渡并接续新过渡。
        /// </summary>
        /// <param name="newTexture">目标纹理</param>
        /// <param name="onComplete">过渡完成回调</param>
        public void TransitionTo(Texture newTexture, Action onComplete = null)
        {
            if (material == null)
                initMat();

            if (newTexture == null)
            {
                Debug.LogWarning("TransitionTo: newTexture 不能为空");
                return;
            }

            // // 如果目标纹理和当前目标纹理相同，避免多余过渡
            // if (targetTexture == newTexture && IsTransitioning)
            // {
            //     // 已在梯度变化中，不再重复启动过渡
            //     return;
            // }

            // 停止当前过渡
            currentTween?.Kill();

            // 当前材质FromTex设置为之前的MainTex
            CurrentTexture = TargetTexture != null ? TargetTexture : Texture2D.whiteTexture;

            TargetTexture = newTexture;
            material.SetTexture(FromTexID, CurrentTexture);
            material.SetTexture(ToTexID, TargetTexture);

            // 重置fade
            fade = 0f;
            material.SetFloat(FadeID, fade);

            // 启动fade过渡动画
            currentTween = DOTween
                .To(
                    () => fade,
                    x =>
                    {
                        fade = x;
                        material.SetFloat(FadeID, fade);
                    },
                    1f,
                    duration
                )
                .SetEase(ease)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 可选暴露立即停止当前过渡
        /// </summary>
        public void StopTransition()
        {
            currentTween?.Kill();
            currentTween = null;
        }
    }
}
