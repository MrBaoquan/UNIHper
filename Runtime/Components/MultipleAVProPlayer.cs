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
        DisplayFadeType fadeType = DisplayFadeType.Alpha;

        public DisplayFadeType FadeType
        {
            get => fadeType;
            set
            {
                fadeType = value;
                Debug.LogWarning(fadeType);
            }
        }

        private ValueDropdownList<DisplayFadeType> getFadeTypes()
        {
            var _fadeTypes = new ValueDropdownList<DisplayFadeType>()
            {
                DisplayFadeType.None,
                DisplayFadeType.Alpha
            };
            if (renderTarget == RenderTarget.RawImage)
            {
                _fadeTypes.Add(DisplayFadeType.Color);
            }
            return _fadeTypes;
        }

        [SerializeField, ShowIf("fadeType", DisplayFadeType.Alpha), LabelText("Fade In Duration")]
        public float FadeAlphaIn = 0.60f;

        [SerializeField, ShowIf("fadeType", DisplayFadeType.Alpha), LabelText("Fade Out Duration")]
        public float FadeAlphaOut = 0.40f;

        [SerializeField, ShowIf("fadeType", DisplayFadeType.Color), LabelText("Fade Duration")]
        public float FadeColorDuration = 1.0f;

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
            videoIndex.SetToMax();
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

        // 准备就绪的媒体贴图
        private int readyMediaTextureID = -1;

        /// <summary>
        /// Called when all videos are prepared
        /// </summary>
        private void onReady()
        {
            Debug.LogWarning("fadeType:" + fadeType);
            setupRenderMaterial();
            videoIndex
                .OnIndexChangedAsObservable()
                .Subscribe(async _newVideoIndex =>
                {
                    if (renderTarget == RenderTarget.RawImage)
                    {
                        isSwapChainBufferReady = false;
                        var _texture = await getMediaTexture(_newVideoIndex);
                        readyMediaTextureID = _newVideoIndex;
                        RawImage_Render.texture = _texture;
                        if (fadeType == DisplayFadeType.Color)
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
                .AddTo(currentPlayer);

            fadePlay(videoIndex.SetToMin(), () => { });
        }

        private void setupRenderMaterial()
        {
            if (renderTarget == RenderTarget.RawImage)
            {
                var _displayMat = new Material(fadeShader);
                _displayMat.SetFloat("_FlipY", 1);
                _displayMat.SetTextureScale("_MainTex", new Vector2(1, -1));
                _displayMat.SetTextureOffset("_MainTex", Vector2.up);
                if (fadeType == DisplayFadeType.None)
                {
                    _displayMat.SetInteger("_Enable", 0);
                }
                else if (fadeType == DisplayFadeType.Color)
                {
                    _displayMat.SetInteger("_Enable", 1);
                    _displayMat.SetFloat("_Weight", 1);
                }
                else if (fadeType == DisplayFadeType.Alpha)
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

        public enum DisplayFadeType
        {
            Alpha,
            Color,
            None
        }

        public void Stop()
        {
            StopVideo();
        }

        public void Rewind(bool pause = true)
        {
            currentPlayer.Rewind(pause);
        }

        public void Seek(double NewTime)
        {
            currentPlayer.Seek(NewTime);
        }

        public void SeekToFrame(int Frame)
        {
            currentPlayer.SeekToFrame(Frame);
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

            fadePlay(
                mediaIndex,
                () =>
                {
                    if (bRewind)
                    {
                        currentPlayer.Rewind(_ =>
                        {
                            if (bAutoPlay)
                            {
                                _.Play();
                            }
                        });
                    }
                    else if (bAutoPlay)
                    {
                        currentPlayer.Play();
                    }
                }
            );
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
                .Select(_ => _displayUGUI.mainTexture)
                .Catch<Texture, Exception>(_ex =>
                {
                    Debug.LogError(
                        $"getMediaTexture TimeoutException: {_ex.Message}, mediaIndex:{mediaIndex}"
                    );
                    return Observable.Return<Texture>(null);
                });
        }

        private bool fadeCompleted = false;

        // private int fadeTargetIndex = -1;

        private List<AVProPlayer> cachedReadyToStopPlayers = new();

        private void fadePlay(int newMediaIndex, Action onCleared = null)
        {
            if (
                fadeCompleted == false /*&& fadeTargetIndex != -1*/
            )
            {
                Debug.LogWarning("fadePlay is called before last fade completed.");
                cachedReadyToStopPlayers.ForEach(_ => _.Stop());
            }
            // fadeTargetIndex = newMediaIndex;
            fadeCompleted = false;
            void _onFadeCompleted()
            {
                fadeCompleted = true;
                // fadeTargetIndex = -1;
                onCleared?.Invoke();
            }

            if (fadeType == DisplayFadeType.None)
            {
                videoIndex.Set(newMediaIndex);
                _onFadeCompleted();
                return;
            }
            else if (fadeType == DisplayFadeType.Alpha)
            {
                fadePlayByAlpha(newMediaIndex, _onFadeCompleted);
            }
            else if (fadeType == DisplayFadeType.Color)
            {
                fadePlayByColor(newMediaIndex, _onFadeCompleted);
            }
        }

        Sequence fadeColorTweener = null;
        IDisposable fadeColorDisposeable = null;

        void fadePlayByColor(int newMediaIndex, Action onCleared = null)
        {
            fadeColorTweener?.Kill();
            fadeAlphaTweener = null;

            fadeColorDisposeable?.Dispose();
            fadeColorDisposeable = null;

            var _renderMat = RawImage_Render.material;
            var _oldPlayer = currentPlayer;
            cachedReadyToStopPlayers.Add(_oldPlayer);

            videoIndex.SetAndForceNotify(newMediaIndex); // 此处更新缓冲区
            fadeColorDisposeable = Observable
                .EveryUpdate()
                .Where(_ => isSwapChainBufferReady && readyMediaTextureID == newMediaIndex)
                .First()
                .Subscribe(_ =>
                {
                    fadeColorDisposeable = null;

                    var _targetWeight = swapRenderChain();
                    var _newPlayer = currentPlayer;

                    fadeColorTweener = DOTween
                        .Sequence()
                        .Append(
                            _renderMat
                                .DOFloat(0.5f, "_Weight", FadeColorDuration * 0.5f)
                                .SetEase(Ease.Linear)
                                .OnComplete(() =>
                                {
                                    cachedReadyToStopPlayers.RemoveAll(_ => _ == _oldPlayer);
                                    if (_oldPlayer != currentPlayer)
                                        _oldPlayer.Pause();
                                    onCleared?.Invoke();
                                })
                        )
                        .Append(
                            _renderMat
                                .DOFloat(_targetWeight, "_Weight", FadeColorDuration * 0.5f)
                                .SetEase(Ease.Linear)
                                .OnComplete(() =>
                                {
                                    // 暂停当前播放器   播放新的视频
                                    if (_oldPlayer != currentPlayer)
                                        _oldPlayer.Stop();
                                })
                        );
                    fadeColorTweener.PlayForward();
                });
        }

        Sequence fadeAlphaTweener = null;

        private void fadePlayByAlpha(int newMediaIndex, Action onCleared = null)
        {
            if (fadeAlphaTweener != null)
                fadeAlphaTweener.Kill();

            var _oldPlayer = currentPlayer;
            cachedReadyToStopPlayers.Add(_oldPlayer);

            fadeAlphaTweener = DOTween
                .Sequence()
                .Append(
                    this.Get<Graphic>()
                        .DOFade(0, FadeAlphaOut)
                        .OnComplete(() =>
                        {
                            cachedReadyToStopPlayers.RemoveAll(_ => _ == _oldPlayer);
                            _oldPlayer.Rewind(true);
                            videoIndex.SetAndForceNotify(newMediaIndex);
                            onCleared?.Invoke();
                        })
                )
                .Append(this.Get<Graphic>().DOFade(1, FadeAlphaIn));
            fadeAlphaTweener
                .OnComplete(() =>
                {
                    fadeAlphaTweener = null;
                })
                .PlayForward();
        }

        private void StopVideo()
        {
            if (currentPlayer == null)
                return;
            currentPlayer.Stop();
        }
    }
}
