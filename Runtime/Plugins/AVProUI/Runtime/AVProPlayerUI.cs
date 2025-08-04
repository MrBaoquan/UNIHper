using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UNIHper;
using DG.Tweening;
using RenderHeads.Media.AVProVideo;
using RenderHeads.Media.AVProVideo.Demos.UI;
using DigitalRubyShared;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

namespace AVProUI
{
    using UniRx;
    using UniRx.Triggers;

    [RequireComponent(typeof(AVProPlayer)), RequireComponent(typeof(AudioOutput))]
    public class AVProPlayerUI : MonoBehaviour
    {
        [Title("Player Settings")]
        [SerializeField, Tooltip("The FullScreen Rect where the video will be transformed to fit")]
        RectTransform FullScreenRect;

        [
            SerializeField,
            Tooltip(
                "Auto hide controls after a few seconds when video is playing, less than 0 to disable auto hide"
            ),
            MaxValue(30),
            MinValue(-1)
        ]
        float _autoHideControls = 3f;

        bool AutoHide => _autoHideControls > 0;

        [
            SerializeField,
            Tooltip("Forward/Backward time in seconds when tapping on buttons"),
            MinValue(0.1)
        ]
        private float _jumpDeltaTime = 5f;

        [Title("Controls Settings")]
        [SerializeField, OnValueChanged("OnEnableFullScreenChanged")]
        private bool _enableFullScreen = true;

        private Canvas _renderCanvas = null;
        private Canvas renderCanvas
        {
            get
            {
                if (_renderCanvas == null)
                {
                    _renderCanvas = GetComponentInParent<Canvas>();
                }
                return _renderCanvas;
            }
        }

        private void OnEnableFullScreenChanged()
        {
            if (_enableFullScreen)
            {
                transform.Find("Controls/BottomRow/EnterFullScreen").SetActive(true);
            }
            else
            {
                transform.Find("Controls/BottomRow/EnterFullScreen").SetActive(false);
            }
        }

        private AVProPlayer avPlayer;
        private MediaPlayer _mediaPlayer => avPlayer?.MediaPlayer;

        private Button _buttonPlayPause;
        private Button _buttonTimeBack;
        private Button _buttonTimeForward;

        private Material _playPauseMaterial;
        private Material _volumeMaterial;
        private Slider _sliderTime;
        private Slider _sliderVolume = null;
        private float _audioVolume = 1f;
        private float _audioFade = 0f;
        private const float AudioFadeDuration = 0.25f;
        private float _audioFadeTime = 0f;
        private bool _isAudioFadingUpToPlay = true;
        private GameObject _liveItem = null;

        private RectTransform timelineTip;
        private HorizontalSegmentsPrimitive _segmentsSeek = null;
        private HorizontalSegmentsPrimitive _segmentsProgress = null;

        private Text _textTimeDuration = null;
        private bool _useAudioFading = true;
        private RawImage _imageAudioSpectrum = null;
        private Material _audioSpectrumMaterial;
        private OverlayManager _overlayManager = null;

        private readonly LazyShaderProperty _propMorph = new LazyShaderProperty("_Morph");
        private readonly LazyShaderProperty _propMute = new LazyShaderProperty("_Mute");
        private readonly LazyShaderProperty _propVolume = new LazyShaderProperty("_Volume");
        private readonly LazyShaderProperty _propSpectrum = new LazyShaderProperty("_Spectrum");
        private readonly LazyShaderProperty _propSpectrumRange = new LazyShaderProperty(
            "_SpectrumRange"
        );

        EventTrigger _videoTouch = null;
        CanvasGroup _controlsGroup = null;

        public void SetAVProPlayer(AVProPlayer aVProPlayer)
        {
            this.avPlayer = aVProPlayer;
            registerVideoControlDisposables();
        }

        private Material DuplicateMaterialOnImage(Graphic image)
        {
            // Assign a copy of the material so we aren't modifying the material asset file
            image.material = new Material(image.material);
            return image.material;
        }

        ControlsScaler _controlsScaler = null;

