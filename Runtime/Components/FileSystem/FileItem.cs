using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    public class FileItem : MonoBehaviour
    {
        public void SetFileName(string InName)
        {
            this.Get<Text>("text_fileName").text = InName;
        }

        public void SetSelected(bool bSelected = true)
        {
            this.Get<Image>().color = bSelected ? Color.gray : Color.clear;
        }

        // Start is called before the first frame update
        void Start() { }

        // Update is called once per frame
        void Update() { }
    }
}
