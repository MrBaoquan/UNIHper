using System;
using UniRx;
using Michsky.MUIP;

namespace UNIHper
{
    public static class MUIPExtnesion
    {
        public static IObservable<Unit> OnClickAsObservable(this ButtonManager _buttonManager)
        {
            return _buttonManager.onClick.AsObservable();
        }
    }
}
