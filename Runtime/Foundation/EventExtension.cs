using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx.Triggers;
using UniRx;

namespace UNIHper
{
    public static class EventExtension
    {
        public static IObservable<UnityEngine.EventSystems.PointerEventData> OnPointerDownAsObservable(
            this Image component
        )
        {
            if (component.GetComponent<ObservablePointerDownTrigger>() == null)
                component.AddComponent<ObservablePointerDownTrigger>();
            return component
                .GetComponent<ObservablePointerDownTrigger>()
                .OnPointerDownAsObservable();
        }

        public static IObservable<UnityEngine.EventSystems.PointerEventData> OnPointerUpAsObservable(
            this Image component
        )
        {
            if (component.GetComponent<ObservablePointerUpTrigger>() == null)
                component.AddComponent<ObservablePointerUpTrigger>();
            return component.GetComponent<ObservablePointerUpTrigger>().OnPointerUpAsObservable();
        }

        public static IObservable<UnityEngine.EventSystems.PointerEventData> OnPointerClickAsObservable(
            this Image component
        )
        {
            if (component.GetComponent<ObservablePointerClickTrigger>() == null)
                component.AddComponent<ObservablePointerClickTrigger>();
            return component
                .GetComponent<ObservablePointerClickTrigger>()
                .OnPointerClickAsObservable();
        }

        public static Button WithSound(this Button button)
        {
            button
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Managements.Audio.PlayEffect(UNIHperSettings.DefaultClickSound);
                })
                .AddTo(button);
            return button;
        }

        public static Image WithSound(this Image image)
        {
            image
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    Managements.Audio.PlayEffect(UNIHperSettings.DefaultClickSound);
                })
                .AddTo(image);
            return image;
        }

        public static Button WithSound(this Button button, string soundName)
        {
            button
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Managements.Audio.PlayEffect(soundName);
                })
                .AddTo(button);
            return button;
        }

        public static Image WithSound(this Image image, string soundName)
        {
            image
                .OnPointerClickAsObservable()
                .Subscribe(_ =>
                {
                    Managements.Audio.PlayEffect(soundName);
                })
                .AddTo(image);
            return image;
        }
    }
}
