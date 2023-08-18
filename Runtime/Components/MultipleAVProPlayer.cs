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

namespace UNIHper
{
    public class MultipleAVProPlayer : MonoBehaviour
    {
        // 渲染介质
        enum RenderTarget
        {
            RawImage,
            DisplayUGUI
        };

        [
            SerializeField,
            ShowIf("@UnityEditor.EditorApplication.isPlaying == false"),
            OnValueChanged("onRenderTargetChanged")
        ]
        private RenderTarget renderTarget = RenderTarget.RawImage;

        private void onRenderTargetChanged()
        {
            if (renderTarget == RenderTarget.DisplayUGUI)
            {
                if (this.Get<Graphic>() != null)
                {
                    GameObject.DestroyImmediate(this.Get<Graphic>());
                }
                if (this.Get<DisplayUGUI>() == null)
                {
                    var _displayUGUI = this.gameObject.AddComponent<DisplayUGUI>();
                    _displayUGUI.ScaleMode = ScaleMode.StretchToFill;
                    _displayUGUI.NoDefaultDisplay = false;
#if UNITY_EDITOR
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(_displayUGUI);
#endif
                }
            }
            else if (renderTarget == RenderTarget.RawImage)
            {
                if (this.Get<Graphic>() != null)
                {
                    GameObject.DestroyImmediate(this.Get<Graphic>());
                }
                if (this.Get<RawImage>() == null)
                {
                    var _rawImage = this.gameObject.AddComponent<RawImage>();
                    _rawImage.raycastTarget = false;
#if UNITY_EDITOR
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(_rawImage);
#endif
                }
            }
        }

        [SerializeField, HideInInspector]
        private Shader fadeShader = null;

        [Title("Player Settings")]
        [SerializeField, ValueDropdown("getFadeTypes")]
        FadeType fadeType = FadeType.Alpha;

        private ValueDropdownList<FadeType> getFadeTypes()
        {
            var _fadeTypes = new ValueDropdownList<FadeType>() { FadeType.None, FadeType.Alpha };
            if (renderTarget == RenderTarget.RawImage)
            {
                _fadeTypes.Add(FadeType.Color);
            }
            return _fadeTypes;
        }

        [SerializeField, ShowIf("fadeType", FadeType.Alpha)]
        public float FadeIn = 0.75f;

        [SerializeField, ShowIf("fadeType", FadeType.Alpha)]
        public float FadeOut = 0.45f;

        [SerializeField, ShowIf("fadeType", FadeType.Color)]
        private float FadeDuration = 1.0f;

        private readonly UnityEvent<AVProPlayer> onPlayerChanged = new();

        public IObservable<AVProPlayer> OnPlayerChangedAsObservable()
        {
            return onPlayerChanged.AsObservable();
        }

        private void Reset()
        {
#if UNITY_EDITOR
            fadeShader = Shader.Find("UNIHper/Unlit/FadeAB");
#endif
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
                Debug.LogWarning("No video found.");
                return Observable.Return<IList<AVProPlayer>>(new List<AVProPlayer>());
            }

            videoIndex.SetMax(VideoPaths.Count() - 1);
            this.videoPaths = VideoPaths.ToList();

            var _defaultPlayer = new GameObject("mediaPlayer_default");
            _defaultPlayer.AddComponent<CanvasRenderer>();

            var _avProPlayer = _defaultPlayer.AddComponent<AVProPlayer>();

            _avProPlayer.MediaPlayer.AutoStart = false;
            _avProPlayer.MediaPlayer.AutoOpen = false;
            _avProPlayer.MediaPlayer.Loop = Loop;

            var _displayUGUI = _defaultPlayer.AddComponent<DisplayUGUI>();
            _displayUGUI.CurrentMediaPlayer = _avProPlayer.MediaPlayer;
            _displayUGUI.ScaleMode = ScaleMode.StretchToFill;
            _displayUGUI.color = Color.clear;
            _displayUGUI.raycastTarget = false;
            _displayUGUI.NoDefaultDisplay = false;

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

            //DisplayUGUI.color = Color.white;

            return Observable
                .Zip(
                    VideoPaths.Select(_path =>
                    {
                        var _mediaPlayer = _mediaPlayerPool.Spawn(
                            _defaultPlayer.transform,
                            transform
                        );
                        _mediaPlayer.SetAsLastSibling();
                        _mediaPlayer.name = _path;

                        var _rectTransform = _mediaPlayer.AddComponent<RectTransform>();
                        _rectTransform.anchorMin = Vector2.zero;
                        _rectTransform.anchorMax = Vector2.one;
                        _rectTransform.offsetMin = Vector2.zero;
                        _rectTransform.offsetMax = Vector2.zero;

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
                    onReady();
                });
        }

