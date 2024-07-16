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

    [RequireComponent(typeof(MediaPlayer))]
    public class AVProPlayer : MonoBehaviour
    {
        #region 事件列表
        private readonly UnityEvent<MediaPlayer> OnMetaDataReady = new(); // Triggered when meta data(width, duration etc) is available
        private readonly UnityEvent<MediaPlayer> OnReadyToPlay = new(); // Triggered when the video is loaded and ready to play
        private readonly UnityEvent<MediaPlayer> OnStarted = new(); // Triggered when the playback starts
        private readonly UnityEvent<MediaPlayer> OnFirstFrameReady = new(); // Triggered when the first frame has been rendered
        private readonly UnityEvent<MediaPlayer> OnFinishedPlaying = new(); // Triggered when a non-looping video has finished playing
        private readonly UnityEvent<MediaPlayer> OnClosing = new(); // Triggered when the media is closed
        private readonly UnityEvent<MediaPlayer> OnError = new(); // Triggered when an error occurs
        private readonly UnityEvent<MediaPlayer> OnSubtitleChange = new(); // Triggered when the subtitles change
        private readonly UnityEvent<MediaPlayer> OnStalled = new(); // Triggered when media is stalled (eg. when lost connection to media stream) - Currently only supported on Windows platforms
        private readonly UnityEvent<MediaPlayer> OnUnstalled = new(); // Triggered when media is resumed form a stalled state (eg. when lost connection is re-established)
        private readonly UnityEvent<MediaPlayer> OnResolutionChanged = new(); // Triggered when the resolution of the video has changed (including the load) Useful for adaptive streams
        private readonly UnityEvent<MediaPlayer> OnStartedSeeking = new(); // Triggered when seeking begins
        private readonly UnityEvent<MediaPlayer> OnFinishedSeeking = new(); // Triggered when seeking has finished
        private readonly UnityEvent<MediaPlayer> OnStartedBuffering = new(); // Triggered when buffering begins
        private readonly UnityEvent<MediaPlayer> OnFinishedBuffering = new(); // Triggered when buffering has finished
        private readonly UnityEvent<MediaPlayer> OnPropertiesChanged = new(); // Triggered when any properties (eg stereo packing are changed) - this has to be triggered manually
        private readonly UnityEvent<MediaPlayer> OnPlaylistItemChanged = new(); // Triggered when the new item is played in the playlist
        private readonly UnityEvent<MediaPlayer> OnPlaylistFinished = new(); // Triggered when the playlist reaches the end
        private readonly UnityEvent<MediaPlayer> OnTextTracksChanged = new(); // Triggered when the text tracks are added or removed
        #endregion

        void Reset()
        {
            MediaPlayer.AutoOpen = false;
            MediaPlayer.AutoStart = false;
        }

#if (UNITY_EDITOR_WIN) || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
        public AVProPlayer SetWindowsVideoAPI(
            RenderHeads.Media.AVProVideo.Windows.VideoApi videoApi
        )
        {
            var _platformOptions =
                MediaPlayer.GetCurrentPlatformOptions()
                as RenderHeads.Media.AVProVideo.MediaPlayer.OptionsWindows;
            _platformOptions.videoApi = videoApi;
            return this;
        }
#endif

        private MediaPlayer _mediaPlayer;
        public MediaPlayer MediaPlayer
        {
            get
            {
                if (_mediaPlayer == null)
                {
                    _mediaPlayer = this.GetComponent<MediaPlayer>();
                    if (_mediaPlayer == null)
                        _mediaPlayer = this.gameObject.AddComponent<MediaPlayer>();
                    if (Application.isPlaying)
                        registerAllEvents();
                }
                return _mediaPlayer;
            }
        }

        /// <summary>
        /// 标识当前播放器是否准备就绪
        /// </summary>
        /// <value></value>
        public bool Ready2Play
        {
            get { return MediaPlayer.Control != null && MediaPlayer.Control.HasMetaData(); }
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
            get { return MediaPlayer.Info.GetDurationFrames(); }
        }

        public int MaxFrameNumber
        {
            get { return MediaPlayer.Info.GetMaxFrameNumber(); }
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
            get { return MediaPlayer.Control.GetCurrentTimeFrames(); }
        }

        public double StartTime { get; protected set; }

        public double EndTime { get; protected set; }

        public IObservable<MediaPlayer> Prepare(string path)
        {
            MediaPlayer.OpenMedia(MediaPathType.RelativeToStreamingAssetsFolder, path, false);
            return OnMetaDataReadyAsObservable().First();
        }

        IDisposable _readyHandler = null;

        //private List<IDisposable> playHandlers = new List<IDisposable>();
        CompositeDisposable _playDisposables = new CompositeDisposable();

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
            ClearPlayHandlers();
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
                var _duration = MediaPlayer.Info.GetDuration();
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
                        _playDisposables.Clear();
                    }

                    // 播放结束，是否跳到开始时间
                    if (seek2StartAfterFinished || bLoop)
                    {
                        __seek(startTime);
                    }
                };

                // 正常播放时间大于指定结束时间
                Observable
                    .EveryUpdate()
                    .Where(_1 => MediaPlayer.Control.GetCurrentTime() >= endTime && !_bFinished)
                    .First()
                    .Subscribe(_1 =>
                    {
                        OnFinishedPlaying.Invoke(MediaPlayer);
                    })
                    .AddTo(_playDisposables);

                // 视频到达结尾
                OnFinishedPlayingAsObservable()
                    .Where(_mPlayer => _mPlayer == MediaPlayer)
                    .First()
                    .Subscribe(_1 =>
                    {
                        _onFinished();
                    })
                    .AddTo(_playDisposables);
            };

            void _registerFinishedSeekingEvent()
            {
                OnFinishedSeekingAsObservable()
                    .Subscribe(_ =>
                    {
                        _playVideo();
                    })
                    .AddTo(_playDisposables);
            }

            void _startSeek()
            {
                _registerFinishedSeekingEvent();
                var _currentTime = MediaPlayer.Control.GetCurrentTime();
                if (_currentTime != startTime)
                {
                    __seek(startTime);
                }
                else
                {
                    _playVideo();
                }
            }

            if (
                MediaPlayer.MediaPath.Path != videoPath
                || MediaPlayer.Control == null
                || !MediaPlayer.Control.HasMetaData()
            )
            {
                _readyHandler = OnFirstFrameReadyAsObservable()
                    .Subscribe(_ =>
                    {
                        _readyHandler.Dispose();
                        _readyHandler = null;
                        _startSeek();
                    });

                var _mediaPathType = MediaPathType.RelativeToStreamingAssetsFolder;

#if UNITY_ANDROID && !UNITY_EDITOR
                _mediaPathType = MediaPathType.RelativeToPersistentDataFolder;
#endif
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

        readonly ReactiveProperty<bool> _isPlaying = new(false);
        readonly ReactiveProperty<bool> _isMute = new(false);
        readonly ReactiveProperty<float> _volume = new(1f);

        public IObservable<AVProPlayer> OnPausedAsObservable()
        {
            return _isPlaying.Where(_ => !_isPlaying.Value).Select(_ => this);
        }

        public IObservable<AVProPlayer> OnMuteChangedAsObservable()
        {
            return _isMute.Select(_ => this);
        }

        public IObservable<AVProPlayer> OnVolumeChangedAsObservable()
        {
            return _volume.Select(_ => this);
        }

        void Update()
        {
            if (MediaPlayer == null)
                return;
            _isPlaying.Value = MediaPlayer.Control.IsPlaying();
            _isMute.Value = MediaPlayer.Control.IsMuted();
            _volume.Value = MediaPlayer.Control.GetVolume();
        }

        public void ClearPlayHandlers()
        {
            _playDisposables.Clear();
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
            ClearPlayHandlers();
            MediaPlayer.Control.Play();
        }

        public void Pause()
        {
            if (!Ready2Play)
            {
                return;
            }
            ClearPlayHandlers();
            MediaPlayer.Control.Pause();
        }

        private void __seek(double time)
        {
            MediaPlayer.Control.Seek(time);
#if (UNITY_EDITOR_WIN) || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
            // TODO: DirectShow 驱动下，没有seek相关事件 seek 好像是同步的，需要后续验证
            var optionsWindows =
                MediaPlayer.GetCurrentPlatformOptions()
                as RenderHeads.Media.AVProVideo.MediaPlayer.OptionsWindows;
            if (optionsWindows.videoApi == RenderHeads.Media.AVProVideo.Windows.VideoApi.DirectShow)
            {
                OnFinishedSeeking.Invoke(MediaPlayer);
            }
#endif
        }

        public void Stop()
        {
            ClearPlayHandlers();
            MediaPlayer.Control?.Stop();
        }

        public void Seek(double InTime, Action<AVProPlayer> onCompleted = null)
        {
            if (!Ready2Play)
            {
                return;
            }
            ClearPlayHandlers();
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
            MediaPlayer.Control?.SeekToFrame(Frame);
        }

        public void SetPlaybackRate(float rate)
        {
            MediaPlayer.Control?.SetPlaybackRate(rate);
        }

        public void SetVolume(float volume)
        {
            MediaPlayer.Control?.SetVolume(volume);
        }

        public void MuteAudio(bool bMute)
        {
            MediaPlayer.Control?.MuteAudio(bMute);
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
                    // Debug.LogWarningFormat($"{gameObject.name} OnEvent: {_type}, {err}");
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
