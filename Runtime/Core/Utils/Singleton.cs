using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * File: Singleton.cs
 * File Created: 2019-10-11 10:17:17
 * Author: MrBaoquan (mrma617@gmail.com)
 * -----
 * Last Modified: 2019-10-11 16:12:59 pm
 * Modified By: MrBaoquan (mrma617@gmail.com>)
 * -----
 * Copyright 2019 - 2019 mrma617@gmail.com
 */

namespace UNIHper {

    public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour {
        private static T instance;
        public static T Instance {
            get {
                if (instance == null) {
                    instance = FindObjectOfType (typeof (T), true) as T;
                }
                return instance;
            }
        }

        private void OnDestroy () {

        }
    }
}