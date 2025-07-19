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

        public int Max { get; private set; }

        public int Min { get; private set; }

        public int Current { get; private set; }

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

        public IObservable<int> OnPrevAsObservable()
        {
            return OnValueChangedAsObservable().Where(_ => LastSet == NextValue());
        }

        public IObservable<int> OnNextAsObservable()
        {
            return OnValueChangedAsObservable().Where(_ => LastSet == PrevValue());
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
            SetMinAndMax(Min, Max, false);
        }

        public Indexer(int Min, int Max, bool Loop)
        {
            SetMinAndMax(Min, Max, false);
            this.Loop = Loop;
        }

        /// <summary>
        /// 设置索引最大值
        /// </summary>
        /// <param name="MaxVal">索引最大值</param>
        public void SetMax(int MaxVal)
        {
            if (MaxVal < Min)
            {
                Debug.LogError("Max index must greater than Min index!");
                return;
            }
            Max = MaxVal;
            if (Current > Max)
                Set(MaxVal);
        }

        /// <summary>
        /// 设置索引最小值
        /// </summary>
        /// <param name="MinVal">索引最小值</param>
        public void SetMin(int MinVal)
        {
            if (MinVal > Max)
            {
                Debug.LogError("Min index must less than Max index!");
                return;
            }
            this.Min = MinVal;
            if (Current < Min)
                Set(Min);
        }

        public bool SetMinAndMax(int MinVal, int MaxVal, bool NotifyIfChanged = true)
        {
            if (MaxVal < MinVal)
            {
                Debug.LogError("Max index must greater than Min index!");
                return false;
            }
            this.Min = MinVal;
            this.Max = MaxVal;
            // if (Current < minIndex)
            // {
            //     if (NotifyIfChanged)
            //         Set(minIndex);
            //     else
            //         SetValueWithoutNotify(minIndex);
            // }
            // else if (Current > maxIndex)
            // {
            //     if (NotifyIfChanged)
            //         Set(maxIndex);
            //     else
            //         SetValueWithoutNotify(maxIndex);
            // }
            return true;
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
            if (Current != _newIndex)
            {
                LastSet = Current;

                Current = _newIndex;
                onValueChanged.Invoke(Current);
            }
            return Current;
        }

        public int SetValueWithoutNotify(int newIndex)
        {
            var _newIndex = limitIndex(newIndex);
            if (Current != _newIndex)
            {
                LastSet = Current;

                Current = _newIndex;
            }
            return Current;
        }

        public int SetValueAndForceNotify(int newIndex)
        {
            var _newIndex = limitIndex(newIndex);
            if (Current != _newIndex)
            {
                LastSet = Current;

                Current = _newIndex;
                onValueChanged.Invoke(Current);
            }
            else
            {
                onValueChanged.Invoke(Current);
            }

            return Current;
        }

        public static (int MinStep, int Direction) GetMinStepsAndDirection(
            int x,
            int y,
            int A,
            int T
        )
        {
            int N = y - x + 1;
            int idxA = A - x;
            int idxT = T - x;

            int distForward = (idxT - idxA + N) % N;
            int distBackward = (idxA - idxT + N) % N;

            if (distForward <= distBackward)
            {
                return (distForward, +1);
            }
            else
            {
                return (distBackward, -1);
            }
        }

        public (int MinStep, int Direction) MinStepForValue(int value)
        {
            return GetMinStepsAndDirection(Min, Max, Current, value);
        }

        public int Next()
        {
            Set(NextValue());
            return Current;
        }

        public int Next(int index)
        {
            Set(index);
            return Next();
        }

        public int Prev()
        {
            Set(PrevValue());
            return Current;
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
            return Current;
        }

        /// <summary>
        /// 将索引置位最后一个
        /// </summary>
        /// <returns></returns>
        public int SetToMax()
        {
            Set(Max);
            return Current;
        }

        /// <summary>
        /// Force Notify Index Changed
        /// </summary>
        /// <returns></returns>
        public int Notify()
        {
            SetValueAndForceNotify(Current);
            return Current;
        }

        public int PrevValue()
        {
            if (Loop)
                return (int)Mathf.Repeat(this.Current - 1 - Min, Max - Min + 1) + Min;
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
                return (int)Mathf.Repeat(this.Current + 1 - Min, Max - Min + 1) + Min;
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
            return _next > Max;
        }

        public bool CheckPrevOverflow()
        {
            var _next = this.Current - 1;
            return _next < Min;
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
            if (newIndex < Min || newIndex > Max)
            {
                if (Loop)
                    newIndex = (int)Mathf.Repeat(newIndex - Min, Max - Min + 1) + Min;
                else
                    newIndex = Mathf.Clamp(newIndex, Min, Max);
            }

            return newIndex;
        }
    }
}
