using System;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper
{
    /// <summary>
    /// 索引迭代器 迭代范围 [Min,Max)
    /// </summary>
    public class Indexer
    {
        public bool Loop { get; set; } = true;
        private int maxIndex;
        public int Max
        {
            get => maxIndex;
        }
        private int minIndex = 0;
        public int Min
        {
            get => minIndex;
        }
        private int current = 0;
        public int Current
        {
            get { return current; }
        }

        private UnityEvent<int> onIndexChanged = new UnityEvent<int>();

        public IObservable<int> OnIndexChangedAsObservable()
        {
            return onIndexChanged.AsObservable();
        }

        public Indexer() { }

        public Indexer(int Max)
        {
            SetMax(Max);
        }

        public Indexer(int Min, int Max)
        {
            SetMin(Min);
            SetMax(Max);
        }

        /// <summary>
        /// 设置索引最大值
        /// </summary>
        /// <param name="Max">索引最大值</param>
        public void SetMax(int Max)
        {
            maxIndex = Max;
        }

        /// <summary>
        /// 设置索引最小值
        /// </summary>
        /// <param name="Min">索引最小值</param>
        public void SetMin(int Min)
        {
            minIndex = Min;
        }

        /// <summary>
        /// 设置当前索引值
        /// </summary>
        /// <param name="Current"></param>
        /// <returns></returns>
        public int Set(int Current)
        {
            if (Current < minIndex || Current > maxIndex)
            {
                if (Loop)
                    Current = (int)Mathf.Repeat(Current, maxIndex - minIndex + 1) + Min;
                else
                    Current = Mathf.Clamp(Current, minIndex, maxIndex);
            }
            current = Current;
            onIndexChanged.Invoke(current);
            return current;
        }

        public int Next()
        {
            current = NextValue();
            onIndexChanged.Invoke(current);
            return current;
        }

        public int Prev()
        {
            current = PrevValue();
            onIndexChanged.Invoke(current);
            return current;
        }

        /// <summary>
        /// 将索引置为第一个
        /// </summary>
        /// <returns></returns>
        public int SetToMin()
        {
            current = Min;
            onIndexChanged.Invoke(current);
            return current;
        }

        /// <summary>
        /// 将索引置位最后一个
        /// </summary>
        /// <returns></returns>
        public int SetToMax()
        {
            current = Max;
            onIndexChanged.Invoke(current);
            return current;
        }

        private int PrevValue()
        {
            if (Loop)
                return (int)Mathf.Repeat(current - 1, Max - Min + 1) + Min;
            else
                return (int)Mathf.Clamp(current - 1, Min, Max);
        }

        private int NextValue()
        {
            if (Loop)
                return (int)Mathf.Repeat(current + 1, Max - Min + 1) + Min;
            else
                return (int)Mathf.Clamp(current + 1, Min, Max);
        }

        /// <summary>
        /// 下一个索引是否会越界
        /// </summary>
        /// <returns>越界则返回True  否则返回False</returns>
        public bool CheckNextOverflow()
        {
            var _next = current + 1;
            return _next > maxIndex;
        }

        public bool CheckPrevOverflow()
        {
            var _next = current - 1;
            return _next < minIndex;
        }
    }
}
