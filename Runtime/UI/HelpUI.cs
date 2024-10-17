using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UNIHper;
using DNHper;
using TMPro;

using UnityEngine.InputSystem;

namespace UNIHper.UI
{
    using UniRx;

    public class HelpUI : UIBase
    {
        private string builtinHelpText =>
            $"{Application.productName}   "
            + $"Version: {Application.version}\n"
            + Managements.UI.Get<LicenseUI>().LicenseText;

        private ReactiveProperty<string> userHelpText = new ReactiveProperty<string>(string.Empty);

        public void SetContent(string helpText)
        {
            userHelpText.Value = helpText;
        }

        public void SetContent(string helpText, float textSize, Color textColor)
        {
            this.helpText.color = textColor;
            this.helpText.fontSize = textSize;
            SetContent(helpText);
        }

        public void SetContent(string helpText, float textSize)
        {
            this.helpText.fontSize = textSize;
            SetContent(helpText);
        }

        public void SetBackgroundColor(Color color)
        {
            this.Get<Image>().color = color;
        }

        // Start is called before the first frame update
        private void Start()
        {
            userHelpText
                .Merge(
                    Managements.UI.Get<LicenseUI>().OnLicenseValidChanged.Select(_ => string.Empty)
                )
                .Subscribe(helpText =>
                {
                    this.helpText.text = builtinHelpText + "\r\n" + helpText;
                })
                .AddTo(this);
        }

        // Update is called once per frame
        private void Update() { }

        TextMeshProUGUI helpText;

        // Called when this ui is loaded
        protected override void OnLoaded()
        {
            helpText = this.Get<TextMeshProUGUI>("text_help");
            SetContent(string.Empty);
            Observable
                .EveryUpdate()
                .Subscribe(_ =>
                {
#if ENABLE_INPUT_SYSTEM
                    if (Keyboard.current.f1Key.wasPressedThisFrame)
                    {
                        this.Toggle();
                    }
#endif
                })
                .AddTo(this);

            this.Get<Button>("btn_license")
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Managements.UI.Show<LicenseUI>();
                });
        }

        // Called when this ui is shown
        protected override void OnShown() { }

        // Called when this ui is hidden
        protected override void OnHidden() { }
    }
}
