using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace UNIHper {

    public class ToggleEnable : MonoBehaviour {
        public bool Default = false;
        public KeyCode EnableKey = KeyCode.F10;
        // Start is called before the first frame update
        void Start () {
            gameObject.SetActive (Default);
            Observable.EveryUpdate ()
                .Subscribe (_ => {
                    if (Input.GetKeyDown (EnableKey)) {
                        gameObject.SetActive (!gameObject.activeInHierarchy);
                    }
                }).AddTo (this);
        }

        // Update is called once per frame
        void Update () {

        }
    }

}