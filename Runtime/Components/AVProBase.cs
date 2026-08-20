using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RenderHeads.Media.AVProVideo;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper
{
    using UniRx;

    public class AVProBase : MonoBehaviour
    {
        #region 成员变量
        /// <summary>
        /// 标识当前播放器是否准备就绪
        /// </summary>
        /// <value></value>
        public virtual bool Ready2Play
        {
            get { return MediaPlayer.Control != null && MediaPlayer.Control.HasMetaData(); }
        }

        /// <summary>
        /// 标识当前视频是否处于暂停状态
        /// </summary>
        /// <value></value>
        public virtual bool IsPaused
        {
            get { return MediaPlayer.Control != null && MediaPlayer.Control.IsPaused(); }
        }

        public virtual bool IsFinished
        {
            get { return MediaPlayer.Control != null && MediaPlayer.Control.IsFinished(); }
        }

        /// <summary>
        /// 当前播放的视频的总时长 seconds
        /// </summary>
        /// <value></value>
        public virtual double Duration
        {
            get { return MediaPlayer.Info != null ? MediaPlayer.Info.GetDuration() : 0; }
        }

        public virtual int DurationFrames
        {
            get { return MediaPlayer.Info != null ? MediaPlayer.Info.GetDurationFrames() : 0; }
        }

        public virtual int MaxFrameNumber
        {
            get { return MediaPlayer.Info != null ? MediaPlayer.Info.GetMaxFrameNumber() : 0; }
        }

        public virtual float PlaybackRate
        {
            get { return MediaPlayer.Control != null ? MediaPlayer.Control.GetPlaybackRate() : 0f; }
        }

        #endregion

        #region 事件列表
        protected readonly UnityEvent<MediaPlayer> OnMetaDataReady = new(); // Triggered when meta data(width, duration etc) is available
        protected readonly UnityEvent<MediaPlayer> OnReadyToPlay = new(); // Triggered when the video is loaded and ready to play
        protected readonly UnityEvent<MediaPlayer> OnStarted = new(); // Triggered when the playback starts
        protected readonly UnityEvent<MediaPlayer> OnFirstFrameReady = new(); // Triggered when the first frame has been rendered
        protected readonly UnityEvent<MediaPlayer> OnFinishedPlaying = new(); // Triggered when a non-looping video has finished playing
        protected readonly UnityEvent<MediaPlayer> OnReachedEnd = new(); // Triggered when the video reaches the end (only in loop mode)
        protected readonly UnityEvent<MediaPlayer> OnClosing = new(); // Triggered when the media is closed
        protected readonly UnityEvent<MediaPlayer> OnError = new(); // Triggered when an error occurs
        protected readonly UnityEvent<MediaPlayer> OnSubtitleChange = new(); // Triggered when the subtitles change
        protected readonly UnityEvent<MediaPlayer> OnStalled = new(); // Triggered when media is stalled (eg. when lost connection to media stream) - Currently only supported on Windows platforms
        protected readonly UnityEvent<MediaPlayer> OnUnstalled = new(); // Triggered when media is resumed form a stalled state (eg. when lost connection is re-established)
        protected readonly UnityEvent<MediaPlayer> OnResolutionChanged = new(); // Triggered when the resolution of the video has changed (including the load) Useful for adaptive streams
        protected readonly UnityEvent<MediaPlayer> OnStartedSeeking = new(); // Triggered when seeking begins
        protected readonly UnityEvent<MediaPlayer> OnFinishedSeeking = new(); // Triggered when seeking has finished
        protected readonly UnityEvent<MediaPlayer> OnStartedBuffering = new(); // Triggered when buffering begins
        protected readonly UnityEvent<MediaPlayer> OnFinishedBuffering = new(); // Triggered when buffering has finished
        protected readonly UnityEvent<MediaPlayer> OnPropertiesChanged = new(); // Triggered when any properties (eg stereo packing are changed) - this has to be triggered manually
        protected readonly UnityEvent<MediaPlayer> OnPlaylistItemChanged = new(); // Triggered when the new item is played in the playlist
        protected readonly UnityEvent<MediaPlayer> OnPlaylistFinished = new(); // Triggered when the playlist reaches the end
        protected readonly UnityEvent<MediaPlayer> OnTextTracksChanged = new(); // Triggered when the text tracks are added or removed

        // Paused && Unpaused
        protected readonly UnityEvent<MediaPlayer> OnPaused = new();
        protected readonly UnityEvent<MediaPlayer> OnUnpaused = new();

        // Seek Event With Target Time
        protected readonly UnityEvent<MediaPlayer, float> OnRequestSeek = new();

        #endregion
        protected readonly CompositeDisposable _playDisposables = new CompositeDisposable();

        public virtual void ClearPlayHandlers()
        {
            Log($"dispose play handlers");
            _playDisposables.Clear();
        }

        private MediaPlayer _mediaPlayer;
        private bool _eventsRegistered = false;

        public MediaPlayer MediaPlayer
        {
            get
            {
                if (_mediaPlayer == null)
                {
                    _mediaPlayer = this.GetComponent<MediaPlayer>();
                    if (_mediaPlayer == null)
                        _mediaPlayer = this.gameObject.AddComponent<MediaPlayer>();
                    if (Application.isPlaying && !_eventsRegistered)
                    {
                        _eventsRegistered = true;
                        registerAllEvents();
                    }
                }
                return _mediaPlayer;
            }
        }

        #region  播放器事件
        public virtual IObservable<MediaPlayer> OnMetaDataReadyAsObservable()
        {
            return OnMetaDataReady.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnPausedAsObservable()
        {
            return OnPaused.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnUnpausedAsObservable()
        {
            return OnUnpaused.AsObservable();
        }

        public virtual IObservable<(MediaPlayer mediaPlayer, float targetTime)> OnRequestSeekAsObservable()
        {
            return OnRequestSeek.AsObservable().Select(x => (x.Item1, x.Item2));
        }

        public virtual IObservable<MediaPlayer> OnReadyToPlayAsObservable()
        {
            return OnReadyToPlay.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnStartedAsObservable()
        {
            return OnStarted.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnFirstFrameReadyAsObservable()
        {
            return OnFirstFrameReady.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnFinishedPlayingAsObservable()
        {
            return OnFinishedPlaying.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnReachedEndAsObservable()
        {
            return OnReachedEnd.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnClosingAsObservable()
        {
            return OnClosing.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnErrorAsObservable()
        {
            return OnError.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnSubtitleChangeAsObservable()
        {
            return OnSubtitleChange.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnStalledAsObservable()
        {
            return OnStalled.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnUnstalledAsObservable()
        {
            return OnUnstalled.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnResolutionChangedAsObservable()
        {
            return OnResolutionChanged.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnStartedSeekingAsObservable()
        {
            return OnStartedSeeking.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnFinishedSeekingAsObservable()
        {
            return OnFinishedSeeking.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnStartedBufferingAsObservable()
        {
            return OnStartedBuffering.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnFinishedBufferingAsObservable()
        {
            return OnFinishedBuffering.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnPropertiesChangedAsObservable()
        {
            return OnPropertiesChanged.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnPlaylistItemChangedAsObservable()
        {
            return OnPlaylistItemChanged.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnPlaylistFinishedAsObservable()
        {
            return OnPlaylistFinished.AsObservable();
        }

        public virtual IObservable<MediaPlayer> OnTextTracksChangedAsObservable()
        {
            return OnTextTracksChanged.AsObservable();
        }

        #endregion

        #region 公共接口
        public static bool EnableLog { get; set; } = false;

        public virtual bool IsPlaying
        {
            get { return MediaPlayer.Control != null && MediaPlayer.Control.IsPlaying(); }
        }

        public virtual double CurrentTime
        {
            get { return MediaPlayer.Control != null ? MediaPlayer.Control.GetCurrentTime() : 0; }
        }

        public virtual int CurrentFrame
        {
            get { return MediaPlayer.Control != null ? MediaPlayer.Control.GetCurrentTimeFrames() : 0; }
        }

        protected void Log(string msg)
        {
            if (!EnableLog)
                return;

            Debug.LogWarning($"[AVProPlayer]: {name} {msg}");
        }

        public virtual void TogglePlay()
        {
            if (this.IsPlaying)
            {
                this.Pause();
            }
            else
            {
                this.Play();
            }
        }

        public virtual void Play(bool withEvent = true)
        {
            MediaPlayer.Play();
            if (withEvent)
                OnUnpaused.Invoke(MediaPlayer);
        }

        public virtual void Pause(bool withEvent = true)
        {
            Log($" pause requested");
            MediaPlayer.Pause();
            if (withEvent)
                OnPaused.Invoke(MediaPlayer);
        }

        public virtual void Stop()
        {
            MediaPlayer.Stop();
            ClearPlayHandlers();
        }

        public virtual void Rewind(bool pause)
        {
            MediaPlayer.Rewind(pause);
        }

        protected void __seek(double time)
        {
            Log($" seek to {time}");
            OnRequestSeek.Invoke(MediaPlayer, (float)time);
            // 计算时间容差（约1帧的时间）
            double frameTolerance = Duration > 0 && DurationFrames > 0 ? Duration / DurationFrames : 0.033; // 默认按30fps计算，约0.033秒

            if (Math.Abs(CurrentTime - time) <= frameTolerance)
            {
                Log($"seek skipped to {time}");
                OnFinishedSeeking.Invoke(MediaPlayer);
                return;
            }

            MediaPlayer.Control.Seek(time);
#if (UNITY_EDITOR_WIN) || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
            // TODO: DirectShow 驱动下，没有seek相关事件 seek 好像是同步的，需要后续验证
            var optionsWindows = MediaPlayer.GetCurrentPlatformOptions() as RenderHeads.Media.AVProVideo.MediaPlayer.OptionsWindows;

            if (optionsWindows.videoApi == RenderHeads.Media.AVProVideo.Windows.VideoApi.DirectShow)
            {
                Log($" DirectShow seek completed to {time}");
                OnFinishedSeeking.Invoke(MediaPlayer);
            }
#endif
        }

        public void CloseMedia()
        {
            MediaPlayer.CloseMedia();
        }

        // 组件内置操作标识, 启用该标识时，禁用外部api调用

        protected bool _builtSeekOperation { get; set; } = false;
        public bool BuiltInOperation => _builtSeekOperation;

        public virtual void Seek(double time)
        {
            __seek(time);
        }

        #endregion

        // 注册所有播放器相关事件
        protected void registerAllEvents()
        {
            MediaPlayer.Events.AddListener(
                (_media, _type, err) =>
                {
                    // Debug.LogWarningFormat($"{gameObject.name} OnEvent: {_type}, {err}");
                    switch (_type)
                    {
                        case MediaPlayerEvent.EventType.MetaDataReady: // Triggered when meta data(width, duration etc) is available
                            OnMetaDataReady.Invoke(_media);
                            break;
                        case MediaPlayerEvent.EventType.ReadyToPlay: // Triggered when the video is loaded and ready to play
                            OnReadyToPlay.Invoke(_media);
                            break;

                        // case MediaPlayerEvent.EventType.Unpaused:
                        //     OnUnpaused.Invoke(_media);
                        //     break;

                        // case MediaPlayerEvent.EventType.Paused:
                        //     OnPaused.Invoke(_media);
                        //     break;

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
