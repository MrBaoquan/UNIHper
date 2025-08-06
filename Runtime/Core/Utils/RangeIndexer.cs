using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace UNIHper
{
    public class RangeIndexer
    {
        private Indexer startIndexer = new Indexer();
        private Indexer endIndexer = new Indexer();

        public List<int> Values
        {
            get
            {
                var _length = endIndexer.Current - startIndexer.Current;
                return Enumerable.Range(0, _length).Select(_idx => startIndexer.NextValue(_idx)).ToList();
            }
        }

        public IObservable<(int Start, int End)> OnValueChangedAsObservable()
        {
            return Observable
                .Merge(startIndexer.OnValueChangedAsObservable(), endIndexer.OnValueChangedAsObservable())
                .Select(_ => (startIndexer.Current, endIndexer.Current));
        }

        public RangeIndexer Set(int start, int end)
        {
            startIndexer.Set(start);
            endIndexer.Set(end);
            Debug.LogWarning($"Set RangeIndexer: {start} ~ {end}, real : {startIndexer.Current} ~ {endIndexer.Current}");
            return this;
        }

        public (int start, int end) Next()
        {
            Debug.Log($"startIndexer Range: {startIndexer.Min} ~ {startIndexer.Max}, Current: {startIndexer.Current}");
            return (startIndexer.Next(), endIndexer.Next());
        }

        public (int start, int end) Prev()
        {
            return (startIndexer.Prev(), endIndexer.Prev());
        }

        public void SetMax(int max)
        {
            startIndexer.SetMax(max);
            endIndexer.SetMax(max);
        }

        public void SetMin(int min)
        {
            startIndexer.SetMin(min);
            endIndexer.SetMin(min);
        }

        public void SetRange(int min, int max)
        {
            SetMin(min);
            SetMax(max);
        }
    }
}
