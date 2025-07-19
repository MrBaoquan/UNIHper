using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class TextureTransition : MonoBehaviour
{
    // [SerializeField]
    public Texture CurrentTexture { get; private set; }

    // [SerializeField]
    public Texture TargetTexture { get; private set; }

    private float fade = 0f;

    private int transitionType = 1;

    private float duration = 1f;

    private Ease ease = Ease.InOutCubic;

    private RawImage rawImage;
    private Material material;

    private Tween currentTween;

    private static readonly int FadeID = Shader.PropertyToID("_Fade");
    private static readonly int FromTexID = Shader.PropertyToID("_FromTex");
    private static readonly int ToTexID = Shader.PropertyToID("_ToTex");
    private static readonly int TransitionTypeID = Shader.PropertyToID("_TransitionType");

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        material = Instantiate(rawImage.material);
        rawImage.material = material;

        TargetTexture = rawImage.texture;
        material.SetTexture(FromTexID, TargetTexture);
        fade = 0f;
        material.SetFloat(FadeID, fade);
    }

    /// <summary>
    /// 立即设置过渡类型（0-7）
    /// </summary>
    public void SetTransitionType(int type)
    {
        transitionType = type;
        material.SetInt(TransitionTypeID, transitionType);
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

    /// <summary>
    /// 切换到新纹理，自动处理from与to纹理与过渡动画
    /// 如果还在过渡，会终止当前过渡并接续新过渡。
    /// </summary>
    /// <param name="newTexture">目标纹理</param>
    /// <param name="onComplete">过渡完成回调</param>
    public void TransitionTo(Texture newTexture, Action onComplete = null)
    {
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
