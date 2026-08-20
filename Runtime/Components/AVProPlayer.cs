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
        // 播放完成的专用通知通道，与原生 AVPro 事件解耦
        // 只在 Play() 内部的 _onFinished() 中触发，避免 EveryUpdate 检查污染全局事件总线
        private readonly Subject<MediaPlayer> _playFinishedSubject = new();

        // 外部 Seek 回调的独占订阅，避免多次 Seek 导致回调堆积
        private readonly SerialDisposable _seekDisposable = new();

        void Reset()
        {
            MediaPlayer.AutoOpen = false;
            MediaPlayer.AutoStart = false;
        }

        void OnDestroy()
        {
            ClearPlayHandlers();
            _seekDisposable.Dispose();
            _readyHandler?.Dispose();
            _readyHandler = null;
            ClearCachedTextures();
            _playFinishedSubject.OnCompleted();
            _playFinishedSubject.Dispose();
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
            ClearPlayHandlers();
            return Observable.Create<AVProPlayer>(observer =>
            {
                path = PathUtils.NormalizePath(path);
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

                // 媒体加载失败时通知订阅者
                OnErrorAsObservable()
                    .First()
                    .Subscribe(_ =>
                    {
                        observer.OnError(new Exception($"[AVProPlayer] failed to open media: {path}"));
                    })
                    .AddTo(_disposable);

                var _mediaPathType = PathUtils.IsAbsolutePathOrUrl(path)
                    ? MediaPathType.AbsolutePathOrURL
                    : MediaPathType.RelativeToStreamingAssetsFolder;
                MediaPlayer.OpenMedia(_mediaPathType, path, false);
                return _disposable;
            });
        }

        public bool Switch(string path)
        {
            path = PathUtils.NormalizePath(path);
            var _mediaPathType = PathUtils.IsAbsolutePathOrUrl(path)
                ? MediaPathType.AbsolutePathOrURL
                : MediaPathType.RelativeToStreamingAssetsFolder;
            return MediaPlayer.OpenMedia(_mediaPathType, path, false);
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
                var _extracted = MediaPlayer.ExtractFrame(null, startTime);
                if (_extracted == null)
                {
                    Debug.LogWarning($"[AVProPlayer] ExtractFrame returned null for {videoPath}, skip caching");
                    return;
                }
                var _texture = _extracted.ToRenderTexture();
                Log($"cache first frame texture: {videoPath} : {_texture}, rt: {_texture as RenderTexture}");
                if (_texture != null)
                {
                    cachedDefaultTexes[videoPath] = _texture;
                    Log($"cachedDefaultTexes count: {cachedDefaultTexes.Count}");
                }
            }
        }

        public void ClearCachedTextures()
        {
            foreach (var tex in cachedDefaultTexes.Values)
            {
                if (tex is RenderTexture rt)
                    rt.Release();
                if (tex != null)
                    Destroy(tex);
            }
            cachedDefaultTexes.Clear();
        }

        public bool AutoSetDefaultTexture { get; set; } = true;
        private bool Loop { get; set; } = true;
        private bool AutoPlay { get; set; } = true;
        public bool ForceReopenOnNextPlay { get; set; } = false;

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
            videoPath = PathUtils.NormalizePath(videoPath);
            ClearPlayHandlers();
            if (_readyHandler != null)
            {
                _readyHandler.Dispose();
                _readyHandler = null;
            }
            SetLoop(bLoop);
            SetAutoPlay(true);

            startTime = Math.Round(startTime, 3);
            endTime = Math.Round(endTime, 3);

            this.StartTime = startTime;
            this.EndTime = endTime;

            var forceReopenOnNextPlay = ForceReopenOnNextPlay;
            ForceReopenOnNextPlay = false;

            bool _notSameSource =
                forceReopenOnNextPlay
                || MediaPlayer.MediaPath.Path != videoPath
                || MediaPlayer.Control == null
                || !MediaPlayer.Control.HasMetaData()
                || !MediaPlayer.Control.CanPlay();

            var displayUI = this.Get<DisplayUGUI>();

            if (AutoSetDefaultTexture && displayUI != null)
            {
                displayUI.DefaultTexture = GetCachedDefaultTexture(videoPath);
                // displayUI.DefaultTexture = MediaPlayer.ExtractFrame(null, CurrentTime);
            }

            Log($" requested play video: {videoPath}, startTime: {startTime}, endTime: {endTime}, notSameSource: {_notSameSource}");
            CompositeDisposable tempPlayDisposables = null;
            void _playVideo()
            {
                Log($" start playing video: {videoPath} from {startTime}");
                tempPlayDisposables?.Dispose();
                tempPlayDisposables = new CompositeDisposable();
                _builtSeekOperation = false;
                Play(false);
                // 强制启动播放：HLS 流在 _notSameSource=false 时 Play(false) 可能不会自动开始
                if (MediaPlayer.Control != null && !MediaPlayer.Control.IsPlaying())
                {
                    MediaPlayer.Control.Play();
                }
                bool _bFinished = false;
                var _duration = MediaPlayer.Info.GetDuration();
                var _durationFrames = MediaPlayer.Info.GetDurationFrames();
                // 动态计算帧容差，防止视频未达到最后一帧时就触发结束
                double frameTolerance = (_duration > 0 && _durationFrames > 0) ? _duration / _durationFrames : 0.033;
                endTime = endTime == 0 ? _duration - frameTolerance : endTime;
                // 防止 endTime 为负值（duration 尚未就绪时会算出 -0.033）
                if (endTime < 0)
                    endTime = 0;

                // 播放结束回调
                void _onFinished()
                {
                    if (_bFinished)
                        return;

                    _bFinished = true;
                    onFinished?.Invoke(this);
                    Pause(false);
                    SetAutoPlay(Loop);
                    if (Loop)
                        _seekThenPlay();
                    else if (seek2StartAfterFinished)
                        __seek(startTime);
                    _playFinishedSubject.OnNext(MediaPlayer);
                }

                // 播放时间到达末尾时触发完成回调
                // 使用实时 duration 而非固定 endTime，兼容 HLS 动态 duration 场景
                Observable
                    .EveryUpdate()
                    .Where(_1 =>
                    {
                        if (_bFinished)
                            return false;
                        // 网络不好时 HLS 会暂停缓冲，此时 IsPlaying=false 但未真正播完
                        // 通过 IsBuffering 判断，缓冲中不触发完成
                        var isBuffering = MediaPlayer.Control?.IsBuffering() ?? false;
                        if (isBuffering)
                            return false;
                        var ct = MediaPlayer.Control?.GetCurrentTime() ?? 0;
                        var dur = MediaPlayer.Info?.GetDuration() ?? 0;
                        if (dur <= 0)
                            return false; // duration 未就绪，等待
                        // 使用实时 duration 判断是否到达末尾
                        var threshold = dur - frameTolerance;
                        return ct >= threshold;
                    })
                    .First()
                    .Subscribe(_1 =>
                    {
                        _onFinished();
                    })
                    .AddTo(_playDisposables)
                    .AddTo(tempPlayDisposables);

                // 原生 AVPro FinishedPlaying 事件作为备用触发源
                base.OnFinishedPlayingAsObservable()
                    .First()
                    .Subscribe(_1 => _onFinished())
                    .AddTo(_playDisposables)
                    .AddTo(tempPlayDisposables);

                // 第三路检测：IsPlaying 从 true 变 false 且时间接近末尾
                Observable
                    .EveryUpdate()
                    .Select(_1 => MediaPlayer.Control?.IsPlaying() ?? false)
                    .DistinctUntilChanged()
                    .Where(isPlaying =>
                    {
                        if (isPlaying || _bFinished)
                            return false;
                        if (MediaPlayer.Control?.IsBuffering() ?? false)
                            return false;
                        var ct = MediaPlayer.Control?.GetCurrentTime() ?? 0;
                        var dur = MediaPlayer.Info?.GetDuration() ?? 0;
                        return dur > 0 && ct >= dur - 0.5;
                    })
                    .Subscribe(_1 => _onFinished())
                    .AddTo(_playDisposables)
                    .AddTo(tempPlayDisposables);
            }

            void _seekThenPlay()
            {
                _builtSeekOperation = true;
                OnFinishedSeekingAsObservable()
                    .First()
                    .Subscribe(_ =>
                    {
                        Log($"seek finished to {startTime}");
                        if (AutoPlay)
                            _playVideo();
                    })
                    .AddTo(_playDisposables);

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
                var _openDisposable = new CompositeDisposable();

                Observable
                    .Merge(OnFirstFrameReadyAsObservable(), OnReadyToPlayAsObservable(), OnMetaDataReadyAsObservable())
                    .First()
                    .Subscribe(_ =>
                    {
                        TryCacheDefaultTexture(videoPath, startTime);
                        _openDisposable.Dispose();
                        _readyHandler = null;
                        _seekThenPlay();
                    })
                    .AddTo(_openDisposable);

                // 媒体加载失败时释放资源并记录错误
                OnErrorAsObservable()
                    .First()
                    .Subscribe(_ =>
                    {
                        Debug.LogError($"[AVProPlayer]: {name} failed to open media: {videoPath}");
                        _openDisposable.Dispose();
                        _readyHandler = null;
                    })
                    .AddTo(_openDisposable);

                _readyHandler = _openDisposable;

                var _mediaPathType = PathUtils.IsAbsolutePathOrUrl(videoPath)
                    ? MediaPathType.AbsolutePathOrURL
                    : MediaPathType.RelativeToStreamingAssetsFolder;
                Log($"open media: {_mediaPathType} : {videoPath}");
                MediaPlayer.OpenMedia(_mediaPathType, videoPath, false);
            }
            else
            {
                _seekThenPlay();
            }
        }

        /// <summary>
        /// 重写播放完成事件：返回专用 Subject 而非全局 UnityEvent
        /// 只在 Play() 流程正常完成时触发，不受 EveryUpdate 检查污染
        /// </summary>
        public override IObservable<MediaPlayer> OnFinishedPlayingAsObservable()
        {
            return _playFinishedSubject.AsObservable();
        }

        /// <summary>
        /// 静音状态变化（基于 EveryUpdate 轮询检测变化，因 AVPro 无原生静音事件）
        /// </summary>
        public IObservable<AVProBase> OnMuteChangedAsObservable()
        {
            return Observable
                .EveryUpdate()
                .Where(_ => MediaPlayer.Control != null)
                .Select(_ => MediaPlayer.Control.IsMuted())
                .DistinctUntilChanged()
                .Select(_ => (AVProBase)this);
        }

        /// <summary>
        /// 音量变化（基于 EveryUpdate 轮询检测变化，因 AVPro 无原生音量事件）
        /// </summary>
        public IObservable<AVProBase> OnVolumeChangedAsObservable()
        {
            return Observable
                .EveryUpdate()
                .Where(_ => MediaPlayer.Control != null)
                .Select(_ => MediaPlayer.Control.GetVolume())
                .DistinctUntilChanged()
                .Select(_ => (AVProBase)this);
        }

        public override void Rewind(bool pause)
        {
            Rewind(pause, null);
        }

        public void Rewind(bool pause, Action<AVProBase> onCompleted)
        {
            Log($"rewind to {this.StartTime}, pause: {pause}");
            if (pause)
                this.Pause();
            Seek(this.StartTime, onCompleted);
        }

        public void Rewind(Action<AVProBase> onCompleted)
        {
            Rewind(false, onCompleted);
        }

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
            _seekDisposable.Disposable = OnFinishedSeekingAsObservable()
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
            _seekDisposable.Disposable = OnFinishedSeekingAsObservable()
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
