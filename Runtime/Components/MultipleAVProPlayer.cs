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
using UnityEngine.UI;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System.ComponentModel;
using DNHper;

namespace UNIHper
{
    public class MultipleAVProPlayer : AVProBase
    {
        // private readonly UnityEvent<AVProPlayer> onPlayerBeforeChanged = new();
        // private readonly UnityEvent<AVProPlayer> onPlayerAfterChanged = new();

        // public IObservable<AVProPlayer> OnPlayerBeforeChangedAsObservable()
        // {
        //     return onPlayerBeforeChanged.AsObservable();
        // }

        // public IObservable<AVProPlayer> OnPlayerAfterChangedAsObservable()
        // {
        //     return onPlayerAfterChanged.AsObservable();
        // }

        public double CurrentTime
        {
            get
            {
                if (CurrentPlayer == null)
                    return 0;
                return this.CurrentPlayer.CurrentTime;
            }
        }

        //public bool Loop { get; set; } = false;

        private readonly Indexer videoIndex = new(0);

        // TODO: 此路径应由PlayList的Items决定， 不应另外维护一份
        private List<string> videoPaths = new();

        public void PrepareVideos(string videoDir, string searchPattern = "*.mp4", SearchOption searchOption = SearchOption.TopDirectoryOnly, Action<AVProPlayer> settingCallback = null)
        {
            if (!Path.IsPathRooted(videoDir))
            {
                videoDir = Path.Combine(Application.streamingAssetsPath, videoDir);
            }
            var _patterns = searchPattern.Replace("*", "").Split('|').ToList();
            var _videoPaths = Directory.GetFiles(videoDir, "*.*", searchOption).Where(_path => _patterns.Exists(_pattern => _path.EndsWith(_pattern)));
            _videoPaths = _videoPaths
                .Select(_path => _path.StartsWith(Application.streamingAssetsPath + "\\") ? _path.Replace(Application.streamingAssetsPath + "\\", "") : _path)
                .Select(_path => _path.ToForwardSlash())
                .ToList();
            PrepareVideos(_videoPaths, settingCallback);
        }

        private PlaylistMediaPlayer listPlayer = null;
        public PlaylistMediaPlayer ListPlayer
        {
            get
            {
                if (listPlayer == null)
                {
                    listPlayer = this.Get<PlaylistMediaPlayer>();
                }
                return listPlayer;
            }
        }

        public void PrepareVideos(IEnumerable<string> VideoPaths, Action<AVProPlayer> settingCallback = null)
        {
            if (VideoPaths.Count() <= 0)
            {
                Debug.LogWarning("No video found.");
            }

            this.registerAllEvents();

            videoIndex.SetMax(VideoPaths.Count() - 1);
            this.videoPaths = VideoPaths.ToList();

            listPlayer = this.Get<PlaylistMediaPlayer>();
            listPlayer.LoopMode = PlaylistMediaPlayer.PlaylistLoopMode.None;
            listPlayer.AutoCloseVideo = false;

            // 播放到视频结尾是否自动跳到下一个视频
            listPlayer.AutoProgress = false;

            // Build the playlist
            listPlayer.Playlist.Items.Clear();
            videoPaths.ForEach(_videoPath =>
            {
                MediaPlaylist.MediaItem _mediaItem = new MediaPlaylist.MediaItem();
                _mediaItem.mediaPath = new MediaPath(_videoPath, MediaPathType.RelativeToStreamingAssetsFolder);
                _mediaItem.startMode = PlaylistMediaPlayer.StartMode.Manual;
                _mediaItem.progressMode = PlaylistMediaPlayer.ProgressMode.OnFinish;
                // item.isOverrideTransition = false;
                // item.overrideTransition = PlaylistMediaPlayer.Transition.Black;
                // item.overrideTransitionDuration = 1.0f;
                // item.overrideTransitionEasing = PlaylistMediaPlayer.Easing.Preset.Linear;
                listPlayer.Playlist.Items.Add(_mediaItem);

                //var _playerUI = new GameObject(_videoPath);
                // var _rectTransform = _playerUI.AddComponent<RectTransform>();
                // _rectTransform.anchorMin = Vector2.zero;
                // _rectTransform.anchorMax = Vector2.one;
                // _rectTransform.offsetMin = Vector2.zero;
                // _rectTransform.offsetMax = Vector2.zero;
                // _playerUI.transform.SetParent(transform);
            });
        }

