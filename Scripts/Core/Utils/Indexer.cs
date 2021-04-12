using UnityEngine;

namespace UNIHper {

    /// <summary>
    /// 数组索引迭代器 迭代范围 [Min,Max)
    /// </summary>
    public class Indexer {
        private int maxIndex;
        private int minIndex = 0;
        private int current = 0;
        public int Current {
            get { return current; }
        }

        public Indexer (int Max) {
            SetMax (Max);
        }

        public Indexer (int Min, int Max) {
            SetMin (Min);
            SetMax (Max);
        }

        /// <summary>
        /// 设置索引最大值
        /// </summary>
        /// <param name="Max">索引最大值</param>
        public void SetMax (int Max) {
            maxIndex = Max;
        }

        /// <summary>
        /// 设置索引最小值
        /// </summary>
        /// <param name="Min">索引最小值</param>
        public void SetMin (int Min) {
            minIndex = Min;
        }

        /// <summary>
        /// 设置当前索引值
        /// </summary>
        /// <param name="Current"></param>
        /// <returns></returns>
        public int Set (int Current) {
            current = Current;
            return current;
        }

        public int Next () {
            current = (int) Mathf.Repeat (current + 1, maxIndex);
            return current;
        }

        public int Prev () {
            current = (int) Mathf.Repeat (current - 1, maxIndex);
            return current;
        }

        /// <summary>
        /// 下一个索引是否会越界
        /// </summary>
        /// <returns>越界则返回True  否则返回False</returns>
        public bool CheckNextOverflow () {
            var _next = current + 1;
            return _next >= maxIndex;
        }

        public bool CheckPrevOverflow () {
            var _next = current - 1;
            return _next < 0;
        }

    }

}