using DG.Tweening;
using UnityEngine.UI;

public static class DOTweenExtension
{
    public static Tweener DOText(this Text text, string startText, string endText, float duration)
    {
        text.text = startText;
        return text.DOText(endText, duration);
    }
}