        public void Pause()
        {
            listPlayer.Pause();
        }

        public bool IsPaused => listPlayer.IsPaused();

        public bool IsFinished
        {
            get
            {
                if (CurrentPlayer == null)
                    return false;
                return CurrentPlayer.IsFinished;
            }
        }

        public int CurrentFrame
        {
            get
            {
                if (CurrentPlayer == null || !CurrentPlayer.Ready2Play)
                    return 0;

                return CurrentPlayer.CurrentFrame;
            }
        }

        public double Duration
        {
            get
            {
                if (CurrentPlayer == null)
                    return 0;
                return CurrentPlayer.Duration;
            }
        }
        public int MaxFrameNumber
        {
            get
            {
                if (CurrentPlayer == null)
                    return 0;
                return CurrentPlayer.MaxFrameNumber;
            }
        }

        public void Play()
        {
            ListPlayer.Play();
        }

        public void Play(int videoIndex, Action<AVProPlayer> OnCompleted, bool Loop = false, double StartTime = 0f, double EndTime = 0f, bool seek2StartAfterFinished = true)
        {
            if (videoIndex < 0 || videoIndex >= videoPaths.Count)
            {
                Debug.LogWarning((object)("Video ID out of range: " + videoIndex));
                return;
            }

            _playDisposables.Clear();

            SwitchAsObservable(videoIndex, StartTime)
                .Subscribe(_player =>
                {
                    Debug.Log("[debug] 视频切换完成, 开始播放...");
                    _player.Play(videoPaths[videoIndex], OnCompleted, Loop, StartTime, EndTime, seek2StartAfterFinished);
                });
        }

        /// <summary>
        /// 播放指定路径的视频
        /// </summary>
        /// <param name="Path">视频名</param>
        /// <param name="OnCompleted">播放结束回调</param>
        /// <param name="Loop">是否循环</param>
        /// <param name="StartTime">开始时间</param>
        /// <param name="EndTime">结束时间</param>
        /// <param name="seek2StartAfterFinished">播放完成后是否跳到首帧</param>
        public void Play(string Path, Action<AVProPlayer> OnCompleted, bool Loop = false, double StartTime = 0f, double EndTime = 0f, bool seek2StartAfterFinished = true)
        {
            var _idx = FindVideoIndex(Path);
            if (_idx == -1)
            {
                Debug.LogWarning("Video path not found: " + Path);
                return;
            }

            Play(_idx, OnCompleted, Loop, StartTime, EndTime, seek2StartAfterFinished);
        }

        public void TogglePlay()
        {
            if (listPlayer.IsPaused())
            {
                listPlayer.Play();
            }
            else
            {
                listPlayer.Pause();
            }
        }

        public void Stop()
        {
            listPlayer.Stop();
        }

        public void Rewind(bool pause = true)
        {
            CurrentPlayer.Rewind(pause);
        }

        public void Seek(double NewTime)
        {
            CurrentPlayer.Seek(NewTime);
        }

        public void SeekToFrame(int Frame)
        {
            CurrentPlayer.SeekToFrame(Frame);
        }

        public void Switch(string videoName, bool bRewind = false, bool bAutoPlay = false, float StartTime = 0, float EndTime = 0)
        {
            var _idx = FindVideoIndex(videoName);
            if (_idx == -1)
            {
                Debug.LogWarning("Video path not found: " + videoName);
                return;
            }

            Switch(_idx, bRewind, bAutoPlay, StartTime, EndTime);
        }

        public MediaPlaylist.MediaItem PlaylistItem => listPlayer.PlaylistItem;

        public MediaPlaylist.MediaItem GetMediaItem(int playListIndex)
        {
            if (listPlayer.Playlist.HasItemAt(playListIndex))
            {
                return listPlayer.Playlist.Items[playListIndex];
            }
            return null;
        }

        public MediaPlaylist.MediaItem GetMediaItem(string videoPath)
        {
            return GetMediaItem(FindVideoIndex(videoPath));
        }

        public bool SetMediaLoop(string videoPath, bool bLoop)
        {
            var _mediaItem = GetMediaItem(videoPath);
            if (_mediaItem == null)
            {
                Debug.LogWarning($"media not found, path:{videoPath}");
                return false;
            }

            _mediaItem.loop = bLoop;
            return true;
        }

