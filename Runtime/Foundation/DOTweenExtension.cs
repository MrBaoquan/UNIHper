using System;
using DG.Tweening;
using UNIHper;
using UnityEngine;
using UnityEngine.UI;

public class DOTweenCache : MonoBehaviour
{
    public Vector3 OriginalLocalPosition { get; private set; }

    public DOTweenCache Record()
    {
        OriginalLocalPosition = transform.localPosition;
        return this;
    }

    private void Awake()
    {
        OriginalLocalPosition = transform.localPosition;
    }
}

public static class DOTweenExtension
{
    /// <summary>
    /// 移动方向枚举
    /// </summary>
    public enum MoveDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public static void FadeWithMove(
        this RawImage rawImage,
        Texture newTexture,
        float duration = 0.45f,
        float moveDistance = 100f,
        MoveDirection direction = MoveDirection.Left,
        Action onComplete = null
    )
    {
        if (rawImage == null || newTexture == null)
        {
            Debug.LogWarning("RawImage 或 Texture 不能为空");
            return;
        }

        var cache = rawImage.GetOrAdd<DOTweenCache>().Record();
        RectTransform rectTransform = rawImage.rectTransform;

        rectTransform.DOKill();

        var canvasGroup = rawImage.GetOrAdd<CanvasGroup>();
        canvasGroup.DOKill();

        float inDuration = duration * 2f / 3f;
        float outDuration = duration / 3f;

        Vector3 targetPosition = cache.OriginalLocalPosition;
        switch (direction)
        {
            case MoveDirection.Left:
                targetPosition += Vector3.left * moveDistance;
                break;
            case MoveDirection.Right:
                targetPosition += Vector3.right * moveDistance;
                break;
            case MoveDirection.Up:
                targetPosition += Vector3.up * moveDistance;
                break;
            case MoveDirection.Down:
                targetPosition += Vector3.down * moveDistance;
                break;
        }

        Sequence seq = DOTween.Sequence();

        seq.Append(rectTransform.DOLocalMove(targetPosition, inDuration).SetEase(Ease.InOutQuad));
        seq.Join(canvasGroup.DOFade(0f, inDuration));

        seq.AppendCallback(() =>
        {
            rawImage.texture = newTexture;
            rawImage.SetNativeSize();
        });

        seq.Append(
            rectTransform
                .DOLocalMove(cache.OriginalLocalPosition, outDuration)
                .SetEase(Ease.InOutQuad)
        );
        seq.Join(canvasGroup.DOFade(1f, outDuration));

        seq.OnComplete(() => onComplete?.Invoke());
        seq.Play();
    }

    public static Tweener DOText(this Text text, string startText, string endText, float duration)
    {
        text.text = startText;
        return text.DOText(endText, duration);
    }

    public static void FadeTo(
        this Graphic graphic,
        UnityEngine.Object newAsset,
        float duration = 0.5f,
        Action onComplete = null
    )
    {
        if (graphic == null || newAsset == null)
        {
            Debug.LogWarning("Graphic 和 newAsset 都不能为空");
            return;
        }

        CanvasGroup canvasGroup = graphic.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = graphic.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1;
        }
        canvasGroup.DOKill();

        var image = graphic as Image;
        var rawImage = graphic as RawImage;

        if (image == null && rawImage == null)
        {
            Debug.LogWarning("仅支持 Image 和 RawImage 组件");
            return;
        }

        if (image != null && !(newAsset is Sprite))
        {
            Debug.LogWarning("Image 组件需要传入 Sprite 类型的 newAsset");
            return;
        }
        if (rawImage != null && !(newAsset is Texture))
        {
            Debug.LogWarning("RawImage 组件需要传入 Texture 类型的 newAsset");
            return;
        }

        float halfDuration = duration / 2f;

        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(0f, halfDuration));

        seq.AppendCallback(() =>
        {
            if (image != null)
            {
                image.sprite = (Sprite)newAsset;
                image.SetNativeSize();
            }
            else if (rawImage != null)
            {
                rawImage.texture = (Texture)newAsset;
                rawImage.SetNativeSize();
            }
        });

        seq.Append(canvasGroup.DOFade(1f, halfDuration));

        seq.OnComplete(() => onComplete?.Invoke());
        seq.Play();
    }
}