        // Start is called before the first frame update
        void Start()
        {
            setupPropertyReferences();
            CreateTimelineDragEvents();

            UpdateVolumeSlider();
            CreateVolumeSliderEvents();

            setupTapGestures();

            setupBasicControlButtons();
            setupControlsShowOrHide();

            if (avPlayer == null)
                SetAVProPlayer(GetComponent<AVProPlayer>());
        }

        private void setupTapGestures()
        {
            TapGestureRecognizer _tapGesture = new TapGestureRecognizer
            {
                MaximumNumberOfTouchesToTrack = 10
            };
            _tapGesture.StateUpdated += (gesture) =>
            {
                if (gesture.State == GestureRecognizerState.Ended)
                {
                    var _touchPoint = new Vector2(gesture.FocusX, gesture.FocusY);

                    if (checkIfScreenPointInControlsArea(_touchPoint))
                    {
                        return;
                    }

                    OnVideoPointerUp();
                }
            };
            _tapGesture.ThresholdSeconds = 0.15f;

            // double tap
            TapGestureRecognizer _doubleTapGesture = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 2,
            };
            _doubleTapGesture.ThresholdSeconds = 0.15f;
            _tapGesture.RequireGestureRecognizerToFail = _doubleTapGesture;

            _doubleTapGesture.StateUpdated += (gesture) =>
            {
                if (gesture.State == GestureRecognizerState.Ended)
                {
                    if (
                        checkIfScreenPointInControlsArea(
                            new Vector2(gesture.FocusX, gesture.FocusY)
                        )
                    )
                    {
                        return;
                    }

                    ToggleFullScreen();
                }
            };
            _tapGesture.PlatformSpecificView = _videoTouch.gameObject;
            _doubleTapGesture.PlatformSpecificView = _videoTouch.gameObject;
            FingersScript.Instance.AddGesture(_tapGesture);
            FingersScript.Instance.AddGesture(_doubleTapGesture);
        }

        private bool checkIfScreenPointInControlsArea(Vector2 screenPos)
        {
            if (_controlsGroup == null)
                return false;
            RectTransform rect = _controlsGroup.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                screenPos,
                null,
                out Vector2 canvasPos
            );

