using System;
using DNHper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    using UniRx;

    public class UInput_Slider : MonoBehaviour
    {
        public string title = "标题";
        public float minValue = 0;
        public float maxValue = 100f;
        public float defaultValue = 50f;

        public float Value
        {
            set { syncValue(value); }
        }

        private TextMeshProUGUI textTitle;
        private TMP_InputField inputValue;
        private Slider sliderValue;
        private Slider SliderValue
        {
            get
            {
                if (sliderValue == null)
                {
                    sliderValue = this.Get<Slider>("slider_value");
                }
                return sliderValue;
            }
        }

        public Action<float> onValueChanged;

        public IObservable<float> OnValueChangedAsObservable()
        {
            return Observable.FromEvent<float>(
                _action => onValueChanged += _action,
                _action => onValueChanged -= _action
            );
        }

        private void OnValidate()
        {
            BuildRefs();
            textTitle.text = title;
            inputValue.text = defaultValue.ToString();
            sliderValue.minValue = minValue;
            sliderValue.maxValue = maxValue;
            sliderValue.value = defaultValue;
        }

        void BuildRefs()
        {
            textTitle = this.Get<TextMeshProUGUI>("text_title");
            inputValue = this.Get<TMP_InputField>("input_value");
            sliderValue = this.Get<Slider>("slider_value");
        }

        // Start is called before the first frame update
        void Start()
        {
            BuildRefs();

            inputValue.onValueChanged
                .AsObservable()
                .Subscribe(_ =>
                {
                    float _value = _.Parse2Float();
                    SliderValue.value = _value;
                    onValueChange();
                });

            SliderValue
                .OnValueChangedAsObservable()
                .Subscribe(_ =>
                {
                    inputValue.text = _.ToString("0.00");
                    SliderValue.value = Mathf.Clamp(_, minValue, maxValue);
                    onValueChange();
                });
        }

        void syncValue(float InValue)
        {
            SliderValue.value = InValue;
        }

        void onValueChange()
        {
            if (onValueChanged != null)
            {
                onValueChanged(SliderValue.value);
            }
        }

        // Update is called once per frame
        void Update() { }
    }
}
