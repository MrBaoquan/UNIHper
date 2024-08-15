using System;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    using UniRx.Triggers;
    using UniRx;
    using UnityEngine.EventSystems;
    using System.Collections.Generic;
    using System.Linq;
    using DNHper;

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

        public static IObservable<Unit> OnClickAsObservable(
            this Button button,
            float throttleSeconds
        )
        {
            return button.onClick
                .AsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(throttleSeconds));
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

        public static Toggle WithSound(this Toggle toggle)
        {
            toggle
                .OnValueChangedAsObservable()
                .Subscribe(_ =>
                {
                    Managements.Audio.PlayEffect(UNIHperSettings.DefaultClickSound);
                })
                .AddTo(toggle);
            return toggle;
        }

        public static Toggle WithSound(this Toggle toggle, string soundName)
        {
            toggle
                .OnValueChangedAsObservable()
                .Subscribe(_ =>
                {
                    Managements.Audio.PlayEffect(soundName);
                })
                .AddTo(toggle);
            return toggle;
        }

        public static Button WithAnimation(
            this Button button,
            string stateName,
            Animator animator = null
        )
        {
            button
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (animator == null)
                        animator = button.GetComponent<Animator>();
                    if (animator == null)
                    {
                        Debug.LogWarning($"Animator not found on {button.name}");
                        return;
                    }
                    animator.Play(stateName);
                })
                .AddTo(button);
            return button;
        }

        public static void PropagatePointerClick(
            this MonoBehaviour behaviour,
            PointerEventData eventData
        ) => PropagatePointerEvent(behaviour, eventData, ExecuteEvents.pointerClickHandler);

        public static void PropagateBeginDrag(
            this MonoBehaviour behaviour,
            PointerEventData eventData
        ) => PropagatePointerEvent(behaviour, eventData, ExecuteEvents.beginDragHandler);

        public static void PropagateDrag(
            this MonoBehaviour behaviour,
            PointerEventData eventData
        ) => PropagatePointerEvent(behaviour, eventData, ExecuteEvents.dragHandler);

        public static void PropagateEndDrag(
            this MonoBehaviour behaviour,
            PointerEventData eventData
        ) => PropagatePointerEvent(behaviour, eventData, ExecuteEvents.endDragHandler);

        private static readonly List<RaycastResult> s_raycastResults = new();

        public static GameObject NextPropagateEventTarget(
            this MonoBehaviour behaviour,
            PointerEventData eventData
        )
        {
            EventSystem.current.RaycastAll(eventData, s_raycastResults);

            var gameObject = behaviour.gameObject;
            int index = s_raycastResults.FindIndex(result => result.gameObject == gameObject) + 1;
            if (index >= s_raycastResults.Count)
                return null;
            return s_raycastResults[index].gameObject;
        }

        public static void PropagatePointerEvent<T>(
            MonoBehaviour behaviour,
            PointerEventData eventData,
            ExecuteEvents.EventFunction<T> eventFunction
        )
            where T : IEventSystemHandler
        {
            EventSystem.current.RaycastAll(eventData, s_raycastResults);

            var gameObject = behaviour.gameObject;
            int index = s_raycastResults.FindIndex(result => result.gameObject == gameObject) + 1;
            if (index >= s_raycastResults.Count)
                return;

            ExecuteEvents.Execute(s_raycastResults[index].gameObject, eventData, eventFunction);
        }
    }
}