        private float swapRenderChain()
        {
            activeSwapChainBufferIndex = nextSwapChainBufferIndex;
            return activeSwapChainBufferIndex;
        }

        private bool isSwapChainBufferReady = false;
        private int activeSwapChainBufferIndex = 0;
        private int nextSwapChainBufferIndex => activeSwapChainBufferIndex == 0 ? 1 : 0;

        private void setSwapChainBuffer(Texture texture)
        {
            if (activeSwapChainBufferIndex == 0)
            {
                RawImage_Render.material.SetTexture("_BTexture", texture);
            }
            else
            {
                RawImage_Render.material.SetTexture("_ATexture", texture);
            }
            isSwapChainBufferReady = true;
        }

        /// <summary>
        /// Called when all videos are prepared
        /// </summary>
        private void onReady()
        {
            setupRenderMaterial();
            videoIndex
                .OnIndexChangedAsObservable()
                .Subscribe(async _index =>
                {
                    if (renderTarget == RenderTarget.RawImage)
                    {
                        isSwapChainBufferReady = false;
                        var _texture = await getMediaTexture(_index);
                        RawImage_Render.texture = _texture;
                        if (fadeType == FadeType.Color)
                        {
                            setSwapChainBuffer(_texture);
                        }
                    }
                    else if (renderTarget == RenderTarget.DisplayUGUI)
                    {
                        DisplayUGUI_Render.CurrentMediaPlayer =
                            currentPlayer.GetComponent<MediaPlayer>();
                    }
                    onPlayerChanged.Invoke(currentPlayer);
                })
                .AddTo(this);

            // 等待当前视频帧准备完成
            // Observable
            //     .EveryUpdate()
            //     .Where(_ => currentPlayer.Get<DisplayUGUI>().HasValidTexture())
            //     .First()
            //     .Subscribe(_ =>
            //     {

            //     })
            //     .AddTo(this);
            fadePlay(videoIndex.Current, () => { });
        }

        private void setupRenderMaterial()
        {
            if (renderTarget == RenderTarget.RawImage)
            {
                var _displayMat = new Material(fadeShader);
                _displayMat.SetFloat("_FlipY", 1);
                _displayMat.SetTextureScale("_MainTex", new Vector2(1, -1));
                _displayMat.SetTextureOffset("_MainTex", Vector2.up);
                if (fadeType == FadeType.None)
                {
                    _displayMat.SetInteger("_Enable", 0);
                }
                else if (fadeType == FadeType.Color)
                {
                    _displayMat.SetInteger("_Enable", 1);
                    _displayMat.SetFloat("_Weight", 1);
                }
                else if (fadeType == FadeType.Alpha)
                {
                    _displayMat.SetInteger("_Enable", 0);
                }

                RawImage_Render.material = _displayMat;
                activeSwapChainBufferIndex = 1;
            }
        }

