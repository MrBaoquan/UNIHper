// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection;
// using Sirenix.OdinInspector;
// using UnityEngine;

// namespace UNIHper {

//     [CreateAssetMenu (fileName = "UIConfig", menuName = "UNIHper/Assets/UIConfig", order = 1)]
//     public class UIConfigObject : ScriptableObject {
//         [ShowInInspector]
//         [DictionaryDrawerSettings (KeyLabel = "UI  Name", ValueLabel = "UI Metadata", DisplayMode = DictionaryDisplayOptions.OneLine)]
//         public Dictionary<string, List<UIMeta>> UIAssets = new Dictionary<string, List<UIMeta>> ();
//         public UIMeta config = new UIMeta ();
//         private static UIConfigObject instance = null;
//         private static UIConfigObject Self () {
//             if (instance == null)
//                 instance = Resources.Load<UIConfigObject> ("UIConfig");
//             return instance;
//         }
//     }

// }