using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    using DG.Tweening;

    public static class AnimUtils
    {
        public static void FadeTo(
            this Image image,
            Sprite target,
            float duration = 0.45f,
            TextureTransition.TransitionType transitionType = TextureTransition.TransitionType.Fade,
            Ease easeType = Ease.InOutQuint
        )
        {
            image
                .GetOrAdd<TextureTransition>()
                .SetTransitionType(transitionType)
                .SetEase(easeType)
                .SetDuration(duration)
                .TransitionTo(target.texture);
        }

        public static void FadeTo(
            this RawImage image,
            Texture target,
            float duration = 0.45f,
            TextureTransition.TransitionType transitionType = TextureTransition.TransitionType.Fade,
            Ease easeType = Ease.InOutQuint
        )
        {
            image
                .GetOrAdd<TextureTransition>()
                .SetTransitionType(transitionType)
                .SetEase(easeType)
                .SetDuration(duration)
                .TransitionTo(target);
        }
    }
}
