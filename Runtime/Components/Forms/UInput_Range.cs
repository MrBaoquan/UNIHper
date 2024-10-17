using System;
using System.Collections;
using System.Collections.Generic;
using DNHper;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    public class UInput_Range : MonoBehaviour
    {
        public string FieldTitle = "字段范围";
        public string MinTitle = "最小值";
        public string MaxTitle = "最大值";
        private InputField inputMinValue = null;
        private InputField inputMaxValue = null;

        private Text fieldTitle = null;

        private Text textMinTitle = null;
        private Text textMaxTitle = null;

        // Start is called before the first frame update

        public Action<float> onMinValueChanged;
        public Action<float> onMaxValueChanged;

        public float MinValue
        {
            set { syncMinValue(value); }
        }

        public float MaxValue
        {
            set { syncMaxValue(value); }
        }

        void syncMinValue(float InValue)
        {
            inputMinValue.text = InValue.ToString();
        }

        void syncMaxValue(float InValue)
        {
            inputMaxValue.text = InValue.ToString();
        }

        private void Awake()
        {
            ReBuildRefs();
        }

        void Start()
        {
            inputMaxValue.onValueChanged.AddListener(_ =>
            {
                if (onMaxValueChanged != null)
                    onMaxValueChanged(_.Parse2Float());
            });

            inputMinValue.onValueChanged.AddListener(_ =>
            {
                if (onMinValueChanged != null)
                    onMinValueChanged(_.Parse2Float());
            });
        }

        private void OnValidate()
        {
            ReBuildRefs();

            fieldTitle.text = FieldTitle;
            textMinTitle.text = MinTitle;
            textMaxTitle.text = MaxTitle;
        }

        void ReBuildRefs()
        {
            fieldTitle = this.Get<Text>("text_title");

            inputMinValue = this.Get<InputField>("input_value_min");
            inputMaxValue = this.Get<InputField>("input_value_max");

            textMinTitle = this.Get<Text>("text_min_title");
            textMaxTitle = this.Get<Text>("text_max_title");
        }

        // Update is called once per frame
        void Update() { }
    }
}
