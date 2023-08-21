using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UNIHper;
using DNHper;
using TMPro;
using UniRx;
using UnityEngine.InputSystem;

public class HelpUI : UIBase
{
    public void SetContent(string helpText)
    {
        this.helpText.text = helpText;
    }

    public void SetContent(string helpText, float textSize, Color textColor)
    {
        this.helpText.text = helpText;
        this.helpText.color = textColor;
        this.helpText.fontSize = textSize;
    }

    public void SetContent(string helpText, float textSize)
    {
        this.helpText.text = helpText;
        this.helpText.fontSize = textSize;
    }

    public void SetBackgroundColor(Color color)
    {
        this.Get<Image>().color = color;
    }

    // Start is called before the first frame update
    private void Start() { }

    // Update is called once per frame
    private void Update() { }

    TextMeshProUGUI helpText;

    // Called when this ui is loaded
    protected override void OnLoaded()
    {
        helpText = this.Get<TextMeshProUGUI>("text_help");
        Observable
            .EveryUpdate()
            .Subscribe(_ =>
            {
                if (Keyboard.current.f1Key.wasPressedThisFrame)
                {
                    this.Toggle();
                }
            })
            .AddTo(this);
    }

    // Called when this ui is shown
    protected override void OnShown() { }

    // Called when this ui is hidden
    protected override void OnHidden() { }
}
