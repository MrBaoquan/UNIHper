using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine.UI;

namespace UNIHper.UI
{
    public class DialogUI : UIBase
    {
        public void ShowDialog()
        {
            this.Get("button_group/btn_cancel").SetActive(true);
            this.Get("button_group/btn_cancel").SetActive(false);
        }

        public void ShowConfirm()
        {
            this.Get("button_group/btn_cancel").SetActive(true);
            this.Get("button_group/btn_cancel").SetActive(true);
        }

        public IObservable<Unit> OnConfirmAsObservable()
        {
            return this.Get<Button>("button_group/btn_confirm").OnClickAsObservable();
        }

        public IObservable<Unit> OnCancelAsObservable()
        {
            return this.Get<Button>("button_group/btn_cancel").OnClickAsObservable();
        }

        public DialogUI SetContent(string Content)
        {
            this.Get<Text>("content_panel/Scroll View/Viewport/Content").text = Content;
            return this;
        }

        public DialogUI SetTitle(string Title)
        {
            this.Get<Text>("title/text_title").text = Title;
            return this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            EnableDragMove();
        }

        // Update is called once per frame
        private void Update() { }

        // Called when this ui is showing
        protected override void OnShown() { }

        // Called when this ui is hidden
        protected override void OnHidden() { }
    }
}
