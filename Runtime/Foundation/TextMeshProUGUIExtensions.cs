using System;
using TMPro;
using UniRx;
using UnityEngine;

public static class TextMeshProUGUIExtensions
{
    /// <summary>
    /// 为文本应用动画点效果 (例如: "请稍等." -> "请稍等.." -> "请稍等..." -> ...)
    /// </summary>
    /// <param name="text">目标文本组件</param>
    /// <param name="baseText">基础文本内容（不含点）</param>
    /// <param name="minDots">最小点数，默认1</param>
    /// <param name="maxDots">最大点数，默认5</param>
    /// <param name="intervalSeconds">点变化间隔（秒），默认0.5秒</param>
    /// <returns>返回 IDisposable，可用于停止动画</returns>
    public static IDisposable PlayLoadingDots(
        this TextMeshProUGUI text,
        string baseText,
        int minDots = 1,
        int maxDots = 5,
        float intervalSeconds = 0.5f
    )
    {
        if (text == null)
        {
            Debug.LogError("TextMeshProUGUI is null!");
            return Disposable.Empty;
        }

        if (minDots < 0)
            minDots = 0;
        if (maxDots < minDots)
            maxDots = minDots;

        int dotRange = maxDots - minDots + 1;
        int currentDotCount = minDots;

        // 使用 Observable.Interval 创建定时器
        return Observable
            .Interval(TimeSpan.FromSeconds(intervalSeconds))
            .Subscribe(_ =>
            {
                if (text == null)
                    return;

                // 生成点字符串
                string dots = new string('.', currentDotCount);
                text.text = baseText + dots;

                // 循环增加点数
                currentDotCount++;
                if (currentDotCount > maxDots)
                {
                    currentDotCount = minDots;
                }
            });
    }
}
