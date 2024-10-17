using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    using UniRx;

    public class UInput : MonoBehaviour
    {
        public string Title = "标题";
        public string Placeholder = "请输入...";
        public string InputValue = string.Empty;
        private ReactiveProperty<string> Reac_InputValue = new ReactiveProperty<string>();
        public InputField.CharacterValidation CharacterValidation = InputField
            .CharacterValidation
            .None;

        public void SetValue(string InInputValue)
        {
            Reac_InputValue.Value = InInputValue;
        }

        public string GetValue()
        {
            if (!Reac_InputValue.HasValue)
                return "";
            return Reac_InputValue.Value;
        }

        public IObservable<string> OnValueChangedAsObservable()
        {
            return this.Get<InputField>("input_value").OnValueChangedAsObservable();
        }

        private void OnValidate()
        {
            this.Get<Text>("text_title").text = Title;
            this.Get<InputField>("input_value").characterValidation = CharacterValidation;
            this.Get<InputField>("input_value").text = InputValue;
            this.Get<Text>("input_value/Placeholder").text = Placeholder;
        }

        void Start()
        {
            OnValueChangedAsObservable()
                .Subscribe(_value =>
                {
                    Reac_InputValue.Value = _value;
                });

            Reac_InputValue
                .ThrottleFrame(1)
                .Subscribe(_value =>
                {
                    if (_value == null)
                        return;
                    InputValue = _value;
                    this.Get<InputField>("input_value").text = _value;
                });
        }

        // Update is called once per frame
        void Update() { }
    }
}
