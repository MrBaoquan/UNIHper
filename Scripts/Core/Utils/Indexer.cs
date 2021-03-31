using UnityEngine;

namespace UNIHper {

    /// <summary>
    /// 数组索引迭代器
    /// </summary>
    public class Indexer {
        private int maxIndex;
        private int current;
        public int Current {
            get { return current; }
        }

        public Indexer (int Size) {
            maxIndex = Size;
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