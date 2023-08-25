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

namespace UNIHper
{
    public class MultipleAVProPlayer : MonoBehaviour
    {
        private readonly UnityEvent<AVProPlayer> onPlayerBeforeChanged = new();
        private readonly UnityEvent<AVProPlayer> onPlayerAfterChanged = new();

        public IObservable<AVProPlayer> OnPlayerBeforeChangedAsObservable()
        {
            return onPlayerBeforeChanged.AsObservable();
        }

        public IObservable<AVProPlayer> OnPlayerAfterChangedAsObservable()
        {
            return onPlayerAfterChanged.AsObservable();
        }

        public double CurrentTime
        {
            get
            {
                if (CurrentPlayer == null)
                    return 0;
                return this.CurrentPlayer.CurrentTime;
            }
        }

        public bool Loop { get; set; } = false;

        private readonly Indexer videoIndex = new(0);
        private List<string> videoPaths = new();

        public void PrepareVideos(
            string videoDir,
            string searchPattern = "*.mp4",
            SearchOption searchOption = SearchOption.TopDirectoryOnly,
            Action<AVProPlayer> settingCallback = null
        )
        {
            if (!Path.IsPathRooted(videoDir))
            {
                videoDir = Path.Combine(Application.streamingAssetsPath, videoDir);
            }
            var _patterns = searchPattern.Replace("*", "").Split('|').ToList();
            var _videoPaths = Directory
                .GetFiles(videoDir, "*.*", searchOption)
                .Where(_path => _patterns.Exists(_pattern => _path.EndsWith(_pattern)));
            _videoPaths = _videoPaths
                .Select(
                    _path =>
                        _path.StartsWith(Application.streamingAssetsPath + "\\")
                            ? _path.Replace(Application.streamingAssetsPath + "\\", "")
                            : _path
                )
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

        public void PrepareVideos(
            IEnumerable<string> VideoPaths,
            Action<AVProPlayer> settingCallback = null
        )
        {
            if (VideoPaths.Count() <= 0)
            {
                Debug.LogWarning("No video found.");
            }

            videoIndex.SetMax(VideoPaths.Count() - 1);
            this.videoPaths = VideoPaths.ToList();

            listPlayer = this.Get<PlaylistMediaPlayer>();
            listPlayer.LoopMode = PlaylistMediaPlayer.PlaylistLoopMode.None;
            listPlayer.AutoCloseVideo = false;
            listPlayer.AutoProgress = true;

            // Build the playlist
            listPlayer.Playlist.Items.Clear();
            videoPaths.ForEach(_videoPath =>
            {
                MediaPlaylist.MediaItem _mediaItem = new MediaPlaylist.MediaItem();
                _mediaItem.mediaPath = new MediaPath(
                    _videoPath,
                    MediaPathType.RelativeToStreamingAssetsFolder
                );
                _mediaItem.startMode = PlaylistMediaPlayer.StartMode.Manual;
                _mediaItem.progressMode = PlaylistMediaPlayer.ProgressMode.OnFinish;
                // item.isOverrideTransition = false;
                // item.overrideTransition = PlaylistMediaPlayer.Transition.Black;
                // item.overrideTransitionDuration = 1.0f;
                //item.overrideTransitionEasing = PlaylistMediaPlayer.Easing.Preset.Linear;
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

        public bool IsPaused
        {
            get
            {
                if (CurrentPlayer == null)
                    return false;
                return CurrentPlayer.IsPaused;
            }
        }

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
                if (CurrentPlayer == null)
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
            listPlayer.Play();
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
        public void Play(
            string Path,
            Action<AVProPlayer> OnCompleted,
            bool Loop = false,
            double StartTime = 0f,
            double EndTime = 0f,
            bool seek2StartAfterFinished = true
        )
        {
            var _idx = FindVideoIndex(Path);
            if (_idx == -1)
            {
                Debug.LogWarning("Video path not found: " + Path);
                return;
            }

            var _avProPlayer = NextPlayer;

            onPlayerBeforeChanged.Invoke(_avProPlayer);
            listPlayer.JumpToItem(_idx);
            videoIndex.Set(_idx);
            onPlayerAfterChanged.Invoke(_avProPlayer);

            _avProPlayer.Play(
                videoPaths[_idx],
                OnCompleted,
                Loop,
                StartTime,
                EndTime,
                seek2StartAfterFinished
            );
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

        public void Switch(string videoName, bool bRewind = false, bool bAutoPlay = false)
        {
            var _idx = FindVideoIndex(videoName);
            if (_idx == -1)
            {
                Debug.LogWarning("Video path not found: " + videoName);
                return;
            }

            Switch(_idx, bRewind, bAutoPlay);
        }

        public void Switch(int mediaIndex, bool bRewind = false, bool bAutoPlay = false)
        {
            if (mediaIndex < 0 || mediaIndex >= videoPaths.Count)
            {
                Debug.LogWarning("mediaIndex out of range: " + mediaIndex);
                return;
            }
            //mediaPlayer.JumpToItem(mediaIndex);
            if (bAutoPlay)
            {
                Play(videoPaths[mediaIndex], null, Loop, 0, 0, false);
            }
            else
            {
                var _avProPlayer = NextPlayer;
                if (!bAutoPlay && _avProPlayer.IsPlaying)
                    _avProPlayer.Pause();
                if (bRewind)
                    _avProPlayer.Rewind();

                onPlayerBeforeChanged.Invoke(_avProPlayer);
                listPlayer.JumpToItem(mediaIndex);
                videoIndex.Set(mediaIndex);
                onPlayerAfterChanged.Invoke(_avProPlayer);
            }
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

        private AVProPlayer CurrentPlayer => listPlayer.CurrentPlayer.Get<AVProPlayer>();
        public AVProPlayer NextPlayer => listPlayer.NextPlayer.Get<AVProPlayer>();
    }
}
