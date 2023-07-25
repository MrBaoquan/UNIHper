using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RenderHeads.Media.AVProVideo;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper
{
    [RequireComponent(typeof(MediaPlayer))]
    public class AVProPlayer : MonoBehaviour
    {
        #region 事件列表
        private UnityEvent<MediaPlayer> OnMetaDataReady = new UnityEvent<MediaPlayer>(); // Triggered when meta data(width, duration etc) is available
        private UnityEvent<MediaPlayer> OnReadyToPlay = new UnityEvent<MediaPlayer>(); // Triggered when the video is loaded and ready to play
        private UnityEvent<MediaPlayer> OnStarted = new UnityEvent<MediaPlayer>(); // Triggered when the playback starts
        private UnityEvent<MediaPlayer> OnFirstFrameReady = new UnityEvent<MediaPlayer>(); // Triggered when the first frame has been rendered
        private UnityEvent<MediaPlayer> OnFinishedPlaying = new UnityEvent<MediaPlayer>(); // Triggered when a non-looping video has finished playing
        private UnityEvent<MediaPlayer> OnClosing = new UnityEvent<MediaPlayer>(); // Triggered when the media is closed
        private UnityEvent<MediaPlayer> OnError = new UnityEvent<MediaPlayer>(); // Triggered when an error occurs
        private UnityEvent<MediaPlayer> OnSubtitleChange = new UnityEvent<MediaPlayer>(); // Triggered when the subtitles change
        private UnityEvent<MediaPlayer> OnStalled = new UnityEvent<MediaPlayer>(); // Triggered when media is stalled (eg. when lost connection to media stream) - Currently only supported on Windows platforms
        private UnityEvent<MediaPlayer> OnUnstalled = new UnityEvent<MediaPlayer>(); // Triggered when media is resumed form a stalled state (eg. when lost connection is re-established)
        private UnityEvent<MediaPlayer> OnResolutionChanged = new UnityEvent<MediaPlayer>(); // Triggered when the resolution of the video has changed (including the load) Useful for adaptive streams
        private UnityEvent<MediaPlayer> OnStartedSeeking = new UnityEvent<MediaPlayer>(); // Triggered when seeking begins
        private UnityEvent<MediaPlayer> OnFinishedSeeking = new UnityEvent<MediaPlayer>(); // Triggered when seeking has finished
        private UnityEvent<MediaPlayer> OnStartedBuffering = new UnityEvent<MediaPlayer>(); // Triggered when buffering begins
        private UnityEvent<MediaPlayer> OnFinishedBuffering = new UnityEvent<MediaPlayer>(); // Triggered when buffering has finished
        private UnityEvent<MediaPlayer> OnPropertiesChanged = new UnityEvent<MediaPlayer>(); // Triggered when any properties (eg stereo packing are changed) - this has to be triggered manually
        private UnityEvent<MediaPlayer> OnPlaylistItemChanged = new UnityEvent<MediaPlayer>(); // Triggered when the new item is played in the playlist
        private UnityEvent<MediaPlayer> OnPlaylistFinished = new UnityEvent<MediaPlayer>(); // Triggered when the playlist reaches the end
        private UnityEvent<MediaPlayer> OnTextTracksChanged = new UnityEvent<MediaPlayer>(); // Triggered when the text tracks are added or removed
        #endregion

        void Reset()
        {
            MediaPlayer.AutoOpen = false;
            MediaPlayer.AutoStart = false;
        }

        private MediaPlayer mediaPlayer;
        public MediaPlayer MediaPlayer
        {
            get
            {
                if (mediaPlayer == null)
                {
                    mediaPlayer = this.GetComponent<MediaPlayer>();
                    registerAllEvents();
                }
                return mediaPlayer;
            }
        }

        /// <summary>
        /// 标识当前播放器是否准备就绪
        /// </summary>
        /// <value></value>
        public bool Ready2Play
        {
            get { return MediaPlayer.Control != null; }
        }

        public bool IsPlaying
        {
            get { return MediaPlayer.Control.IsPlaying(); }
        }

        /// <summary>
        /// 标识当前视频是否处于暂停状态
        /// </summary>
        /// <value></value>
        public bool IsPaused
        {
            get { return MediaPlayer.Control.IsPaused(); }
        }

        public bool IsFinished
        {
            get { return MediaPlayer.Control.IsFinished(); }
        }

        /// <summary>
        /// 当前播放的视频的总时长 seconds
        /// </summary>
        /// <value></value>
        public double Duration
        {
            get { return MediaPlayer.Info.GetDuration(); }
        }

        public int DurationFrames
        {
            get { return mediaPlayer.Info.GetDurationFrames(); }
        }

        public int MaxFrameNumber
        {
            get { return mediaPlayer.Info.GetMaxFrameNumber(); }
        }

        public float PlaybackRate
        {
            get { return MediaPlayer.Control.GetPlaybackRate(); }
        }

        /// <summary>
        /// Current video time in seconds
        /// </summary>
        /// <value></value>
        public double CurrentTime
        {
            get { return MediaPlayer.Control.GetCurrentTime(); }
        }

        public int CurrentFrame
        {
            get { return mediaPlayer.Control.GetCurrentTimeFrames(); }
        }

        public double StartTime { get; protected set; }

        public double EndTime { get; protected set; }

        public IObservable<MediaPlayer> Prepare(string path)
        {
            MediaPlayer.OpenMedia(MediaPathType.RelativeToStreamingAssetsFolder, path, false);
            return OnMetaDataReadyAsObservable().First();
        }

        IDisposable _readyHandler = null;
        private List<IDisposable> playHandlers = new List<IDisposable>();

        /// <summary>
        /// 播放指定地址的视频  可为网络地址 或者本地地址
        /// </summary>
        /// <param name="videoPath">视频地址</param>
        /// <param name="onFinished">播放到结尾回调</param>
        /// <param name="bLoop">是否循环</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        public void Play(
            string videoPath,
            Action<AVProPlayer> onFinished,
            bool bLoop,
            double startTime = 0,
            double endTime = 0,
            bool seek2StartAfterFinished = true
        )
        {
            disposeHandlers(playHandlers);
            if (_readyHandler != null)
            {
                _readyHandler.Dispose();
                _readyHandler = null;
            }

            this.StartTime = startTime;
            this.EndTime = endTime;

            Action _playVideo = () =>
            {
                MediaPlayer.Play();
                bool _bFinished = false;
                var _duration = mediaPlayer.Info.GetDuration();
                endTime = endTime == 0 ? _duration : endTime;

                // 播放结束回调
                Action _onFinished = () =>
                {
                    if (_bFinished)
                        return;

                    _bFinished = true;
                    onFinished?.Invoke(this);
                    MediaPlayer.Pause();

                    if (!bLoop)
                    {
                        // 不循环 直接释放seek回调
                        disposeHandlers(playHandlers);
                    }

                    // 播放结束，是否跳到开始时间
                    if (seek2StartAfterFinished)
                        MediaPlayer.Control.Seek(startTime);
                };

                // 正常播放时间大于指定结束时间
                playHandlers.Add(
                    Observable
                        .EveryUpdate()
                        .Where(_1 => MediaPlayer.Control.GetCurrentTime() >= endTime)
                        .First()
                        .Subscribe(_1 =>
                        {
                            _onFinished();
                            //Debug.LogFormat ("UVA: reach end point c1: {0}", MediaPlayer.Control.GetCurrentTime ());
                        })
                );

                // 视频到达结尾
                playHandlers.Add(
                    OnFinishedPlayingAsObservable()
                        .Subscribe(_1 =>
                        {
                            //Debug.LogFormat ("UVA: reach end point  c2: {0}", MediaPlayer.Control.GetCurrentTime ());
                            _onFinished();
                        })
                );
            };

            Action _registerFinishedSeekingEvent = () =>
            {
                playHandlers.Add(
                    OnFinishedSeekingAsObservable()
                        .Subscribe(_ =>
                        {
                            _playVideo();
                        })
                );
            };

            Action _startSeek = () =>
            {
                _registerFinishedSeekingEvent();
                var _currentTime = MediaPlayer.Control.GetCurrentTime();
                if (_currentTime != startTime)
                {
                    MediaPlayer.Control.Seek(startTime);
                }
                else
                {
                    _playVideo();
                }
            };
            MediaPlayer.Loop = bLoop;
            if (MediaPlayer.MediaPath.Path != videoPath || MediaPlayer.Control == null)
            {
                _readyHandler = OnFirstFrameReadyAsObservable()
                    .Subscribe(_ =>
                    {
                        _readyHandler.Dispose();
                        _readyHandler = null;
                        _startSeek();
                    });
                var _mediaPathType = MediaPathType.RelativeToStreamingAssetsFolder;
                if (Path.IsPathRooted(videoPath))
                {
                    _mediaPathType = MediaPathType.AbsolutePathOrURL;
                }
                MediaPlayer.OpenMedia(_mediaPathType, videoPath, false);
            }
            else
            {
                _startSeek();
            }
        }

        public void ClearPlayHandlers()
        {
            disposeHandlers(playHandlers);
        }

        public void Rewind(bool pause = false, Action<AVProPlayer> onCompleted = null)
        {
            ClearPlayHandlers();
            if (pause)
                this.Pause();
            Seek(this.StartTime, onCompleted);
        }

        public void Rewind(Action<AVProPlayer> onCompleted)
        {
            Rewind(false, onCompleted);
        }

        public void Play()
        {
            if (!Ready2Play)
            {
                return;
            }
            MediaPlayer.Control.Play();
        }

        public void Pause()
        {
            if (!Ready2Play)
            {
                return;
            }
            MediaPlayer.Control.Pause();
        }

        public void Stop()
        {
            Rewind(true);
        }

        public void Seek(double InTime, Action<AVProPlayer> onCompleted = null)
        {
            if (!Ready2Play)
            {
                return;
            }
            OnFinishedSeekingAsObservable()
                .First()
                .Subscribe(_ =>
                {
                    onCompleted?.Invoke(this);
                });
            MediaPlayer.Control.Seek(InTime);
        }

        public void SeekToFrame(int Frame, Action<AVProPlayer> onFinished = null)
        {
            if (!Ready2Play)
                return;
            OnFinishedSeekingAsObservable()
                .First()
                .Subscribe(_ =>
                {
                    onFinished?.Invoke(this);
                });
            mediaPlayer.Control.SeekToFrame(Frame);
        }

        public void SetPlaybackRate(float rate)
        {
            mediaPlayer.Control.SetPlaybackRate(rate);
        }

        public void SetVolume(float volume)
        {
            mediaPlayer.Control.SetVolume(volume);
        }

        public void MuteAudio(bool bMute)
        {
            mediaPlayer.Control.MuteAudio(bMute);
        }

        private void disposeHandlers(List<IDisposable> InHandlers)
        {
            InHandlers.ForEach(_handler =>
            {
                if (_handler != null)
                {
                    _handler.Dispose();
                    _handler = null;
                }
            });
            InHandlers.Clear();
        }

        #region  播放器事件
        public IObservable<MediaPlayer> OnMetaDataReadyAsObservable()
        {
            return OnMetaDataReady.AsObservable();
        }

        public IObservable<MediaPlayer> OnReadyToPlayAsObservable()
        {
            return OnReadyToPlay.AsObservable();
        }

        public IObservable<MediaPlayer> OnStartedAsObservable()
        {
            return OnStarted.AsObservable();
        }

        public IObservable<MediaPlayer> OnFirstFrameReadyAsObservable()
        {
            return OnFirstFrameReady.AsObservable();
        }

        public IObservable<MediaPlayer> OnFinishedPlayingAsObservable()
        {
            return OnFinishedPlaying.AsObservable();
        }

        public IObservable<MediaPlayer> OnClosingAsObservable()
        {
            return OnClosing.AsObservable();
        }

        public IObservable<MediaPlayer> OnErrorAsObservable()
        {
            return OnError.AsObservable();
        }

        public IObservable<MediaPlayer> OnSubtitleChangeAsObservable()
        {
            return OnSubtitleChange.AsObservable();
        }

        public IObservable<MediaPlayer> OnStalledAsObservable()
        {
            return OnStalled.AsObservable();
        }

        public IObservable<MediaPlayer> OnUnstalledAsObservable()
        {
            return OnUnstalled.AsObservable();
        }

        public IObservable<MediaPlayer> OnResolutionChangedAsObservable()
        {
            return OnResolutionChanged.AsObservable();
        }

        public IObservable<MediaPlayer> OnStartedSeekingAsObservable()
        {
            return OnStartedSeeking.AsObservable();
        }

        public IObservable<MediaPlayer> OnFinishedSeekingAsObservable()
        {
            return OnFinishedSeeking.AsObservable();
        }

        public IObservable<MediaPlayer> OnStartedBufferingAsObservable()
        {
            return OnStartedBuffering.AsObservable();
        }

        public IObservable<MediaPlayer> OnFinishedBufferingAsObservable()
        {
            return OnFinishedBuffering.AsObservable();
        }

        public IObservable<MediaPlayer> OnPropertiesChangedAsObservable()
        {
            return OnPropertiesChanged.AsObservable();
        }

        public IObservable<MediaPlayer> OnPlaylistItemChangedAsObservable()
        {
            return OnPlaylistItemChanged.AsObservable();
        }

        public IObservable<MediaPlayer> OnPlaylistFinishedAsObservable()
        {
            return OnPlaylistFinished.AsObservable();
        }

        public IObservable<MediaPlayer> OnTextTracksChangedAsObservable()
        {
            return OnTextTracksChanged.AsObservable();
        }
        #endregion

        // 注册所有播放器相关事件
        private void registerAllEvents()
        {
            MediaPlayer.Events.AddListener(
                (_media, _type, err) =>
                {
                    // Debug.LogWarningFormat ("new event: {0}", _type);
                    switch (_type)
                    {
                        case MediaPlayerEvent.EventType.MetaDataReady: // Triggered when meta data(width, duration etc) is available
                            OnMetaDataReady.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.ReadyToPlay: // Triggered when the video is loaded and ready to play
                            OnReadyToPlay.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.Started: // Triggered when the playback starts
                            OnStarted.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.FirstFrameReady: // Triggered when the first frame has been rendered
                            OnFirstFrameReady.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.FinishedPlaying: // Triggered when a non-looping video has finished playing
                            OnFinishedPlaying.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.Closing: // Triggered when the media is closed
                            OnClosing.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.Error: // Triggered when an error occurs
                            OnError.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.SubtitleChange: // Triggered when the subtitles change
                            OnSubtitleChange.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.Stalled: // Triggered when media is stalled (eg. when lost connection to media stream) - Currently only supported on Windows platforms
                            OnStalled.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.Unstalled: // Triggered when media is resumed form a stalled state (eg. when lost connection is re-established)
                            OnUnstalled.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.ResolutionChanged: // Triggered when the resolution of the video has changed (including the load) Useful for adaptive streams
                            OnResolutionChanged.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.StartedSeeking: // Triggered when seeking begins
                            OnStartedSeeking.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.FinishedSeeking: // Triggered when seeking has finished
                            OnFinishedSeeking.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.StartedBuffering: // Triggered when buffering begins
                            OnStartedBuffering.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.FinishedBuffering: // Triggered when buffering has finished
                            OnFinishedBuffering.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.PropertiesChanged: // Triggered when any properties (eg stereo packing are changed) - this has to be triggered manually
                            OnPropertiesChanged.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.PlaylistItemChanged: // Triggered when the new item is played in the playlist
                            OnPlaylistItemChanged.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.PlaylistFinished: // Triggered when the playlist reaches the end
                            OnPlaylistFinished.Invoke(_media);
                            break;
                    }
                }
            );
        }
    }
}
