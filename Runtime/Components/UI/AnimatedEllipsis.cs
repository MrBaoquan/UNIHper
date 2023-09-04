using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UNIHper
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class AnimatedEllipsis : MonoBehaviour
    {
        private TextMeshProUGUI textComponent;
        private string originalText;

        [SerializeField]
        private int ellipsisCount = 3;

        [SerializeField]
        private string ellipsisText = ".";

        [SerializeField]
        private float ellipsisInterval = 0.5f;

        // Start is called before the first frame update
        void Start()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            originalText = textComponent.text;
            StartCoroutine(animateEllipsis());
        }

        private IEnumerator animateEllipsis()
        {
            while (true)
            {
                textComponent.text = originalText;
                for (int i = 0; i < ellipsisCount + 1; i++)
                {
                    yield return new WaitForSeconds(ellipsisInterval);
                    textComponent.text += ellipsisText;
                }
            }
        }

        // Update is called once per frame
        void Update() { }
    }
}
