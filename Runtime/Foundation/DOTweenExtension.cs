using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class DOTweenExtension
{
    public static Tweener DOText(this Text text, string startText, string endText, float duration)
    {
        text.text = startText;
        return text.DOText(endText, duration);
    }

    public static void FadeSwitchSprite(
        this Image image,
        Sprite newSprite,
        float duration,
        Action onComplete = null
    )
    {
        if (image == null || newSprite == null)
        {
            Debug.LogWarning("Image 或 Sprite 不能为空");
            return;
        }

        // 使用 DOTween 把图片透明度淡出到 0
        image
            .DOFade(0f, duration / 2)
            .OnComplete(() =>
            {
                // 更换图片
                image.sprite = newSprite;
                image.SetNativeSize();
                // 把透明度淡入到 1
                image
                    .DOFade(1f, duration / 2)
                    .OnComplete(() =>
                    {
                        onComplete?.Invoke();
                    });
            });
    }
}
