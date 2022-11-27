using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNHper;
using UniRx;
using UnityEngine;

namespace UNIHper {

    public class UAudioManager : SingletonBehaviour<UAudioManager> {

        public AudioSource PlayMusic (AudioClip InMusic, float InVolume = 1.0f, bool bLoop = true, int Index = 0) {
            var _audioSource = musicPlayer.GetAudioSource (Index);
            _audioSource.clip = InMusic;
            _audioSource.volume = InVolume;
            _audioSource.loop = bLoop;
            _audioSource.Play ();
            return _audioSource;
        }

        public void PlayMusic (string InMusic, float InVolume = 1.0f, bool bLoop = true, int Index = 0) {
            AudioClip _clip = Managements.Resource.Get<AudioClip> (InMusic);
            PlayMusic (_clip, InVolume, bLoop, Index);
        }

        public void PlayMusic (int Index = 0) {
            var _audioSource = musicPlayer.GetAudioSource (Index);
            if (_audioSource.clip != null && !_audioSource.isPlaying)
                _audioSource.Play ();
        }

        public void PauseMusic (int Index = 0) {
            var _audioSource = musicPlayer.GetAudioSource (Index);
            _audioSource.Pause ();
        }

        public void StopMusic (int Index = 0) {
            var _audioSource = musicPlayer.GetAudioSource (Index);
            _audioSource.Stop ();
        }

        public void PlayEffect (AudioClip InEffect, float InVolume = 1.0f, int Index = 0) {
            var _audioSource = effectPlayer.GetAudioSource (Index);
            _audioSource.volume = InVolume;
            _audioSource.PlayOneShot (InEffect);
        }

        public void PlayEffect (string InEffect, float InVolume = 1.0f, int Index = 0) {
            AudioClip _clip = Managements.Resource.Get<AudioClip> (InEffect);
            PlayEffect (_clip, InVolume, Index);
        }

        public void StopEffect (int Index = 0) {
            var _audioSource = effectPlayer.GetAudioSource (Index);
            _audioSource.Stop ();
        }

        private UAudioPlayer musicPlayer = null;
        private UAudioPlayer effectPlayer = null;

        public Task Initialize () {
            var _musicPlayer = new GameObject ("music_player");
            var _effectPlayer = new GameObject ("effect_player");
            _musicPlayer.transform.parent = this.transform;
            _effectPlayer.transform.parent = this.transform;

            musicPlayer = _musicPlayer.AddComponent<UAudioPlayer> ();
            effectPlayer = _effectPlayer.AddComponent<UAudioPlayer> ();
            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        void Start () {

        }

        // Update is called once per frame
        void Update () {

        }
    }

}