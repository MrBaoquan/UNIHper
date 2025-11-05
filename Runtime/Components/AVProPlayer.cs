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

        public AVProBase SetTransparency(AlphaPacking alphaPacking = AlphaPacking.LeftRight)
        {
            if (alphaPacking == AlphaPacking.None)
            {
                return SetOpaque();
            }

            MediaHints _hints = MediaPlayer.FallbackMediaHints;
            _hints.transparency = TransparencyMode.Transparent;
            _hints.alphaPacking = alphaPacking;
            MediaPlayer.FallbackMediaHints = _hints;
            return this;
        }

        public AVProBase SetOpaque()
        {
            MediaHints _hints = MediaPlayer.FallbackMediaHints;
            _hints.transparency = TransparencyMode.Opaque;
            MediaPlayer.FallbackMediaHints = _hints;
            return this;
        }

        public double StartTime { get; protected set; }

        public double EndTime { get; protected set; }

        public IObservable<AVProPlayer> SwitchAsObservable(string path, double startTime = 0)
        {
            return Observable.Create<AVProPlayer>(observer =>
            {
                var _disposable = new CompositeDisposable();

                OnFirstFrameReadyAsObservable()
                    .First()
                    .SelectMany(_ => SeekAsObservable(startTime))
                    .Subscribe(_ =>
                    {
                        TryCacheDefaultTexture(path, startTime);
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

        public void Switch() { }

        IDisposable _readyHandler = null;

        public Dictionary<string, Texture> cachedDefaultTexes = new();

        // 共享的透明贴图（静态，避免重复创建）
        private static Texture2D _sharedTransparentTexture;
        private static Texture2D SharedTransparentTexture
        {
            get
            {
                if (_sharedTransparentTexture == null)
                {
                    // 使用 2x2 尺寸避免 AABB 计算错误
                    _sharedTransparentTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                    // 创建透明色数组并一次性填充
                    var pixels = new Color32[4];
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(0, 0, 0, 0);

                    _sharedTransparentTexture.SetPixels32(pixels);
                    _sharedTransparentTexture.Apply();
                    _sharedTransparentTexture.name = "SharedTransparentTexture";
                }
                return _sharedTransparentTexture;
            }
        }

        public Texture GetCachedDefaultTexture(string videoPath)
        {
            if (cachedDefaultTexes.TryGetValue(videoPath, out var cachedTex))
            {
                return cachedTex;
            }

            // 返回共享的透明贴图
            return SharedTransparentTexture;
        }

        public void TryCacheDefaultTexture(string videoPath, double startTime = 0)
        {
            if (!cachedDefaultTexes.ContainsKey(videoPath))
            {
                var _texture = MediaPlayer.ExtractFrame(null, startTime).ToRenderTexture();
                Log($"cache first frame texture: {videoPath} : {_texture}, rt: {_texture as RenderTexture}");
                if (_texture != null)
                {
                    cachedDefaultTexes[videoPath] = _texture;
                    Log($"cachedDefaultTexes count: {cachedDefaultTexes.Count}");
                }
            }
        }

        public bool AutoSetDefaultTexture { get; set; } = true;
        private bool Loop { get; set; } = true;
        private bool AutoPlay { get; set; } = true;

        public void SetLoop(bool loop)
        {
            Loop = loop;
        }

        public void SetAutoPlay(bool autoPlay)
        {
            AutoPlay = autoPlay;
        }

        public void Play(
            string videoPath,
            bool bLoop = false,
            double startTime = 0,
            double endTime = 0,
            bool seek2StartAfterFinished = true
        )
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
            bool bLoop = false,
            double startTime = 0,
            double endTime = 0,
            bool seek2StartAfterFinished = false
        )
        {
            ClearPlayHandlers();
            if (_readyHandler != null)
            {
                _readyHandler.Dispose();
                _readyHandler = null;
            }
            var _videoName = Path.GetFileName(videoPath);
            SetLoop(bLoop);
            SetAutoPlay(true);

            startTime = Math.Round(startTime, 3);
            endTime = Math.Round(endTime, 3);

            this.StartTime = startTime;
            this.EndTime = endTime;

            bool _notSameSource =
                MediaPlayer.MediaPath.Path != videoPath || MediaPlayer.Control == null || !MediaPlayer.Control.HasMetaData();

            var displayUI = this.Get<DisplayUGUI>();

            if (AutoSetDefaultTexture && displayUI != null)
            {
                displayUI.DefaultTexture = GetCachedDefaultTexture(videoPath);
                // displayUI.DefaultTexture = MediaPlayer.ExtractFrame(null, CurrentTime);
            }

            Log($" requested play video: {videoPath}, startTime: {startTime}, endTime: {endTime}, notSameSource: {_notSameSource}");
            CompositeDisposable tempPlayDisposables = null;
            Action _playVideo = () =>
            {
                Log($" start playing video: {videoPath} from {startTime}");
                tempPlayDisposables?.Dispose();
                tempPlayDisposables = new CompositeDisposable();
                _builtSeekOperation = false;
                Play(false);
                bool _bFinished = false;
                var _duration = MediaPlayer.Info.GetDuration();
                endTime = endTime == 0 ? Mathf.FloorToInt((float)_duration * 100) / 100f - 0.033 : endTime;

                // 播放结束回调
                Action _onFinished = () =>
                {
                    if (_bFinished)
                        return;

                    _bFinished = true;
                    onFinished?.Invoke(this);
                    Pause(false);
                    SetAutoPlay(Loop);
                    // 播放结束，是否跳到开始时间
                    if (seek2StartAfterFinished || Loop)
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
                    .AddTo(_playDisposables)
                    .AddTo(tempPlayDisposables);

                // 视频到达结尾
                OnFinishedPlayingAsObservable().First().Subscribe(_1 => _onFinished()).AddTo(_playDisposables).AddTo(tempPlayDisposables);
            };

            void _registerFinishedSeekingEvent()
            {
                OnFinishedSeekingAsObservable()
                    .Subscribe(_ =>
                    {
                        Log($"seek finished to {startTime}");
                        if (AutoPlay)
                            _playVideo();
                    })
                    .AddTo(_playDisposables);
            }

            void _startSeek()
            {
                _builtSeekOperation = true;
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

            if (_notSameSource)
            {
                Log($"open new media: {videoPath}, cached tex count: {cachedDefaultTexes.Count}");
                _readyHandler = OnFirstFrameReadyAsObservable()
                    .First()
                    .Subscribe(_ =>
                    {
                        TryCacheDefaultTexture(videoPath, startTime);
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


        public void SeekRelative(double deltaTime)
        {
            if (!Ready2Play)
            {
                return;
            }
            var _targetTime = Math.Max(0, Math.Min(Duration, CurrentTime + deltaTime));
            Seek(_targetTime);
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

        public IObservable<AVProPlayer> SeekAsObservable(double InTime, float timeoutSeconds = 3f)
        {
            Log($" seek requested to {InTime} ");
            return Observable.Create<AVProPlayer>(_observer =>
            {
                var disposable = new CompositeDisposable();

                _builtSeekOperation = true;
                OnFinishedSeekingAsObservable()
                    .Timeout(TimeSpan.FromSeconds(timeoutSeconds))
                    .Catch<MediaPlayer, Exception>(ex => Observable.Return(MediaPlayer))
                    .First()
                    .Subscribe(_ =>
                    {
                        _builtSeekOperation = false;
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
