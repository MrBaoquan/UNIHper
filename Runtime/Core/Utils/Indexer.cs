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

        private UnityEvent<int> onValueChanged = new UnityEvent<int>();

        public IObservable<int> OnValueChangedAsObservable()
        {
            return onValueChanged.AsObservable();
        }

        public IObservable<int> OnValueChangedToMaxAsObservable()
        {
            return OnValueChangedAsObservable().Where(_ => _ == Max);
        }

        public IObservable<int> OnValueChangedToMinAsObservable()
        {
            return OnValueChangedAsObservable().Where(_ => _ == Min);
        }

        public Indexer() { }

        public Indexer(int Max)
        {
            SetMax(Max);
        }

        public Indexer(int Max, bool Loop)
        {
            SetMax(Max);
            this.Loop = Loop;
        }

        public Indexer(int Min, int Max)
        {
            SetMin(Min);
            SetMax(Max);
        }

        public Indexer(int Min, int Max, bool Loop)
        {
            SetMin(Min);
            SetMax(Max);
            this.Loop = Loop;
        }

        /// <summary>
        /// 设置索引最大值
        /// </summary>
        /// <param name="Max">索引最大值</param>
        public void SetMax(int Max)
        {
            if (Max < minIndex)
            {
                Debug.LogError("Max index must greater than Min index!");
                return;
            }
            maxIndex = Max;
            if (Current > maxIndex)
                Set(maxIndex);
        }

        /// <summary>
        /// 设置索引最小值
        /// </summary>
        /// <param name="Min">索引最小值</param>
        public void SetMin(int Min)
        {
            if (Min > maxIndex)
            {
                Debug.LogError("Min index must less than Max index!");
                return;
            }
            minIndex = Min;
            if (Current < minIndex)
                Set(minIndex);
        }

        /// <summary>
        /// 上一次设置的索引值
        /// </summary>
        public int LastSet { get; private set; } = 0;

        /// <summary>
        /// 设置当前索引值
        /// </summary>
        /// <param name="newIndex"></param>
        /// <returns></returns>
        public int Set(int newIndex)
        {
            var _newIndex = limitIndex(newIndex);
            if (current != _newIndex)
            {
                current = _newIndex;
                LastSet = current;
                onValueChanged.Invoke(current);
            }
            return current;
        }

        public int SetValueWithoutNotify(int newIndex)
        {
            var _newIndex = limitIndex(newIndex);
            if (current != _newIndex)
            {
                current = _newIndex;
                LastSet = current;
            }
            return current;
        }

        public int SetValueAndForceNotify(int newIndex)
        {
            var _newIndex = limitIndex(newIndex);
            if (current != _newIndex)
            {
                current = _newIndex;
                LastSet = current;
                onValueChanged.Invoke(current);
            }
            else
            {
                onValueChanged.Invoke(current);
            }

            return current;
        }

        public int Next()
        {
            Set(NextValue());
            return current;
        }

        public int Next(int index)
        {
            Set(index);
            return Next();
        }

        public int Prev()
        {
            Set(PrevValue());
            return current;
        }

        public int Prev(int index)
        {
            Set(index);
            return Prev();
        }

        /// <summary>
        /// 将索引置为第一个
        /// </summary>
        /// <returns></returns>
        public int SetToMin()
        {
            Set(Min);
            return current;
        }

        /// <summary>
        /// 将索引置位最后一个
        /// </summary>
        /// <returns></returns>
        public int SetToMax()
        {
            Set(Max);
            return current;
        }

        /// <summary>
        /// Force Notify Index Changed
        /// </summary>
        /// <returns></returns>
        public int Notify()
        {
            SetValueAndForceNotify(current);
            return current;
        }

        public int PrevValue()
        {
            if (Loop)
                return (int)Mathf.Repeat(this.Current - 1, Max - Min + 1) + Min;
            else
                return (int)Mathf.Clamp(this.Current - 1, Min, Max);
        }

        /// <summary>
        /// 获取下一个索引值,不改变当前索引值
        /// </summary>
        /// <returns></returns>
        public int NextValue()
        {
            if (Loop)
                return (int)Mathf.Repeat(this.Current + 1, Max - Min + 1) + Min;
            else
                return (int)Mathf.Clamp(this.Current + 1, Min, Max);
        }

        /// <summary>
        /// 下一个索引是否会越界
        /// </summary>
        /// <returns>越界则返回True  否则返回False</returns>
        public bool CheckNextOverflow()
        {
            var _next = this.Current + 1;
            return _next > maxIndex;
        }

        public bool CheckPrevOverflow()
        {
            var _next = this.Current - 1;
            return _next < minIndex;
        }

        public bool IsLast()
        {
            return this.Current == Max;
        }

        public bool IsFirst()
        {
            return this.Current == Min;
        }

        private int limitIndex(int newIndex)
        {
            if (newIndex < minIndex || newIndex > maxIndex)
            {
                if (Loop)
                    newIndex = (int)Mathf.Repeat(newIndex, maxIndex - minIndex + 1) + Min;
                else
                    newIndex = Mathf.Clamp(newIndex, minIndex, maxIndex);
            }
            return newIndex;
        }
    }
}
