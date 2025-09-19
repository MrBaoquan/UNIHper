using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using RenderHeads.Media.AVProVideo;
using UniRx;
using UnityEngine;

namespace UNIHper
{
    [RequireComponent(typeof(MediaPlayer))]
    public class AVProPlayer : AVProBase
    {
        void Reset()
        {
            MediaPlayer.AutoOpen = false;
            MediaPlayer.AutoStart = false;
        }

#if (UNITY_EDITOR_WIN) || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
        public AVProBase SetWindowsVideoAPI(RenderHeads.Media.AVProVideo.Windows.VideoApi videoApi)
        {
            var _platformOptions = MediaPlayer.GetCurrentPlatformOptions() as RenderHeads.Media.AVProVideo.MediaPlayer.OptionsWindows;
            _platformOptions.videoApi = videoApi;
            return this;
        }
#endif

        /// <summary>
        /// 标识当前播放器是否准备就绪
        /// </summary>
        /// <value></value>
        public bool Ready2Play
        {
            get { return MediaPlayer.Control != null && MediaPlayer.Control.HasMetaData(); }
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

        public IObservable<AVProPlayer> SwitchAsObservable(string path, double startTime = 0)
        {
            return Observable.Create<AVProPlayer>(observer =>
            {
                var _disposable = new CompositeDisposable();

                OnMetaDataReadyAsObservable()
                    .First()
                    .SelectMany(_ => SeekAsObservable(startTime))
                    .Subscribe(_ =>
                    {
                        observer.OnNext(this);
                        observer.OnCompleted();
                    })
                    .AddTo(_disposable);

                MediaPlayer.OpenMedia(MediaPathType.RelativeToStreamingAssetsFolder, path, false);
                return _disposable;
            });
        }

        public bool Switch(string path)
        {
            return MediaPlayer.OpenMedia(MediaPathType.RelativeToStreamingAssetsFolder, path, false);
        }

        IDisposable _readyHandler = null;

        public Dictionary<string, Texture> cachedDefaultTexes = new();

        public Texture GetCachedDefaultTexture(string videoPath)
        {
            if (cachedDefaultTexes.TryGetValue(videoPath, out var cachedTex))
            {
                return cachedTex;
            }
            return null;
        }

        public bool AutoSwitchCachedFirstFrame { get; set; } = true;

        public void CloseMedia()
        {
            MediaPlayer.CloseMedia();
        }

        public void Play(string videoPath, bool bLoop = true, double startTime = 0, double endTime = 0, bool seek2StartAfterFinished = true)
        {
            Play(videoPath, null, bLoop, startTime, endTime, seek2StartAfterFinished);
        }

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
            bool bLoop = true,
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
            var _videoName = Path.GetFileName(videoPath);
            Log(
                $"request play: {_videoName}, loop: {bLoop}, startTime: {startTime}, endTime: {endTime}, seek2StartAfterFinished: {seek2StartAfterFinished}"
            );

            startTime = Math.Round(startTime, 3);
            endTime = Math.Round(endTime, 3);

            this.StartTime = startTime;
            this.EndTime = endTime;

            bool _notSameSource =
                MediaPlayer.MediaPath.Path != videoPath || MediaPlayer.Control == null || !MediaPlayer.Control.HasMetaData();

            var displayUI = this.Get<DisplayUGUI>();

            var _useFade = _notSameSource && AutoSwitchCachedFirstFrame;

            if (_useFade && displayUI != null)
            {
                displayUI.DefaultTexture = GetCachedDefaultTexture(videoPath);
                displayUI.color = displayUI.DefaultTexture == null ? Color.black : Color.white;
                displayUI.CurrentMediaPlayer = null;
            }

            CompositeDisposable tempPlayDisposables = null;
            Action _playVideo = () =>
            {
                tempPlayDisposables?.Dispose();
                tempPlayDisposables = new CompositeDisposable();

                Log($"do play video: {_videoName} at {startTime}");
                if (_useFade && displayUI != null)
                {
                    displayUI.color = Color.white;
                    displayUI.CurrentMediaPlayer = MediaPlayer;
                }

                MediaPlayer.Play();
                bool _bFinished = false;
                var _duration = MediaPlayer.Info.GetDuration();
                endTime = endTime == 0 ? _duration : endTime;

                // 播放结束回调
                Action _onFinished = () =>
                {
                    Log($"Video Finished: {videoPath}");
                    if (_bFinished)
                        return;

                    _bFinished = true;
                    onFinished?.Invoke(this);
                    MediaPlayer.Pause();

                    if (!bLoop)
                    {
                        // 不循环 直接释放seek回调
                        // _playDisposables.Clear();
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
                        Log($"reach specified end time: {endTime} >= {_duration}");
                        OnFinishedPlaying.Invoke(MediaPlayer);
                    })
                    .AddTo(_playDisposables)
                    .AddTo(tempPlayDisposables);

                // 视频到达结尾
                OnFinishedPlayingAsObservable()
                    .Where(_mPlayer => _mPlayer == MediaPlayer)
                    .First()
                    .Subscribe(_1 =>
                    {
                        Log($"reach video end: {_duration}");
                        _onFinished();
                    })
                    .AddTo(_playDisposables)
                    .AddTo(tempPlayDisposables);
            };

            void _registerFinishedSeekingEvent()
            {
                OnFinishedSeekingAsObservable()
                    .Subscribe(_ =>
                    {
                        Log($"seek completed to {startTime}");
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
                    Log($"request seek to {startTime} from {_currentTime}");
                    __seek(startTime);
                }
                else
                {
                    Log($"request play directly at {startTime}");
                    _playVideo();
                }
            }

            if (_notSameSource)
            {
                _readyHandler = OnFirstFrameReadyAsObservable()
                    .First()
                    .Subscribe(_ =>
                    {
                        if (!cachedDefaultTexes.ContainsKey(videoPath))
                        {
                            var _texture = MediaPlayer.ExtractFrame(null, startTime).ToRenderTexture();

                            if (_texture != null)
                            {
                                cachedDefaultTexes[videoPath] = _texture;
                            }
                        }
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
                Log($"open media: {_mediaPathType} : {videoPath}");
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

        // public IObservable<AVProBase> OnPausedAsObservable()
        // {
        //     return _isPlaying.Where(_ => !_isPlaying.Value).Select(_ => this);
        // }

        public IObservable<AVProBase> OnMuteChangedAsObservable()
        {
            return _isMute.Select(_ => this);
        }

        public IObservable<AVProBase> OnVolumeChangedAsObservable()
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

        public void Rewind(bool pause = false, Action<AVProBase> onCompleted = null)
        {
            Log($"rewind to {this.StartTime}, pause: {pause}");
            // ClearPlayHandlers();
            if (pause)
                this.Pause();
            Seek(this.StartTime, onCompleted);
        }

        public void Rewind(Action<AVProBase> onCompleted)
        {
            Rewind(false, onCompleted);
        }

        // public void Play()
        // {
        //     if (!Ready2Play)
        //     {
        //         return;
        //     }
        //     // ClearPlayHandlers();
        //     MediaPlayer.Control.Play();
        // }

        // public void Pause()
        // {
        //     if (!Ready2Play)
        //     {
        //         return;
        //     }
        //     // ClearPlayHandlers();
        //     MediaPlayer.Control.Pause();
        // }

        private void __seek(double time)
        {
            Log($" seek to {time}");
            OnRequestSeek.Invoke(MediaPlayer, (float)time);
            if (CurrentTime == time)
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

        // public void Stop()
        // {
        //     Log($" {this.name} Stop");
        //     ClearPlayHandlers();
        //     MediaPlayer.Control?.Stop();
        // }


        public override void Seek(double time)
        {
            __seek(time);
        }

        public void Seek(double InTime, Action<AVProBase> onCompleted = null)
        {
            if (!Ready2Play)
            {
                return;
            }
            // ClearPlayHandlers();
            OnFinishedSeekingAsObservable()
                .First()
                .Subscribe(_ =>
                {
                    onCompleted?.Invoke(this);
                });
            __seek(InTime);
        }

        public IObservable<AVProPlayer> SeekAsObservable(double InTime)
        {
            return Observable.Create<AVProPlayer>(_observer =>
            {
                var disposable = new CompositeDisposable();

                OnFinishedSeekingAsObservable()
                    .First()
                    .Subscribe(_ =>
                    {
                        _observer.OnNext(this);
                        _observer.OnCompleted();
                    })
                    .AddTo(disposable);
                __seek(InTime);
                return disposable;
            });
        }

        public void SeekToFrame(int Frame, Action<AVProBase> onFinished = null)
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
    }
}
