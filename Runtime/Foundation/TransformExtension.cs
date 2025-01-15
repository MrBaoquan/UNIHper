using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UNIHper
{
    public static class TransformExtension
    {
        public static Vector2 ConvertScreenPointToAnchoredPosition(
            this RectTransform transform,
            Vector2 screenPos
        )
        {
            Vector2 localPoint; // 存储转换后的本地坐标
            var canvas = transform.GetComponentInParent<Canvas>();
            // 调用 RectTransformUtility 的转换方法
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform, // 父级的 RectTransform
                screenPos, // 屏幕坐标
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, // 渲染用相机
                out localPoint // 输出结果
            );

            return localPoint; // 本地坐标即为 Anchored 坐标
        }

        public static void EnableDragMove(
            this RectTransform transform,
            Action<PointerEventData> onBeginDrag = null,
            Action<PointerEventData> onDrag = null,
            Action<PointerEventData> onEndDrag = null
        )
        {
            var _deltaPos = Vector2.zero;
            transform
                .AddComponent<ObservableBeginDragTrigger>()
                .OnBeginDragAsObservable()
                .Subscribe(_ =>
                {
                    var _anchoredPos = transform.ConvertScreenPointToAnchoredPosition(_.position);
                    _deltaPos = transform.anchoredPosition - _anchoredPos;

                    onBeginDrag?.Invoke(_);
                });

            transform
                .AddComponent<ObservableDragTrigger>()
                .OnDragAsObservable()
                .Subscribe(_ =>
                {
                    var _anchoredPos = transform.ConvertScreenPointToAnchoredPosition(_.position);
                    transform.anchoredPosition = _anchoredPos + _deltaPos;
                    onDrag?.Invoke(_);
                });

            transform
                .AddComponent<ObservableEndDragTrigger>()
                .OnEndDragAsObservable()
                .Subscribe(_ =>
                {
                    var _anchoredPos = transform.ConvertScreenPointToAnchoredPosition(_.position);
                    transform.anchoredPosition = _anchoredPos + _deltaPos;

                    onEndDrag?.Invoke(_);
                });
        }
    }
}
