using System;
using UnityEngine;
using UnityEngine.UI;
using UNIHper;

namespace UNIHper
{
    using UniRx;

    [RequireComponent(typeof(Slider))]
    public class USlider : MonoBehaviour
    {
        private ReactiveProperty<string> m_textTitle = new ReactiveProperty<string>();
        private ReactiveProperty<string> m_textValue = new ReactiveProperty<string>();

        public IObservable<float> OnValueChangedAsObservable()
        {
            return GetComponent<Slider>().OnValueChangedAsObservable();
        }

        public void SetValue(float value)
        {
            GetComponent<Slider>().value = value;
        }

        // Start is called before the first frame update
        void Start()
        {
            m_textTitle.Value = this.Get<Text>("text_title").text;
            m_textTitle.SubscribeToText(this.Get<Text>("text_title"));
            m_textValue.SubscribeToText(this.Get<Text>("text_value"));
            var _slider = GetComponent<Slider>();
            _slider
                .OnValueChangedAsObservable()
                .Subscribe(x =>
                {
                    m_textValue.Value = _slider.wholeNumbers ? x.ToString("0") : x.ToString("0.00");
                })
                .AddTo(this);
        }

        // Update is called once per frame
        void Update() { }
    }
}