        public void Pause()
        {
            currentPlayer?.Pause();
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

        public void Play()
        {
            currentPlayer?.Play();
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

            fadePlay(
                _idx,
                () =>
                {
                    currentPlayer.Play(
                        videoPaths[_idx],
                        OnCompleted,
                        Loop,
                        StartTime,
                        EndTime,
                        seek2StartAfterFinished
                    );
                }
            );
        }

        public void TogglePlay()
        {
            if (currentPlayer == null)
            {
                Debug.LogWarning("currentPlayer is null");
                return;
            }

            if (currentPlayer.IsPaused)
            {
                currentPlayer.Play();
            }
            else
            {
                currentPlayer.Pause();
            }
        }

        enum FadeType
        {
            Alpha,
            Color,
            None
        }

        public void Stop()
        {
            StopVideo();
        }

        public void Seek(double NewTime)
        {
            currentPlayer.Seek(NewTime);
        }

        public void SeekToFrame(int Frame)
        {
            currentPlayer.SeekToFrame(Frame);
        }

        public void Switch(string videoName, bool bAutoPlay = true)
        {
            var _idx = FindVideoIndex(videoName);
            if (_idx == -1)
            {
                Debug.LogWarning("Video path not found: " + videoName);
                return;
            }

            Switch(_idx, bAutoPlay);
        }

        public void Switch(int mediaIndex, bool bAutoPlay = true)
        {
            if (mediaIndex < 0 || mediaIndex >= videoPaths.Count)
            {
                Debug.LogWarning("mediaIndex out of range: " + mediaIndex);
                return;
            }

            fadePlay(
                mediaIndex,
                () =>
                {
                    if (bAutoPlay)
                    {
                        currentPlayer.Play();
                    }
                }
            );
        }

        public void SwitchNext(bool bAutoPlay = true)
        {
            fadePlay(
                videoIndex.NextValue(),
                () =>
                {
                    if (bAutoPlay)
                    {
                        currentPlayer.Play();
                    }
                }
            );
        }

        public void SwitchPrev(bool bAutoPlay = true)
        {
            fadePlay(
                videoIndex.PrevValue(),
                () =>
                {
                    if (bAutoPlay)
                    {
                        currentPlayer.Play();
                    }
                }
            );
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

        public int FindVideoIndex(string videoName)
        {
            var _idx = videoPaths.FindIndex(_path => _path == videoName);
            if (_idx == -1)
            {
                return videoPaths.FindIndex(_ => _.EndsWith(videoName));
            }
            return _idx;
        }

        private AVProPlayer currentPlayer
        {
            get => transform.GetChild(videoIndex.Current).GetComponent<AVProPlayer>();
        }

        public AVProPlayer CurrentPlayer
        {
            get => currentPlayer;
        }

        private RawImage rawImage = null;
        public RawImage RawImage_Render
        {
            get
            {
                if (rawImage == null)
                {
                    rawImage = this.Get<RawImage>();
                }
                return rawImage;
            }
        }

        private DisplayUGUI displayUGUI = null;
        public DisplayUGUI DisplayUGUI_Render
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

        private IObservable<Texture> getMediaTexture(int mediaIndex)
        {
            var _displayUGUI = transform.GetChild(mediaIndex).GetComponent<DisplayUGUI>();
            return Observable
                .EveryUpdate()
                .Where(_ => _displayUGUI.HasValidTexture())
                .First()
                .Timeout(TimeSpan.FromSeconds(3))
                .Select(_ => _displayUGUI.mainTexture);
        }

        private void fadePlay(int newMediaIndex, Action onCleared = null)
        {
            if (fadeType == FadeType.None)
            {
                videoIndex.Set(newMediaIndex);
                onCleared?.Invoke();
                return;
            }
            else if (fadeType == FadeType.Alpha)
            {
                fadePlayByAlpha(newMediaIndex, onCleared);
            }
            else if (fadeType == FadeType.Color)
            {
                fadePlayByColor(newMediaIndex, onCleared);
            }
        }

        async void fadePlayByColor(int newMediaIndex, Action onCleared = null)
        {
            var _renderMat = RawImage_Render.material;
            var _oldPlayer = currentPlayer;
            videoIndex.Set(newMediaIndex); // 此处更新缓冲区

            // wait unitl isSwapChainBufferReady
            await Observable.EveryUpdate().Where(_ => isSwapChainBufferReady).First();

            var _targetWeight = swapRenderChain();
            var _newPlayer = currentPlayer;

            DOTween
                .Sequence()
                .Append(
                    _renderMat
                        .DOFloat(0.5f, "_Weight", FadeDuration * 0.5f)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (_oldPlayer != currentPlayer)
                                _oldPlayer.Pause();
                            onCleared?.Invoke();
                        })
                )
                .Append(
                    _renderMat
                        .DOFloat(_targetWeight, "_Weight", FadeDuration * 0.5f)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            // 暂停当前播放器   播放新的视频
                            if (_oldPlayer != currentPlayer)
                                _oldPlayer.Stop();
                        })
                )
                .PlayForward();
        }

        private void fadePlayByAlpha(int newMediaIndex, Action onCleared = null)
        {
            var _oldPlayer = currentPlayer;
            DOTween
                .Sequence()
                .Append(
                    this.Get<Graphic>()
                        .DOFade(0, FadeOut)
                        .OnComplete(() =>
                        {
                            _oldPlayer.Stop();
                            videoIndex.Set(newMediaIndex);
                            onCleared?.Invoke();
                        })
                )
                .Append(this.Get<Graphic>().DOFade(1, FadeIn))
                .PlayForward();
        }

        private void StopVideo()
        {
            if (currentPlayer == null)
                return;
            currentPlayer.Rewind(true);
        }
    }
}
