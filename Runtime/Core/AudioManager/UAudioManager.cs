using System;
using System.Collections;
using System.Collections.Generic;
using DNHper;
using UniRx;
using UnityEngine;

namespace UNIHper {

    public class UAudioManager : SingletonBehaviour<UAudioManager> {

        public void PlayMusic (AudioClip InMusic, float InVolume = 1.0f, bool Loop = true) {
            musicAudioSource.clip = InMusic;
            musicAudioSource.volume = InVolume;
            musicAudioSource.loop = Loop;
            musicAudioSource.Play ();
        }

        public void PlayMusic (string InMusic, float InVolume = 1.0f) {
            AudioClip _clip = Managements.Resource.Get<AudioClip> (InMusic);
            PlayMusic (_clip, InVolume);
        }

        public void PlayMusic () {
            if (!musicAudioSource.isPlaying)
                musicAudioSource.Play ();
        }

        public void PauseMusic () {
            musicAudioSource.Pause ();
        }

        public void StopMusic () {
            musicAudioSource.Stop ();
        }

        public void PlayEffect (AudioClip InEffect, Action<AudioClip> InCallback = null) {
            effectAudioSource.PlayOneShot (InEffect);
            Observable.Timer (TimeSpan.FromSeconds (InEffect.length + 0.1f))
                .Subscribe (_1 => {
                    if (InCallback != null) InCallback (InEffect);
                });
        }

        public void PlayEffect (string InEffect, Action<AudioClip> InCallback = null) {
            AudioClip _clip = Managements.Resource.Get<AudioClip> (InEffect);
            PlayEffect (_clip, InCallback);
        }

        public void StopEffect () {
            effectAudioSource.Stop ();
        }

        private AudioSource musicAudioSource;
        public AudioSource MusicAudioSource { get => musicAudioSource; }

        private AudioSource effectAudioSource;
        public AudioSource EffectAudioSource { get => effectAudioSource; }

        private void Awake () {
            AudioSource[] _audioSources = this.GetComponents<AudioSource> ();
            musicAudioSource = _audioSources[0];
            effectAudioSource = _audioSources[1];
        }

        // Start is called before the first frame update
        void Start () {

        }

        // Update is called once per frame
        void Update () {

        }
    }

}