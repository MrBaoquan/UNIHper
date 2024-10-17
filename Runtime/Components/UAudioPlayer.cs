using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UNIHper
{
    public class AudioPlayer : MonoBehaviour
    {
        public AudioSource GetAudioSource(int audioID)
        {
            var _audioSources = GetComponents<AudioSource>();
            for (var _idx = _audioSources.Length; _idx <= audioID; ++_idx)
            {
                gameObject.AddComponent<AudioSource>();
            }
            return GetComponents<AudioSource>()[audioID];
        }

        public float Duration(int audioID = 0)
        {
            var _audioClip = GetAudioSource(audioID).clip;
            return _audioClip ? _audioClip.length : 0.0f;
        }

        public AudioClip Clip(int audioID = 0)
        {
            return GetAudioSource(audioID).clip;
        }

        // Start is called before the first frame update
        void Start() { }

        // Update is called once per frame
        void Update() { }
    }
}
