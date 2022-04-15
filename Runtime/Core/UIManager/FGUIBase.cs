using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNHper;
using FairyGUI;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UNIHper.UI;

namespace UNIHper {
    public abstract class FGUIBase : UIBase {

        public UIPanel Panel { get; private set; }

        protected GComponent ui {
            get => Panel.ui;
        }

        protected override void handleInit () {
            base.handleInit ();
            if (gameObject.GetComponent<UIPanel> () == null) {
                Panel = gameObject.AddComponent<UIPanel> ();
                Panel.packageName = __UIConfig.Package;
                Panel.componentName = __UIConfig.Component;
            } else {
                Panel = gameObject.GetComponent<UIPanel> ();
            }
            Panel.CreateUI ();
        }

        protected override void OnLoaded () { }

        protected override void OnShow () { }

        protected override void OnHidden () { }
    }

}