using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace UNIHper
{
    using UniRx;

    [RequireComponent(typeof(VideoPlayer))]
    public class UVideoPlayer : MonoBehaviour
    {
        public VideoRenderMode renderMode = VideoRenderMode.RenderTexture;
        public VideoSource videoSource = VideoSource.Url;

        /// <summary>
        /// 视频剪辑
        /// </summary>
        public VideoClip videoClip;

        /// <summary>
        /// 视频连接地址 如果是相对路径 则相对于 Application.StreamingAssetsPath
        /// </summary>
        public string videoUrl = string.Empty;
        private bool looping = false;
        public bool Looping
        {
            set { looping = value; }
            get { return looping; }
        }

        public bool isPlaying
        {
            get
            {
                if (videoPlayer != null)
                {
                    return videoPlayer.isPlaying;
                }
                return false;
            }
        }

        public bool isPrepared
        {
            get
            {
                if (videoPlayer != null)
                {
                    return videoPlayer.isPrepared;
                }
                return false;
            }
        }

        public string Url
        {
            set
            {
                if (videoPlayer != null)
                {
                    videoPlayer.url = value;
                }
            }
            get
            {
                if (videoPlayer != null)
                {
                    return videoPlayer.url;
                }
                return string.Empty;
            }
        }

        public double Time
        {
            get { return videoPlayer.time; }
        }

        //private RectTransform rectTransform = null;
        private VideoPlayer videoPlayer = null;
        private RawImage videoImage = null;

        private RenderTexture renderTexture = null;
        public RenderTexture RenderTexture
        {
            get { return renderTexture; }
        }

        /// <summary>
        /// 构建渲染目标
        /// </summary>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="bAutoRender">是否自动渲染到 RawImage 组件</param>
        public UVideoPlayer BuildRender(int Width = -1, int Height = -1, bool bAutoRender = true)
        {
            RectTransform rectTransform = this.transform as RectTransform;
            if (rectTransform)
            { // 如果是UI组件 则渲染为UI组件的尺寸
                if (Width == -1)
                    Width = (int)rectTransform.rect.width;
                if (Height == -1)
                    Height = (int)rectTransform.rect.height;
            }
            else
            { // 渲染为屏幕大小的尺寸
                if (Width == -1)
                    Width = Screen.width;
                if (Height == -1)
                    Height = Screen.height;
            }
            RenderTexture _videoRT = new RenderTexture(
                Width,
                Height,
                0,
                RenderTextureFormat.ARGB32
            );

            if (bAutoRender)
            {
                var _renderer = this.GetComponent<RawImage>();
                if (!_renderer)
                {
                    _renderer = this.AddComponent<RawImage>();
                }
                _renderer.texture = _videoRT;
            }

            Render2Texture(_videoRT);
            return this;
        }

        public void Render2Texture(RenderTexture InTexture)
        {
            buildRefs();

            renderTexture = InTexture;

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
        }

        public void Render2Texture(Material InMaterial)
        {
            InMaterial.SetTexture("_MainTex", videoPlayer.targetTexture);
        }

        List<IDisposable> timerHandlers = new List<IDisposable>();

        public void Render2Material(Renderer InRenderer = null)
        {
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            if (InRenderer == null)
            {
                InRenderer = this.GetComponent<Renderer>();
                if (InRenderer == null)
                {
                    Debug.LogWarning("UVP: There is no renderer component. from Render2Material");
                    return;
                }
            }
            videoPlayer.targetMaterialRenderer = InRenderer;
        }

        public UVideoPlayer DisablePlayOnAwake()
        {
            videoPlayer.playOnAwake = false;
            return this;
        }

        public UVideoPlayer DisableLoop()
        {
            videoPlayer.isLooping = false;
            return this;
        }

        private void Awake()
        {
            buildRefs();
            videoPlayer.isLooping = false;
            //syncRenderMode();

            videoPlayer.errorReceived += (_vp, _error) =>
            {
                Debug.LogError(_error);
            };

            videoPlayer.started += (_) => {
                //Debug.LogFormat("UVP_{0} ============== Started: {1}", UnityEngine.Time.time, videoPlayer.time);
            };
        }

        void buildRefs()
        {
            if (videoPlayer == null)
                videoPlayer = this.GetComponent<VideoPlayer>();
        }

        private VideoPlayer.EventHandler vpReachEnd = null;

        private void clearVPReachEnd()
        {
            if (vpReachEnd != null)
            {
                videoPlayer.loopPointReached -= vpReachEnd;
                vpReachEnd = null;
            }
        }

        private IDisposable vpLoopTimer = null;

        private void clearVPLoopTimer()
        {
            if (vpLoopTimer != null)
            {
                vpLoopTimer.Dispose();
                vpLoopTimer = null;
            }
        }

        /**
         *  loop -1 根据Looping属性决定 0 不循环   1循环
         */

        /// <summary>
        /// 从URL播放视频
        /// </summary>
        /// <param name="InUrl">视频地址</param>
        /// <param name="OnReachEndHandler">视频播放到结尾时回调</param>
        /// <param name="loop">循环模式 -1 保持当前 0 不循环 1 循环 </param>
        /// <param name="StartTime">开始时间</param>
        /// <param name="InEndTime">结束时间</param>
        public void Play(
            string InUrl,
            VideoPlayer.EventHandler OnReachEndHandler = null,
            int loop = -1,
            float StartTime = 0,
            float InEndTime = 0
        )
        {
            videoSource = VideoSource.Url;
            videoPlayer.url = InUrl;
            this.Play(OnReachEndHandler, loop, StartTime, InEndTime);
        }

        public void Play(
            VideoClip InClip,
            VideoPlayer.EventHandler OnReachEndHandler = null,
            int loop = -1,
            float StartTime = 0,
            float InEndTime = 0
        )
        {
            videoSource = VideoSource.VideoClip;
            videoPlayer.clip = InClip;
            Play(OnReachEndHandler, loop, StartTime, InEndTime);
        }

        public void Play(
            VideoPlayer.EventHandler OnReachEndHandler = null,
            int loop = -1,
            float StartTime = 0,
            float InEndTime = 0
        )
        {
            if (videoPlayer.isPlaying)
            {
                Debug.Log(1);
                videoPlayer.Pause();
                this.realPlay(OnReachEndHandler, loop, StartTime, InEndTime);
            }
            else if (!videoPlayer.isPrepared)
            {
                Debug.Log(2);
                this.Prepare(_ =>
                {
                    this.realPlay(OnReachEndHandler, loop, StartTime, InEndTime);
                });
            }
            else
            {
                Debug.Log(3);
                this.realPlay(OnReachEndHandler, loop, StartTime, InEndTime);
            }
        }

        private bool isFullVPLength(double InLength)
        {
            return videoPlayer.length == InLength;
        }

        /// <summary>
        /// 视频播放控制核心逻辑
        /// </summary>
        /// <param name="OnReachEndHandler"></param>
        /// <param name="loop"></param>
        /// <param name="StartTime"></param>
        /// <param name="InEndTime"></param>
        private void realPlay(
            VideoPlayer.EventHandler OnReachEndHandler = null,
            int loop = -1,
            float StartTime = 0,
            float InEndTime = 0
        )
        {
            if (!renderTexture)
            {
                BuildRender();
            }
            //Debug.LogFormat("UVP_{0} ========== request real play", UnityEngine.Time.time);
            double _originTime = UnityEngine.Time.time;
            if (!videoPlayer.isPrepared)
            {
                Debug.LogWarning("UVP: video source is not prepared.");
                OnReachEndHandler(videoPlayer);
                return;
            }

            bool _looping = loop == -1 ? this.Looping : loop == 1;
            Looping = _looping;

            double _startTime = Mathf.Max(StartTime, 0);
            double _endTime = 0.0f;
            bool _bSeekCompleted = false;

            if (InEndTime > 0)
            {
                _endTime = Mathf.Min(InEndTime, (float)videoPlayer.length);
            }
            else
            {
                _endTime = videoPlayer.length;
            }
            //Debug.LogFormat("UVP:Play [{0} - {1}]",_startTime, _endTime);
            clearVPReachEnd();
            clearVPLoopTimer();

            vpReachEnd = _ =>
            {
                _bSeekCompleted = false;
                if (OnReachEndHandler != null)
                    OnReachEndHandler(videoPlayer);

                if (looping)
                {
                    this.SeekTo(
                        _startTime,
                        _7 => { },
                        _8 =>
                        {
                            _bSeekCompleted = true;
                        },
                        true
                    );
                }
                else
                {
                    if (videoPlayer.isPlaying)
                    {
                        videoPlayer.Pause();
                    }
                    clearVPReachEnd();
                    clearVPLoopTimer();
                }
            };

            vpLoopTimer = Observable
                .EveryUpdate()
                .Where(_ => _bSeekCompleted)
                .Subscribe(_1 =>
                {
                    if (videoPlayer.time >= _endTime)
                    { // 经测试  触发onReachEndpoint时  videoPlayer.time 是小于 videoPlayer.length的
                        vpReachEnd(videoPlayer);
                    }
                });

            this.SeekTo(
                _startTime,
                _ => { },
                _2 =>
                {
                    _bSeekCompleted = true;
                    videoPlayer.loopPointReached += vpReachEnd;
                },
                true
            );
        }

        private VideoPlayer.EventHandler vpPreapared = null;

        public void Prepare(VideoClip InClip, VideoPlayer.EventHandler OnPrepared = null)
        {
            videoPlayer.clip = InClip;
            this.Prepare(_ =>
            {
                this.SeekTo(0f, OnPrepared);
            });
        }

        public void Prepare(
            string InUrl,
            VideoPlayer.EventHandler OnPrepared = null,
            VideoPlayer.EventHandler OnTimeReady = null
        )
        {
            videoPlayer.url = InUrl;
            this.Prepare(_ =>
            {
                this.SeekTo(0f, OnPrepared, OnTimeReady);
            });
        }

        public void Prepare(VideoPlayer.EventHandler OnPrepared = null)
        {
            if (videoPlayer == null)
            {
                Debug.LogWarning("UVP: null reference of videoPlayer");
            }

            if (vpPreapared != null)
            {
                videoPlayer.prepareCompleted -= vpPreapared;
                vpPreapared = null;
            }

            vpPreapared = _ =>
            {
                if (OnPrepared != null)
                    OnPrepared(videoPlayer);
                videoPlayer.prepareCompleted -= vpPreapared;
            };
            videoPlayer.prepareCompleted += vpPreapared;
            videoPlayer.Stop();
            videoPlayer.Prepare();
        }

        private VideoPlayer.EventHandler vpSeekCompleted = null;

        private void clearSeekCompltedHandler()
        {
            if (vpSeekCompleted != null)
            {
                videoPlayer.seekCompleted -= vpSeekCompleted;
                vpSeekCompleted = null;
            }
        }

        private IDisposable vpSeekTimer = null;

        private void clearSeekTimer()
        {
            if (vpSeekTimer != null)
            {
                vpSeekTimer.Dispose();
                vpSeekTimer = null;
            }
        }

        private bool timeGreaterThan(double InTime)
        {
            return videoPlayer.time >= InTime;
        }

        /// <summary>
        /// seek
        /// </summary>
        /// <param name="InTime"></param>
        /// <param name="InSeekedHandler">原生组件seek完成事件</param>
        /// <param name="InTimeReadyHandler">真正的时间完成事件(原生事件触发时，获取时间并不是设置的时间)</param>
        /// <param name="AutoPlay"></param>
        public void SeekTo(
            double InTime,
            VideoPlayer.EventHandler InSeekedHandler = null,
            VideoPlayer.EventHandler InTimeReadyHandler = null,
            bool AutoPlay = false
        )
        {
            clearSeekCompltedHandler();
            clearSeekTimer();

            double _originDelta = videoPlayer.time - InTime;
            Func<long, bool> _condition = _2 =>
            {
                var _curDelta = videoPlayer.time - InTime;
                bool _reachEnd = _curDelta >= 0;
                return _reachEnd;
            };
            if (videoPlayer.time > InTime)
            {
                _condition = _2 =>
                {
                    var _curDelta = videoPlayer.time - InTime;
                    bool _reachEnd = _curDelta >= 0 && _curDelta < _originDelta;
                    return _reachEnd;
                };
            }

            double _originGameTime = UnityEngine.Time.time;

            //Debug.LogFormat("UVP_{0}:========= start seek ",UnityEngine.Time.time, UnityEngine.Time.time - _originGameTime);
            vpSeekCompleted = _ =>
            {
                //Debug.LogFormat("UVP_{0}:=========Seek complted delta 1: {1}",UnityEngine.Time.time, UnityEngine.Time.time - _originGameTime);
                clearSeekCompltedHandler();
                videoPlayer.SetDirectAudioMute(0, false);
                if (InSeekedHandler != null)
                {
                    InSeekedHandler(videoPlayer);
                }

                vpSeekTimer = Observable
                    .EveryUpdate()
                    .Where(_condition)
                    .First()
                    .Subscribe(_11 =>
                    {
                        //Debug.LogFormat("UVP_{0}:=========Seek complted delta 2: {1}",UnityEngine.Time.time, UnityEngine.Time.time - _originGameTime);
                        clearSeekTimer();
                        if (videoPlayer.isPlaying && !AutoPlay)
                        {
                            videoPlayer.Pause();
                        }
                        if (InTimeReadyHandler != null)
                        {
                            InTimeReadyHandler(videoPlayer);
                        }
                    });
            };

            videoPlayer.SetDirectAudioMute(0, true);
            videoPlayer.seekCompleted += vpSeekCompleted;

            if (!videoPlayer.isPlaying)
            {
                //Debug.Log("UVP: Play video by Play 3");
                videoPlayer.Play();
            }
            videoPlayer.time = InTime;
        }

        public void Play()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Play();
            }
        }

        public void Stop(bool bFullyStop = false)
        {
            clearSeekCompltedHandler();
            clearSeekTimer();
            clearVPLoopTimer();
            clearVPReachEnd();
            if (videoPlayer == null)
                return;
            if (bFullyStop)
            {
                videoPlayer.Stop();
                videoPlayer.SetDirectAudioMute(0, true);
                videoPlayer.time = 0f;
            }
            else
            {
                SeekTo(0, _ => { });
            }
        }

        public void Pause()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Pause();
            }
        }

        public void SetSpeed(float InSpeed)
        {
            videoPlayer.playbackSpeed = InSpeed;
        }

        // Start is called before the first frame update
        void Start()
        {
            buildRefs();
            if (videoSource == VideoSource.VideoClip && videoPlayer.clip)
            {
                this.Play(videoClip, _ => { }, Looping ? 1 : 0);
            }
            else if (videoSource == VideoSource.Url && videoUrl != string.Empty)
            {
                string _url = videoUrl;
                if (!Path.IsPathRooted(videoUrl))
                {
                    _url = Path.Combine(Application.streamingAssetsPath, videoUrl);
                }
                Play(_url);
            }
        }

        // Update is called once per frame
        void Update() { }

        private void Reset()
        {
            if (videoImage is null)
                return;
            RectTransform _transform = this.transform as RectTransform;
            _transform.anchorMin = Vector2.zero;
            _transform.anchorMax = Vector2.one;
            _transform.offsetMin = Vector2.zero;
            _transform.offsetMax = Vector2.zero;
        }

        private void OnValidate()
        {
            //buildRefs();
            //syncRenderMode();
        }

        private void syncRenderMode()
        {
            if (renderMode == VideoRenderMode.MaterialOverride)
            {
                Render2Material();
            }
            else if (renderMode == VideoRenderMode.RenderTexture)
            {
                BuildRender();
            }
        }
    }
}
