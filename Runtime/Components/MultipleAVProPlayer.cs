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
        private readonly UnityEvent<AVProPlayer> onPlayerChanged = new();

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

        private readonly Indexer videoIndex = new(0);
        private List<string> videoPaths = new();

        public IObservable<IList<AVProPlayer>> PrepareVideos(
            string videoDir,
            string searchPattern = "*.mp4",
            SearchOption searchOption = SearchOption.TopDirectoryOnly,
            Action<AVProPlayer> settingCallback = null
        )
        {
            var _patterns = searchPattern.Split('|').ToList();
            var _videoPaths = Directory
                .GetFiles(videoDir, "*.*", searchOption)
                .Where(_path => _patterns.Exists(_pattern => _path.EndsWith(_pattern)));
            return PrepareVideos(_videoPaths, settingCallback);
        }

        public IObservable<IList<AVProPlayer>> PrepareVideos(
            IEnumerable<string> VideoPaths,
            Action<AVProPlayer> settingCallback = null
        )
        {
            transform
                .Children()
                .ToList()
                .ForEach(_child =>
                {
                    var _avProPlayer = _child.Get<AVProPlayer>();
                    _avProPlayer.ClearPlayHandlers();
                    _avProPlayer.MediaPlayer.CloseMedia();
                });

            if (VideoPaths.Count() <= 0)
            {
                return Observable.Empty<IList<AVProPlayer>>();
            }

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

            if (PoolManager.Pools.ContainsKey(gameObject.name + "_pool"))
            {
                PoolManager.Pools.Destroy(gameObject.name + "_pool");
            }

            PoolManager.Pools.Create(gameObject.name + "_pool");
            var _mediaPlayerPool = PoolManager.Pools[gameObject.name + "_pool"];
            _mediaPlayerPool.DespawnAll();

            _mediaPlayerPool.CreatePrefabPool(_playerPrefabPool);

            DisplayUGUI.color = Color.clear;

            return Observable
                .Zip(
                    VideoPaths.Select(_path =>
                    {
                        var _mediaPlayer = _mediaPlayerPool.Spawn(
                            _defaultPlayer.transform,
                            transform
                        );
                        _mediaPlayer.SetAsLastSibling();
                        var _avproPlayer = _mediaPlayer.GetComponent<AVProPlayer>();
                        var _mediaPathType = MediaPathType.RelativeToStreamingAssetsFolder;
                        if (Path.IsPathRooted(_path))
                        {
                            _mediaPathType = MediaPathType.AbsolutePathOrURL;
                        }
                        _avproPlayer.MediaPlayer.OpenMedia(_mediaPathType, _path, false);
                        return _avproPlayer
                            .OnMetaDataReadyAsObservable()
                            .First()
                            .Select(_ => _avproPlayer);
                    })
                )
                .First()
                .DoOnCompleted(() =>
                {
                    GameObject.DestroyImmediate(_defaultPlayer);
                    onPlayerChanged.Invoke(currentPlayer);
                });
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

            fadePlay(
                FadePlay,
                () =>
                {
                    StopVideo();
                    videoIndex.Set(_idx);
                    onPlayerChanged.Invoke(currentPlayer);
                    DisplayUGUI.CurrentMediaPlayer = currentPlayer.GetComponent<MediaPlayer>();
                    currentPlayer.Play(Path, OnCompleted, Loop, StartTime, EndTime);
                }
            );
        }

        public void TogglePlay()
        {
            if (currentPlayer == null)
                return;
            if (currentPlayer.IsPaused)
            {
                playVideo(false);
            }
            else
            {
                Pause();
            }
        }

        public bool FadePlay { get; set; } = true;

        public void Stop()
        {
            StopVideo();
        }

        public void SwitchNext(bool bAutoPlay = true)
        {
            StopVideo();
            videoIndex.Next();
            onPlayerChanged.Invoke(currentPlayer);
            if (bAutoPlay)
            {
                playVideo();
            }
        }

        public void SwitchPrev(bool bAutoPlay = true)
        {
            StopVideo();
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

        public void SetRenderMedia(int mediaIndex)
        {
            videoIndex.Set(mediaIndex);
            ReferenceRenderMedia();
        }

        /// <summary>
        /// Reference the current media player to the display UGUI
        /// </summary>
        public void ReferenceRenderMedia()
        {
            DisplayUGUI.CurrentMediaPlayer = currentPlayer.GetComponent<MediaPlayer>();
        }

        private void playVideo(bool bFade = true)
        {
            var _mediaPlayer = currentPlayer;
            fadePlay(
                bFade,
                () =>
                {
                    DisplayUGUI.CurrentMediaPlayer = _mediaPlayer.GetComponent<MediaPlayer>();
                    _mediaPlayer.Play();
                }
            );
        }

        private void fadePlay(bool bFade, Action onCleared = null)
        {
            if (!bFade)
            {
                onCleared?.Invoke();
                return;
            }

            DOTween
                .Sequence()
                .Append(
                    DisplayUGUI.DOColor(Color.clear, 0.15f).OnComplete(() => onCleared?.Invoke())
                )
                .Append(DisplayUGUI.DOColor(Color.white, 0.45f))
                .PlayForward();
        }

        private void StopVideo()
        {
            if (currentPlayer == null)
                return;
            currentPlayer.Rewind(true);
            DisplayUGUI.color = Color.clear;
        }
    }
}
