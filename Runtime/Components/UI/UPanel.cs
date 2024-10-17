using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace UNIHper
{
    public class UPanel : MonoBehaviour
    {
        // Start is called before the first frame update
        public string Title = "标题";

        public IObservable<Unit> OnCloseAsObservable()
        {
            return this.Get<Button>("btn_close").OnClickAsObservable();
        }

        public void SetTitle(string InTitle)
        {
            Title = InTitle;
            OnValidate();
        }

        private void OnValidate()
        {
            this.Get<Text>("title/text_title").text = Title;
        }

        void Start() { }

        // Update is called once per frame
        void Update() { }
    }
}
