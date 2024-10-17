using System.Threading.Tasks;
using UnityEngine;

namespace UNIHper
{
    public class AudioManager : SingletonBehaviourDontDestroy<AudioManager>
    {
        public AudioSource PlayMusic(
            AudioClip InMusic,
            float InVolume = 1.0f,
            bool bLoop = true,
            int Index = 0
        )
        {
            var _audioSource = MusicPlayer.GetAudioSource(Index);
            _audioSource.clip = InMusic;
            _audioSource.volume = InVolume;
            _audioSource.loop = bLoop;
            _audioSource.Play();
            return _audioSource;
        }

        public AudioSource PlayMusic(
            string InMusic,
            float InVolume = 1.0f,
            bool bLoop = true,
            int Index = 0
        )
        {
            AudioClip _clip = Managements.Resource.Get<AudioClip>(InMusic);
            return PlayMusic(_clip, InVolume, bLoop, Index);
        }

        public void PlayMusic(int Index = 0)
        {
            var _audioSource = MusicPlayer.GetAudioSource(Index);
            if (_audioSource.clip != null && !_audioSource.isPlaying)
                _audioSource.Play();
        }

        public void PauseMusic(int Index = 0)
        {
            var _audioSource = MusicPlayer.GetAudioSource(Index);
            _audioSource.Pause();
        }

        public void StopMusic(int Index = 0)
        {
            var _audioSource = MusicPlayer.GetAudioSource(Index);
            _audioSource.Stop();
        }

        public void PlayEffect(AudioClip effect, float InVolume = 1.0f, int Index = 0)
        {
            if (effect == null)
                return;
            var _audioSource = EffectPlayer.GetAudioSource(Index);
            _audioSource.volume = InVolume;
            _audioSource.PlayOneShot(effect);
        }

        public void PlayEffect(string effectName, float volume = 1.0f, int index = 0)
        {
            AudioClip _clip = Managements.Resource.Get<AudioClip>(effectName);
            PlayEffect(_clip, volume, index);
        }

        public void StopEffect(int index = 0)
        {
            var _audioSource = EffectPlayer.GetAudioSource(index);
            _audioSource.Stop();
        }

        private AudioPlayer musicPlayer = null;
        public AudioPlayer MusicPlayer
        {
            get
            {
                if (musicPlayer == null)
                {
                    var _musicPlayer = new GameObject("music_player");
                    _musicPlayer.transform.parent = this.transform;
                    musicPlayer = _musicPlayer.AddComponent<AudioPlayer>();
                }
                return musicPlayer;
            }
        }
        private AudioPlayer effectPlayer = null;

        public AudioPlayer EffectPlayer
        {
            get
            {
                if (effectPlayer == null)
                {
                    var _effectPlayer = new GameObject("effect_player");
                    _effectPlayer.transform.parent = this.transform;
                    effectPlayer = _effectPlayer.AddComponent<AudioPlayer>();
                }
                return effectPlayer;
            }
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        // Start is called before the first frame update
        void Start() { }

        // Update is called once per frame
        void Update() { }
    }
}
