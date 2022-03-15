using System;
using System.Collections;
using System.Collections.Generic;

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

namespace DNHper {
    public class Singleton<T> where T : new () {
        /*	Instance	*/
        private static T instance;

        static Singleton () { }

        public static T Instance {
            get {
                if (instance == null) {
                    instance = new T ();
                }
                return instance;
            }
        }

        public static void Destroy () {
            instance = default (T);
        }
    }

}