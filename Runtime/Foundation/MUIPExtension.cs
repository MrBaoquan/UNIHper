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

        public static IObservable<float> OnValueChangedAsObservable(
            this SliderManager _sliderManager
        )
        {
            return _sliderManager.mainSlider.onValueChanged.AsObservable();
        }
    }
}