        public void SetDefaultMediaTransition(PlaylistMediaPlayer.Transition transition)
        {
            ListPlayer.DefaultTransition = transition;
        }

        public bool SetMediaTransition(string videoPath, PlaylistMediaPlayer.Transition transition)
        {
            var _mediaItem = GetMediaItem(videoPath);
            if (_mediaItem == null)
            {
                Debug.LogWarning($"media not found, path:{videoPath}");
                return false;
            }

            _mediaItem.isOverrideTransition = true;
            _mediaItem.overrideTransition = transition;
            return true;
        }

        public void Switch(int mediaIndex, bool bRewind = false, bool bAutoPlay = false, float StartTime = 0, float EndTime = 0)
        {
            var _mediaItem = GetMediaItem(mediaIndex);

            if (_mediaItem == null)
            {
                Debug.LogWarning($"media not found, index:{mediaIndex}");
                return;
            }

            //mediaPlayer.JumpToItem(mediaIndex);
            if (bAutoPlay)
            {
                // Play(videoPaths[mediaIndex], null, Loop, 0, 0, false);
                Play(_mediaItem.mediaPath.Path, null, _mediaItem.loop, StartTime, EndTime, false);
            }
            else
            {
                _playDisposables.Clear();

                OnItemChangedAsObservable()
                    .First()
                    .Subscribe(_ =>
                    {
                        _.Pause();
                        if (bRewind)
                            _.Rewind();
                    })
                    .AddTo(_playDisposables);

                listPlayer.JumpToItem(mediaIndex);
                videoIndex.Set(mediaIndex);
            }
        }

        public IObservable<AVProPlayer> SwitchAsObservable(string videoPath, double startTime = -1)
        {
            return SwitchAsObservable(FindVideoIndex(videoPath), startTime);
        }

        public IObservable<AVProPlayer> SwitchAsObservable(int mediaIndex, double startTime = -1f)
        {
            startTime = Math.Round(startTime, 3);
            return Observable.Create<AVProPlayer>(_observer =>
            {
                _playDisposables.Clear();
                OnItemChangedAsObservable()
                    .First()
                    .Do(_player =>
                    {
                        Debug.Log("OnItemChangedAsObservable: " + _player.name);
                        _player.Pause();
                    })
                    .SelectMany(_player => (startTime == -1 || _player.CurrentTime == startTime) ? Observable.Return(_player) : _player.SeekAsObservable(startTime))
                    .Subscribe(_ =>
                    {
                        Debug.Log($"[debug] 视频切换完成, 当前视频: {_.name}, 开始时间: {startTime}");
                        _observer.OnNext(CurrentPlayer);
                        _observer.OnCompleted();
                    })
                    .AddTo(_playDisposables);

                listPlayer.JumpToItem(mediaIndex);
                videoIndex.Set(mediaIndex);
                return Disposable.Empty;
            });
        }

        public void SwitchNext(bool bRewind = false, bool bAutoPlay = false)
        {
            Switch(videoIndex.NextValue(), bRewind, bAutoPlay);
        }

        public void SwitchPrev(bool bRewind = false, bool bAutoPlay = false)
        {
            Switch(videoIndex.PrevValue(), bRewind, bAutoPlay);
        }

        public void SetPlaybackRate(float rate)
        {
            CurrentPlayer.SetPlaybackRate(rate);
        }

        public void SetVolume(float volume)
        {
            listPlayer.AudioVolume = volume;
        }

        public void MuteAudio(bool bMute)
        {
            listPlayer.AudioMuted = bMute;
        }

        public int FindVideoIndex(string videoName)
        {
            var _idx = videoPaths.FindIndex(_path => _path == videoName);
            if (_idx == -1)
            {
                return videoPaths.FindIndex(_ => _.EndsWith(videoName));
            }
            return _idx;
        }

        public IObservable<AVProPlayer> OnItemChangedAsObservable()
        {
            return this.OnPlaylistItemChangedAsObservable().SelectMany(_ => Observable.NextFrame().Select(_1 => CurrentPlayer));
        }

        public AVProPlayer CurrentPlayer => ListPlayer.CurrentPlayer.Get<AVProPlayer>();
        public AVProPlayer NextPlayer => ListPlayer.NextPlayer.Get<AVProPlayer>();
    }
}
