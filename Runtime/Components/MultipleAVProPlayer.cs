using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using PathologicalGames;
using RenderHeads.Media.AVProVideo;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper
{
    [RequireComponent(typeof(DisplayUGUI))]
    public class MultipleAVProPlayer : MonoBehaviour
    {
        private UnityEvent<AVProPlayer> onPlayerChanged = new UnityEvent<AVProPlayer>();

        public IObservable<AVProPlayer> OnPlayerChangedAsObservable()
        {
            return onPlayerChanged.AsObservable();
        }

        public double CurrentTime
        {
            get
            {
                if (currentPlayer == null)
                    return 0;
                return this.currentPlayer.CurrentTime;
            }
        }

        public bool Loop { get; set; } = false;

        private Indexer videoIndex = new Indexer(0);
        private List<string> videoPaths = new List<string>();

        public async Task PrepareVideos(
            IEnumerable<string> VideoPaths,
            Action<AVProPlayer> settingCallback = null
        )
        {
            videoIndex.SetMax(VideoPaths.Count() - 1);
            this.videoPaths = VideoPaths.ToList();

            var _defaultPlayer = new GameObject("mediaPlayer_default");
            var _avProPlayer = _defaultPlayer.AddComponent<AVProPlayer>();
            _avProPlayer.MediaPlayer.AutoStart = false;
            _avProPlayer.MediaPlayer.AutoOpen = false;
            _avProPlayer.MediaPlayer.Loop = Loop;

            settingCallback?.Invoke(_avProPlayer);

            var _playerPrefabPool = new PrefabPool(_defaultPlayer.transform);
            _playerPrefabPool.cullDespawned = true;
            _playerPrefabPool.preloadAmount = 3;

            var _mediaPlayerPool = PoolManager.Pools.Create(gameObject.name + "_pool");
            _mediaPlayerPool.CreatePrefabPool(_playerPrefabPool);

            await Observable.Zip(
                VideoPaths.Select(_path =>
                {
                    var _mediaPlayer = _mediaPlayerPool.Spawn(_defaultPlayer.transform, transform);
                    _mediaPlayer.SetAsLastSibling();
                    var _avproPlayer = _mediaPlayer.GetComponent<AVProPlayer>();
                    var _mediaPathType = MediaPathType.RelativeToStreamingAssetsFolder;
                    if (Path.IsPathRooted(_path))
                    {
                        _mediaPathType = MediaPathType.AbsolutePathOrURL;
                    }
                    _avproPlayer.MediaPlayer.OpenMedia(_mediaPathType, _path, false);
                    return _avproPlayer.OnMetaDataReadyAsObservable().First();
                })
            );
            GameObject.DestroyImmediate(_defaultPlayer);
            onPlayerChanged.Invoke(currentPlayer);
        }

        public void Pause()
        {
            if (currentPlayer == null)
                return;
            currentPlayer.Pause();
        }

        public bool IsPaused
        {
            get
            {
                if (currentPlayer == null)
                    return false;
                return currentPlayer.IsPaused;
            }
        }

        public bool IsFinished
        {
            get
            {
                if (currentPlayer == null)
                    return false;
                return currentPlayer.IsFinished;
            }
        }

        public int CurrentFrame
        {
            get
            {
                if (currentPlayer == null)
                    return 0;
                return currentPlayer.CurrentFrame;
            }
        }

        public double Duration
        {
            get
            {
                if (currentPlayer == null)
                    return 0;
                return currentPlayer.Duration;
            }
        }
        public int MaxFrameNumber
        {
            get
            {
                if (currentPlayer == null)
                    return 0;
                return currentPlayer.MaxFrameNumber;
            }
        }

        public Material Material
        {
            get => displayUGUI.material;
        }

        public void Play(bool FadeEffect = true)
        {
            playVideo(FadeEffect);
        }

        public void Play(
            string Path,
            Action<AVProPlayer> OnCompleted,
            bool Loop = false,
            double StartTime = 0f,
            double EndTime = 0f
        )
        {
            var _idx = videoPaths.FindIndex(_path => _path == Path);
            if (_idx == -1)
            {
                Debug.LogWarning("Video path not found: " + Path);
                return;
            }
            stopVideo();

            videoIndex.Set(_idx);
            onPlayerChanged.Invoke(currentPlayer);

            currentPlayer.Play(Path, OnCompleted, Loop, StartTime, EndTime);
            DisplayUGUI.CurrentMediaPlayer = currentPlayer.GetComponent<MediaPlayer>();
            playFadeEffect();
        }

        public bool FadePlay { get; set; } = true;

        public void Stop()
        {
            stopVideo();
        }

        public void SwitchNext(bool bAutoPlay = true)
        {
            stopVideo();
            videoIndex.Next();
            onPlayerChanged.Invoke(currentPlayer);
            if (bAutoPlay)
            {
                playVideo();
            }
        }

        public void SwitchPrev(bool bAutoPlay = true)
        {
            stopVideo();
            videoIndex.Prev();
            onPlayerChanged.Invoke(currentPlayer);
            if (bAutoPlay)
            {
                playVideo();
            }
        }

        public void SetPlaybackRate(float rate)
        {
            currentPlayer.SetPlaybackRate(rate);
        }

        public void SetVolume(float volume)
        {
            currentPlayer.SetVolume(volume);
        }

        public void MuteAudio(bool bMute)
        {
            currentPlayer.MuteAudio(bMute);
        }

        private AVProPlayer currentPlayer
        {
            get => transform.GetChild(videoIndex.Current).GetComponent<AVProPlayer>();
        }
        public AVProPlayer CurrentPlayer
        {
            get => currentPlayer;
        }
        private DisplayUGUI displayUGUI = null;
        public DisplayUGUI DisplayUGUI
        {
            get
            {
                if (displayUGUI == null)
                {
                    displayUGUI = this.Get<DisplayUGUI>();
                }
                return displayUGUI;
            }
        }

        public void SetMaterial(Material NewMaterial)
        {
            DisplayUGUI.material = NewMaterial;
        }

        public void Seek(double NewTime)
        {
            currentPlayer.Seek(NewTime);
        }

        public void SeekToFrame(int Frame)
        {
            currentPlayer.SeekToFrame(Frame);
        }

        private void playVideo(bool FadeEffect = true)
        {
            var _mediaPlayer = currentPlayer;
            DisplayUGUI.CurrentMediaPlayer = _mediaPlayer.GetComponent<MediaPlayer>();
            _mediaPlayer.Play();
            if (FadeEffect)
                playFadeEffect();
        }

        private void playFadeEffect()
        {
            if (!FadePlay)
                return;
            DOTween
                .Sequence()
                .Append(DisplayUGUI.DOColor(Color.clear, 0.15f))
                .Append(DisplayUGUI.DOColor(Color.white, 0.45f))
                .PlayForward();
        }

        private void stopVideo()
        {
            if (currentPlayer == null)
                return;
            currentPlayer.Rewind(true);
        }

        // Start is called before the first frame update
        void Start() { }

        // Update is called once per frame
        void Update() { }
    }
}