            Rect rr = RectTransformUtility.PixelAdjustRect(rect, null);
            rr.height += 40;
            return rr.Contains(canvasPos);
        }

        ReactiveProperty<bool> _isFullScreen = new ReactiveProperty<bool>(false);

        private void setupPropertyReferences()
        {
            _controlsScaler = this.Get<ControlsScaler>("Controls");

            _buttonPlayPause = this.Get<Button>("Controls/BottomRow/ButtonPlayPause");
            _playPauseMaterial = DuplicateMaterialOnImage(_buttonPlayPause.image);

            _liveItem = this.Get("Controls/BottomRow/TextLive").gameObject;
            _sliderTime = this.Get<Slider>("Controls/Timeline");
            timelineTip = this.Get<RectTransform>("Controls/Timeline-Hovers");
            _segmentsSeek = this.Get<HorizontalSegmentsPrimitive>(
                "Controls/Timeline/Fill Area/Fill-Seek"
            );
            _sliderVolume = this.Get<Slider>("Controls/BottomRow/VolumeMask/SliderVolume");
            _segmentsProgress = this.Get<HorizontalSegmentsPrimitive>(
                "Controls/Timeline/Fill Area/Fill-Progress"
            );
            _textTimeDuration = this.Get<Text>("Controls/BottomRow/TextTimeDuration");
            _volumeMaterial = DuplicateMaterialOnImage(
                this.Get<Button>("Controls/BottomRow/ButtonVolume").image
            );

            // 音频频谱
            _imageAudioSpectrum = this.Get<RawImage>("Controls/BottomRow/AudioSpectrum");
            _audioSpectrumMaterial = DuplicateMaterialOnImage(_imageAudioSpectrum);

            // overlay
            _overlayManager = this.Get<OverlayManager>("Overlays");

            _controlsGroup = this.Get<CanvasGroup>("Controls");
            _videoTouch = this.Get<EventTrigger>("Video/VideoDisplay");

            var _buttonEnterFullScreen = this.Get<Button>("Controls/BottomRow/EnterFullScreen");
            var _buttonExitFullScreen = this.Get<Button>("Controls/BottomRow/ExitFullScreen");

            _buttonEnterFullScreen
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    EnterFullScreen();
                });

            _buttonExitFullScreen
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ExitFullScreen();
                });

            if (FullScreenRect == null)
            {
                // find first canvas of parent
                var _transform = transform;
                while (_transform.parent != null)
                {
                    _transform = _transform.parent;
                    var _canvas = _transform.GetComponent<Canvas>();
                    if (_canvas != null)
                    {
                        FullScreenRect = _canvas.GetComponent<RectTransform>();
                        break;
                    }
                }
                if (FullScreenRect == null)
                {
#if UNITY_2023_1_OR_NEWER
                    var _canvas_default = GameObject.FindFirstObjectByType<Canvas>();
#else
                    var _canvas_default = GameObject.FindObjectOfType<Canvas>();
#endif
                    if (_canvas_default == null)
                    {
                        Debug.LogError("No Canvas found in scene");
                        return;
                    }
                    FullScreenRect = _canvas_default.GetComponent<RectTransform>();
                }
                if (FullScreenRect == null)
                {
                    Debug.LogError("No Canvas found in scene");
                    return;
                }
            }

            _isFullScreen.Subscribe(_fullScreen =>
            {
                if (!_enableFullScreen)
                {
                    _buttonEnterFullScreen.gameObject.SetActive(false);
                    _buttonExitFullScreen.gameObject.SetActive(false);
                    return;
                }
                if (_fullScreen)
                {
                    _buttonEnterFullScreen.gameObject.SetActive(false);
                    _buttonExitFullScreen.gameObject.SetActive(true);
                }
                else
                {
                    _buttonEnterFullScreen.gameObject.SetActive(true);
                    _buttonExitFullScreen.gameObject.SetActive(false);
                }
            });

            if (isFullScreen())
            {
                _enableFullScreen = false;
                _isFullScreen.SetValueAndForceNotify(true);
            }
            else
            {
                _isFullScreen.SetValueAndForceNotify(false);
            }
            fetchDefaultWindowsInfo();
        }

        private void fetchDefaultWindowsInfo()
        {
            var _videoUIRect = this.Get<RectTransform>();

            _defaultPosition = _videoUIRect.localPosition;
            var _bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                transform.parent,
                transform
            );

            _videoUIRect.anchorMin = Vector2.one * 0.5f;
            _videoUIRect.anchorMax = Vector2.one * 0.5f;
            _videoUIRect.pivot = Vector2.one * 0.5f;

            _defaultPosition = RectTransformUtility.PixelAdjustPoint(
                _bounds.center,
                transform,
                null
            );
            _defaultSize = RectTransformUtility.PixelAdjustPoint(_bounds.size, transform, null);
            _videoUIRect.localPosition = _defaultPosition;
            _videoUIRect.sizeDelta = _defaultSize;
        }

        private readonly List<IDisposable> _videoControlDisposables = new();

        private void registerVideoControlDisposables()
        {
            _videoControlDisposables?.ForEach(_ => _.Dispose());
            _videoControlDisposables?.Clear();

            if (avPlayer == null)
                return;

            // 暂停时显示控制条
            _videoControlDisposables.Add(
                avPlayer
                    .OnPausedAsObservable()
                    .Subscribe(_ =>
                    {
                        clearHideControls();
                        FadeUpControls();
                    })
            );

            // 自动隐藏控制条
            _videoControlDisposables.Add(
                Observable
                    .Merge(
                        avPlayer.OnStartedAsObservable().Select(_ => avPlayer),
                        avPlayer.OnFinishedSeekingAsObservable().Select(_ => avPlayer),
                        avPlayer.OnVolumeChangedAsObservable(),
                        avPlayer.OnMuteChangedAsObservable()
                    )
                    .Where(_ => gameObject.activeInHierarchy)
                    .Subscribe(_ =>
                    {
                        autoHideControls(_autoHideControls);
                    })
            );
        }

        private void setupControlsShowOrHide()
        {
            // 鼠标移入时显示控制条
            this.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(_ =>
                {
                    if (_.pointerId != 2)
                        return;
                    FadeUpControls();
                })
                .AddTo(this);

            // 鼠标移出时隐藏控制条
            this.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(_ =>
                {
                    if (_.pointerId != 2)
                        return;
                    if (avPlayer.IsPaused)
                        return;
                    FadeDownControls();
                });
        }

        private void setupBasicControlButtons()
        {
            _buttonPlayPause.OnClickAsObservable().Subscribe(OnPlayPauseButtonPressed).AddTo(this);

            this.Get<Button>("Controls/BottomRow/ButtonNavBack")
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    SeekRelative(-_jumpDeltaTime);
                })
                .AddTo(this);

            this.Get<Button>("Controls/BottomRow/ButtonNavForward")
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    SeekRelative(_jumpDeltaTime);
                })
                .AddTo(this);

            this.Get<Button>("Controls/BottomRow/ButtonVolume")
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ToggleMute();
                })
                .AddTo(this);
        }

        Vector3 _defaultPosition;
        Vector3 _defaultSize;

        private bool isFullScreen()
        {
            var _videoUIRect = this.Get<RectTransform>();
            var _playerBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                _videoUIRect.parent,
                _videoUIRect
            );
            var _screenBounds = FullScreenRect.TransformBoundsTo(_videoUIRect.parent);
            var _rCenter = RectTransformUtility.PixelAdjustPoint(
                _playerBounds.center,
                _videoUIRect,
                null
            );
            var _rSize = RectTransformUtility.PixelAdjustPoint(
                _playerBounds.size,
                _videoUIRect,
                null
            );
            var _lCenter = RectTransformUtility.PixelAdjustPoint(
                _screenBounds.center,
                _videoUIRect,
                null
            );
            var _lSize = RectTransformUtility.PixelAdjustPoint(
                _screenBounds.size,
                _videoUIRect,
                null
            );

            bool _isFullScreen =
                Mathf.RoundToInt(_rCenter.x) == Mathf.RoundToInt(_lCenter.x)
                && Mathf.RoundToInt(_rCenter.y) == Mathf.RoundToInt(_lCenter.y)
                && Mathf.RoundToInt(_rSize.x) == Mathf.RoundToInt(_lSize.x)
                && Mathf.RoundToInt(_rSize.y) == Mathf.RoundToInt(_lSize.y);

            Debug.Log(_isFullScreen);
            return _isFullScreen;
        }

        private void ToggleFullScreen()
        {
            var _isFullScreen = isFullScreen();
            if (_isFullScreen)
            {
                ExitFullScreen();
            }
            else
            {
                EnterFullScreen();
            }
        }

        public void EnterFullScreen()
        {
            if (isFullScreen())
                return;
            var _videoUIRect = this.Get<RectTransform>();

            _videoUIRect.anchorMin = Vector2.one * 0.5f;
            _videoUIRect.anchorMax = Vector2.one * 0.5f;

            var _newBounds = FullScreenRect.TransformBoundsTo(_videoUIRect.parent);

            _videoUIRect.DOLocalMove(_newBounds.center, 0.5f).SetEase(Ease.OutCubic);
            _videoUIRect
                .DOSizeDelta(_newBounds.size, 0.5f)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() =>
                {
                    syncControlsScaler();
                })
                .OnComplete(() =>
                {
                    syncControlsScaler();
                });

            _isFullScreen.Value = true;
        }

        public void ExitFullScreen()
        {
            if (!isFullScreen())
                return;
            var _videoUIRect = this.Get<RectTransform>();
            _videoUIRect.anchorMin = Vector2.one * 0.5f;
            _videoUIRect.anchorMax = Vector2.one * 0.5f;
            _videoUIRect.DOLocalMove(_defaultPosition, 0.5f).SetEase(Ease.OutCubic);
            _videoUIRect
                .DOSizeDelta(_defaultSize, 0.5f)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() =>
                {
                    syncControlsScaler();
                })
                .OnComplete(() =>
                {
                    syncControlsScaler();
                });
            _isFullScreen.Value = false;
        }

        private void syncControlsScaler()
        {
            this.Get<ControlsScaler>("Controls").RefreshScaler();
        }

        private float _maxValue = 1f;
        private float[] _spectrumSamples = new float[128];
        private float[] _spectrumSamplesSmooth = new float[128];

        private void UpdateVolumeSlider()
        {
            _sliderVolume.value = _audioVolume;
        }

        private void UpdateAudioSpectrum()
        {
            bool showAudioSpectrum = false;
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                AudioSource audioSource = _mediaPlayer.AudioSource;

                if (audioSource && _audioSpectrumMaterial)
                {
                    showAudioSpectrum = true;

                    float maxFreq = (
                        RenderHeads.Media.AVProVideo.Helper.GetUnityAudioSampleRate() / 2
                    );

                    // Frequencies over 18Khz generally aren't very interesting to visualise, so clamp the range
                    const float clampFreq = 18000f;
                    int sampleRange = Mathf.FloorToInt(
                        Mathf.Clamp01(clampFreq / maxFreq) * _spectrumSamples.Length
                    );

                    // Add new samples and smooth the samples over time
                    audioSource.GetSpectrumData(_spectrumSamples, 0, FFTWindow.BlackmanHarris);

                    // Find the maxValue sample for normalising with
                    float maxValue = -1.0f;
                    for (int i = 0; i < sampleRange; i++)
                    {
                        if (_spectrumSamples[i] > maxValue)
                        {
                            maxValue = _spectrumSamples[i];
                        }
                    }

                    // Chase maxValue to zero
                    _maxValue = Mathf.Lerp(_maxValue, 0.0f, Mathf.Clamp01(2.0f * Time.deltaTime));

                    // Update maxValue
                    _maxValue = Mathf.Max(_maxValue, maxValue);
                    if (_maxValue <= 0.01f)
                    {
                        _maxValue = 1f;
                    }

                    // Copy and smooth the spectrum values
                    for (int i = 0; i < sampleRange; i++)
                    {
                        float newSample = _spectrumSamples[i] / _maxValue;
                        _spectrumSamplesSmooth[i] = Mathf.Lerp(
                            _spectrumSamplesSmooth[i],
                            newSample,
                            Mathf.Clamp01(15.0f * Time.deltaTime)
                        );
                    }

                    // Update shader
                    _audioSpectrumMaterial.SetFloatArray(_propSpectrum.Id, _spectrumSamplesSmooth);
                    _audioSpectrumMaterial.SetFloat(_propSpectrumRange.Id, (float)sampleRange);
                }
            }

            if (_imageAudioSpectrum)
            {
                _imageAudioSpectrum.gameObject.SetActive(showAudioSpectrum);
            }
        }

        void OnPlayPauseButtonPressed(Unit _unit)
        {
            TogglePlayPause();
        }

        private void Play()
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                if (_overlayManager)
                {
                    _overlayManager.TriggerFeedback(OverlayManager.Feedback.Play);
                }
                _mediaPlayer.Play();
            }
        }

        public void TogglePlayPause()
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                if (_useAudioFading && _mediaPlayer.Info.HasAudio())
                {
                    if (_mediaPlayer.Control.IsPlaying())
                    {
                        if (_overlayManager)
                        {
                            _overlayManager.TriggerFeedback(OverlayManager.Feedback.Pause);
                        }
                        _isAudioFadingUpToPlay = false;
                    }
                    else
                    {
                        _isAudioFadingUpToPlay = true;
                        Play();
                    }
                    _audioFadeTime = 0f;
                }
                else
                {
                    if (_mediaPlayer.Control.IsPlaying())
                    {
                        Pause();
                    }
                    else
                    {
                        Play();
                    }
                }
            }
        }

        public void ToggleMute()
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                if (_mediaPlayer.AudioMuted)
                {
                    MuteAudio(false);
                }
                else
                {
                    MuteAudio(true);
                }
            }
        }

        private void MuteAudio(bool mute)
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                // Change mute
                _mediaPlayer.AudioMuted = mute;

                // Update the UI
                // The UI element is constantly updated by the Update() method

                // Trigger the overlays
                if (_overlayManager)
                {
                    _overlayManager.TriggerFeedback(
                        mute ? OverlayManager.Feedback.VolumeMute : OverlayManager.Feedback.VolumeUp
                    );
                }
            }
        }

        private void CreateVolumeSliderEvents()
        {
            if (_sliderVolume != null)
            {
                EventTrigger trigger = _sliderVolume.gameObject.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.PointerDown;
                    entry.callback.AddListener(
                        (data) =>
                        {
                            OnVolumeSliderDrag();
                        }
                    );
                    trigger.triggers.Add(entry);

                    entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.Drag;
                    entry.callback.AddListener(
                        (data) =>
                        {
                            OnVolumeSliderDrag();
                        }
                    );
                    trigger.triggers.Add(entry);
                }
            }
        }

        private void OnVolumeSliderDrag()
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                _audioVolume = _sliderVolume.value;
                ApplyAudioVolume();
            }
        }

        private void ApplyAudioVolume()
        {
            if (_mediaPlayer)
            {
                _mediaPlayer.AudioVolume = (_audioVolume * _audioFade);
            }
        }

        private void CreateTimelineDragEvents()
        {
            EventTrigger trigger = _sliderTime.gameObject.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerDown;
                entry.callback.AddListener(
                    (data) =>
                    {
                        OnTimeSliderBeginDrag();
                    }
                );
                trigger.triggers.Add(entry);

                entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.Drag;
                entry.callback.AddListener(
                    (data) =>
                    {
                        OnTimeSliderDrag();
                    }
                );
                trigger.triggers.Add(entry);

                entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerUp;
                entry.callback.AddListener(
                    (data) =>
                    {
                        OnTimeSliderEndDrag();
                    }
                );
                trigger.triggers.Add(entry);

                entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener(
                    (data) =>
                    {
                        OnTimelineBeginHover((PointerEventData)data);
                    }
                );
                trigger.triggers.Add(entry);

                entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerExit;
                entry.callback.AddListener(
                    (data) =>
                    {
                        OnTimelineEndHover((PointerEventData)data);
                    }
                );
                trigger.triggers.Add(entry);
            }
        }

        public void SeekRelative(float deltaTime)
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                TimeRange timelineRange = GetTimelineRange();
                double time = _mediaPlayer.Control.GetCurrentTime() + deltaTime;
                time = System.Math.Max(time, timelineRange.startTime);
                time = System.Math.Min(time, timelineRange.startTime + timelineRange.duration);
                _mediaPlayer.Control.Seek(time);

                if (_overlayManager)
                {
                    _overlayManager.TriggerFeedback(
                        deltaTime > 0f
                            ? OverlayManager.Feedback.SeekForward
                            : OverlayManager.Feedback.SeekBack
                    );
                }
            }
        }

        private bool _wasPlayingBeforeTimelineDrag;

        void OnTimeSliderBeginDrag()
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                _wasPlayingBeforeTimelineDrag = _mediaPlayer.Control.IsPlaying();
                if (_wasPlayingBeforeTimelineDrag)
                {
                    _mediaPlayer.Pause();
                }
                OnTimeSliderDrag();
            }
        }

        private void OnTimeSliderDrag()
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                TimeRange timelineRange = GetTimelineRange();
                double time =
                    timelineRange.startTime + (_sliderTime.value * timelineRange.duration);
                _mediaPlayer.Control.Seek(time);
                _isHoveringOverTimeline = true;
            }
        }

        private void OnTimeSliderEndDrag()
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                if (_wasPlayingBeforeTimelineDrag)
                {
                    _mediaPlayer.Play();
                    _wasPlayingBeforeTimelineDrag = false;
                }
            }
        }

        private bool _isHoveringOverTimeline = false;

        void OnTimelineBeginHover(PointerEventData _data)
        {
            _isHoveringOverTimeline = true;
            _sliderTime.transform.localScale = new Vector3(1f, 2.5f, 1f);
        }

        void OnTimelineEndHover(PointerEventData _data)
        {
            _isHoveringOverTimeline = false;
            _sliderTime.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        private void OnVideoPointerUp()
        {
            bool controlsMostlyVisible = (
                _controlsGroup.alpha >= 0.5f && _controlsGroup.gameObject.activeSelf
            );
            if (controlsMostlyVisible)
            {
                TogglePlayPause();
            }
            else
            {
                FadeUpControls();
                autoHideControls(5);
            }
        }

        IDisposable _autoHideControlsDisposable;

        private void clearHideControls()
        {
            if (_autoHideControlsDisposable != null)
            {
                _autoHideControlsDisposable.Dispose();
                _autoHideControlsDisposable = null;
            }
        }

        private void autoHideControls(float autoHideTime = 3f)
        {
            clearHideControls();
            _autoHideControlsDisposable = Observable
                .Timer(TimeSpan.FromSeconds(autoHideTime))
                .Subscribe(_ =>
                {
                    _autoHideControlsDisposable = null;
                    if (_mediaPlayer.Control.IsPaused())
                        return;

#if ENABLE_INPUT_SYSTEM
                    if (checkIfScreenPointInControlsArea(Mouse.current.position.ReadValue()))
                        return;
#else
                    if (checkIfScreenPointInControlsArea(Input.mousePosition))
                        return;
#endif

                    if (_controlsGroup.alpha >= 0.5f && _controlsGroup.gameObject.activeSelf)
                    {
                        FadeDownControls();
                    }
                });
        }

        private TimeRange GetTimelineRange()
        {
            if (_mediaPlayer.Info != null)
            {
                return RenderHeads.Media.AVProVideo.Helper.GetTimelineRange(
                    _mediaPlayer.Info.GetDuration(),
                    _mediaPlayer.Control.GetSeekableTimes()
                );
            }
            return new TimeRange();
        }

        private void Pause(bool skipFeedback = false)
        {
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                if (!skipFeedback)
                {
                    if (_overlayManager)
                    {
                        _overlayManager.TriggerFeedback(OverlayManager.Feedback.Pause);
                    }
                }
                _mediaPlayer.Pause();
            }
        }

        void UpdateAudioFading()
        {
            if (_mediaPlayer == null)
                return;
            // Increment fade timer
            if (_audioFadeTime < AudioFadeDuration)
            {
                _audioFadeTime = Mathf.Clamp(
                    _audioFadeTime + Time.deltaTime,
                    0f,
                    AudioFadeDuration
                );
            }

            // Trigger pause when audio faded down
            if (_audioFadeTime >= AudioFadeDuration)
            {
                if (!_isAudioFadingUpToPlay)
                {
                    Pause(skipFeedback: true);
                    _isAudioFadingUpToPlay = true;
                }
            }

            // Apply audio fade value
            if (_mediaPlayer.Control != null && _mediaPlayer.Control.IsPlaying())
            {
                _audioFade = Mathf.Clamp01(_audioFadeTime / AudioFadeDuration);
                if (!_isAudioFadingUpToPlay)
                {
                    _audioFade = (1f - _audioFade);
                }
                ApplyAudioVolume();
            }
        }

        private void FadeDownControls()
        {
            if (!AutoHide)
            {
                return;
            }
            _controlsGroup.DOFade(0, 0.5f);
        }

        private void FadeUpControls()
        {
            _controlsGroup.DOFade(1, 0.5f);
        }

        private Camera renderCamera =>
            renderCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : renderCanvas.worldCamera;

        // Update is called once per frame
        void Update()
        {
            UpdateAudioFading();
            UpdateAudioSpectrum();
            if (_mediaPlayer == null || _mediaPlayer.Info == null)
                return;
            TimeRange timelineRange = GetTimelineRange();

            // Updated stalled display
            if (_overlayManager)
            {
                _overlayManager.Reset();
                if (_mediaPlayer.Info.IsPlaybackStalled())
                {
                    _overlayManager.TriggerStalled();
                }
            }

            if (_isHoveringOverTimeline)
            {
                timelineTip.SetActive(true);
                _segmentsSeek.gameObject.SetActive(true);

                var _canvasTransform = this.Get<RectTransform>();
                Vector2 canvasPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasTransform,
#if ENABLE_INPUT_SYSTEM
                    Mouse.current.position.ReadValue(),
#else
                    Input.mousePosition,
#endif
                    renderCamera,
                    out canvasPos
                );

                Vector3 mousePos = _canvasTransform.TransformPoint(canvasPos);

                // seek time position
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    _sliderTime.GetComponent<RectTransform>()
                );
                var _timeLinePos = new Vector2(mousePos.x, timelineTip.position.y);
                var _minPos = _sliderTime.Get<RectTransform>().TransformPoint(bounds.min);
                var _maxPos = _sliderTime.Get<RectTransform>().TransformPoint(bounds.max);
                _timeLinePos.x = Mathf.Clamp(_timeLinePos.x, _minPos.x, _maxPos.x);
                timelineTip.position = _timeLinePos;

                float x = Mathf.Clamp01(
                    (canvasPos.x - bounds.min.x * _controlsScaler.scale)
                        / (bounds.size.x * _controlsScaler.scale)
                );
                double time = (double)x * timelineRange.Duration;

                // Update time text
                Text hoverText = timelineTip.GetComponentInChildren<Text>();
                if (hoverText != null)
                {
                    time -= timelineRange.startTime;
                    time = System.Math.Max(time, 0.0);
                    time = System.Math.Min(time, timelineRange.Duration);
                    hoverText.text = RenderHeads.Media.AVProVideo.Helper.GetTimeString(time, false);
                }

                float[] ranges = new float[2];
                if (timelineRange.Duration > 0.0)
                {
                    double t = (
                        (_mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime)
                        / timelineRange.duration
                    );
                    ranges[1] = x;
                    ranges[0] = (float)t;
                }
                _segmentsSeek.Segments = ranges;
            }
            else
            {
                timelineTip.SetActive(false);
                _segmentsSeek.gameObject.SetActive(false);
            }

            if (!_isHoveringOverTimeline)
            {
                double t = 0.0;

                t = avPlayer.CurrentTime / avPlayer.Duration;
                _sliderTime.value = Mathf.Clamp01((float)t);
            }

            // Update progress segment
            if (_segmentsProgress)
            {
                TimeRanges times = _mediaPlayer.Control.GetBufferedTimes();
                float[] ranges = null;
                if (times.Count > 0 && timelineRange.Duration > 0.0)
                {
                    ranges = new float[2];
                    double x1 = (times.MinTime - timelineRange.startTime) / timelineRange.duration;
                    double x2 = (
                        (_mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime)
                        / timelineRange.duration
                    );
                    ranges[0] = Mathf.Max(0f, (float)x1);
                    ranges[1] = Mathf.Min(1f, (float)x2);
                }
                _segmentsProgress.Segments = ranges;
            }

            // Update time/duration text display
            if (_textTimeDuration)
            {
                string t1 = RenderHeads.Media.AVProVideo.Helper.GetTimeString(
                    (_mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime),
                    false
                );
                string d1 = RenderHeads.Media.AVProVideo.Helper.GetTimeString(
                    timelineRange.duration,
                    false
                );
                _textTimeDuration.text = string.Format("{0} / {1}", t1, d1);
            }

            // Update volume slider
            if (!_useAudioFading)
            {
                UpdateVolumeSlider();
            }

            // Animation play/pause button
            if (_playPauseMaterial != null)
            {
                float t = _playPauseMaterial.GetFloat(_propMorph.Id);
                float d = 1f;
                if (_mediaPlayer.Control.IsPlaying())
                {
                    d = -1f;
                }
                t += d * Time.deltaTime * 6f;
                t = Mathf.Clamp01(t);
                _playPauseMaterial.SetFloat(_propMorph.Id, t);
            }

            // Animation volume/mute button
            if (_volumeMaterial != null)
            {
                float t = _volumeMaterial.GetFloat(_propMute.Id);
                float d = 1f;
                if (!_mediaPlayer.AudioMuted)
                {
                    d = -1f;
                }
                t += d * Time.deltaTime * 6f;
                t = Mathf.Clamp01(t);
                _volumeMaterial.SetFloat(_propMute.Id, t);
                _volumeMaterial.SetFloat(_propVolume.Id, _audioVolume);
            }

            // Apply audio fade value
            if (_mediaPlayer.Control != null && _mediaPlayer.Control.IsPlaying())
            {
                _audioFade = Mathf.Clamp01(_audioFadeTime / AudioFadeDuration);
                if (!_isAudioFadingUpToPlay)
                {
                    _audioFade = (1f - _audioFade);
                }
                ApplyAudioVolume();
            }

            // Update LIVE text visible
            if (_liveItem)
            {
                _liveItem.SetActive(double.IsInfinity(_mediaPlayer.Info.GetDuration()));
            }
        }
    }
}
