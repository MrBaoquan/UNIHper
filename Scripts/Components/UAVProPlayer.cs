using System;
using System.Collections;
using System.Collections.Generic;
using RenderHeads.Media.AVProVideo;
using UniRx;
using UnityEngine;
using UnityEngine.Events;

namespace UNIHper {

    [RequireComponent (typeof (MediaPlayer))]
    public class UAVProPlayer : MonoBehaviour {

        #region 事件列表
        private UnityEvent<MediaPlayer> OnMetaDataReady = new UnityEvent<MediaPlayer> (); // Triggered when meta data(width, duration etc) is available
        private UnityEvent<MediaPlayer> OnReadyToPlay = new UnityEvent<MediaPlayer> (); // Triggered when the video is loaded and ready to play
        private UnityEvent<MediaPlayer> OnStarted = new UnityEvent<MediaPlayer> (); // Triggered when the playback starts
        private UnityEvent<MediaPlayer> OnFirstFrameReady = new UnityEvent<MediaPlayer> (); // Triggered when the first frame has been rendered
        private UnityEvent<MediaPlayer> OnFinishedPlaying = new UnityEvent<MediaPlayer> (); // Triggered when a non-looping video has finished playing
        private UnityEvent<MediaPlayer> OnClosing = new UnityEvent<MediaPlayer> (); // Triggered when the media is closed
        private UnityEvent<MediaPlayer> OnError = new UnityEvent<MediaPlayer> (); // Triggered when an error occurs
        private UnityEvent<MediaPlayer> OnSubtitleChange = new UnityEvent<MediaPlayer> (); // Triggered when the subtitles change
        private UnityEvent<MediaPlayer> OnStalled = new UnityEvent<MediaPlayer> (); // Triggered when media is stalled (eg. when lost connection to media stream) - Currently only supported on Windows platforms
        private UnityEvent<MediaPlayer> OnUnstalled = new UnityEvent<MediaPlayer> (); // Triggered when media is resumed form a stalled state (eg. when lost connection is re-established)
        private UnityEvent<MediaPlayer> OnResolutionChanged = new UnityEvent<MediaPlayer> (); // Triggered when the resolution of the video has changed (including the load) Useful for adaptive streams
        private UnityEvent<MediaPlayer> OnStartedSeeking = new UnityEvent<MediaPlayer> (); // Triggered when seeking begins
        private UnityEvent<MediaPlayer> OnFinishedSeeking = new UnityEvent<MediaPlayer> (); // Triggered when seeking has finished
        private UnityEvent<MediaPlayer> OnStartedBuffering = new UnityEvent<MediaPlayer> (); // Triggered when buffering begins
        private UnityEvent<MediaPlayer> OnFinishedBuffering = new UnityEvent<MediaPlayer> (); // Triggered when buffering has finished
        private UnityEvent<MediaPlayer> OnPropertiesChanged = new UnityEvent<MediaPlayer> (); // Triggered when any properties (eg stereo packing are changed) - this has to be triggered manually
        private UnityEvent<MediaPlayer> OnPlaylistItemChanged = new UnityEvent<MediaPlayer> (); // Triggered when the new item is played in the playlist
        private UnityEvent<MediaPlayer> OnPlaylistFinished = new UnityEvent<MediaPlayer> (); // Triggered when the playlist reaches the end
        private UnityEvent<MediaPlayer> OnTextTracksChanged = new UnityEvent<MediaPlayer> (); // Triggered when the text tracks are added or removed
        #endregion
        private MediaPlayer mediaPlayer;
        public MediaPlayer MediaPlayer {
            get {
                if (mediaPlayer == null) {
                    mediaPlayer = this.GetComponent<MediaPlayer> ();
                    registerAllEvents ();
                }
                return mediaPlayer;
            }
        }

        private List<IDisposable> playHandlers = new List<IDisposable> ();
        /// <summary>
        /// 播放指定地址的视频  可为网络地址 或者本地地址
        /// </summary>
        /// <param name="InUrl">视频地址</param>
        /// <param name="OnFinished">播放到结尾回调</param>
        /// <param name="bLoop">是否循环</param>
        /// <param name="StartTime">开始时间</param>
        /// <param name="EndTime">结束时间</param>
        public void Play (string InUrl, Action<UAVProPlayer> OnFinished, bool bLoop, double StartTime = 0, double EndTime = 0) {
            disposeHandlers (playHandlers);

            if (MediaPlayer.MediaPath.Path != InUrl || MediaPlayer.Control == null)
                MediaPlayer.OpenMedia (MediaPathType.AbsolutePathOrURL, InUrl, false);

            MediaPlayer.Control.Seek (StartTime);
            MediaPlayer.Loop = bLoop;

            playHandlers.Add (OnFinishedSeekingAsObservable ().Subscribe (_ => {
                MediaPlayer.Play ();
                Debug.Log ("Seek Completed");
                bool _bFinished = false;
                var _duration = mediaPlayer.Info.GetDuration ();
                EndTime = EndTime == 0 ? _duration : EndTime;

                var _startTime = Mathf.Clamp ((float) StartTime, 0, (float) _duration);
                var _endTime = Mathf.Clamp ((float) EndTime, _startTime, (float) _duration);

                // 播放结束回调
                Action _onFinished = () => {
                    if (_bFinished) return;

                    _bFinished = true;
                    if (OnFinished != null) OnFinished (this);
                    MediaPlayer.Pause ();

                    if (bLoop) {
                        MediaPlayer.Control.Seek (StartTime);
                    } else {
                        disposeHandlers (playHandlers);
                    }
                };

                // 正常播放时间大于指定结束时间
                playHandlers.Add (Observable.EveryUpdate ()
                    .Where (_1 => MediaPlayer.Control.GetCurrentTime () >= EndTime)
                    .First ()
                    .Subscribe (_1 => {
                        _onFinished ();
                        //Debug.LogFormat ("UVA: reach end point c1: {0}", MediaPlayer.Control.GetCurrentTime ());
                    }));

                // 视频到达结尾
                playHandlers.Add (OnFinishedPlayingAsObservable ().Subscribe (_1 => {
                    //Debug.LogFormat ("UVA: reach end point  c2: {0}", MediaPlayer.Control.GetCurrentTime ());
                    _onFinished ();
                }));

            }));
        }

        private void disposeHandlers (List<IDisposable> InHandlers) {
            InHandlers.ForEach (_handler => {
                if (_handler != null) {
                    _handler.Dispose ();
                    _handler = null;
                }
            });
            InHandlers.Clear ();
        }
        #region  播放器事件
        public IObservable<MediaPlayer> OnMetaDataReadyAsObservable () {
            return OnMetaDataReady.AsObservable ();
        }
        public IObservable<MediaPlayer> OnReadyToPlayAsObservable () {
            return OnReadyToPlay.AsObservable ();
        }
        public IObservable<MediaPlayer> OnStartedAsObservable () {
            return OnStarted.AsObservable ();
        }
        public IObservable<MediaPlayer> OnFirstFrameReadyAsObservable () {
            return OnFirstFrameReady.AsObservable ();
        }
        public IObservable<MediaPlayer> OnFinishedPlayingAsObservable () {
            return OnFinishedPlaying.AsObservable ();
        }
        public IObservable<MediaPlayer> OnClosingAsObservable () {
            return OnClosing.AsObservable ();
        }
        public IObservable<MediaPlayer> OnErrorAsObservable () {
            return OnError.AsObservable ();
        }
        public IObservable<MediaPlayer> OnSubtitleChangeAsObservable () {
            return OnSubtitleChange.AsObservable ();
        }
        public IObservable<MediaPlayer> OnStalledAsObservable () {
            return OnStalled.AsObservable ();
        }
        public IObservable<MediaPlayer> OnUnstalledAsObservable () {
            return OnUnstalled.AsObservable ();
        }
        public IObservable<MediaPlayer> OnResolutionChangedAsObservable () {
            return OnResolutionChanged.AsObservable ();
        }
        public IObservable<MediaPlayer> OnStartedSeekingAsObservable () {
            return OnStartedSeeking.AsObservable ();
        }
        public IObservable<MediaPlayer> OnFinishedSeekingAsObservable () {
            return OnFinishedSeeking.AsObservable ();
        }
        public IObservable<MediaPlayer> OnStartedBufferingAsObservable () {
            return OnStartedBuffering.AsObservable ();
        }
        public IObservable<MediaPlayer> OnFinishedBufferingAsObservable () {
            return OnFinishedBuffering.AsObservable ();
        }
        public IObservable<MediaPlayer> OnPropertiesChangedAsObservable () {
            return OnPropertiesChanged.AsObservable ();
        }
        public IObservable<MediaPlayer> OnPlaylistItemChangedAsObservable () {
            return OnPlaylistItemChanged.AsObservable ();
        }
        public IObservable<MediaPlayer> OnPlaylistFinishedAsObservable () {
            return OnPlaylistFinished.AsObservable ();
        }
        public IObservable<MediaPlayer> OnTextTracksChangedAsObservable () {
            return OnTextTracksChanged.AsObservable ();
        }
        #endregion

        // 注册所有播放器相关事件
        private void registerAllEvents () {
            MediaPlayer.Events.AddListener ((_media, _type, err) => {
                switch (_type) {
                    case MediaPlayerEvent.EventType.MetaDataReady: // Triggered when meta data(width, duration etc) is available
                        OnMetaDataReady.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.ReadyToPlay: // Triggered when the video is loaded and ready to play
                        OnReadyToPlay.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.Started: // Triggered when the playback starts
                        OnStarted.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.FirstFrameReady: // Triggered when the first frame has been rendered
                        OnFirstFrameReady.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.FinishedPlaying: // Triggered when a non-looping video has finished playing
                        OnFinishedPlaying.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.Closing: // Triggered when the media is closed
                        OnClosing.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.Error: // Triggered when an error occurs
                        OnError.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.SubtitleChange: // Triggered when the subtitles change
                        OnSubtitleChange.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.Stalled: // Triggered when media is stalled (eg. when lost connection to media stream) - Currently only supported on Windows platforms
                        OnStalled.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.Unstalled: // Triggered when media is resumed form a stalled state (eg. when lost connection is re-established)
                        OnUnstalled.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.ResolutionChanged: // Triggered when the resolution of the video has changed (including the load) Useful for adaptive streams
                        OnResolutionChanged.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.StartedSeeking: // Triggered when seeking begins
                        OnStartedSeeking.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.FinishedSeeking: // Triggered when seeking has finished
                        OnFinishedSeeking.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.StartedBuffering: // Triggered when buffering begins
                        OnStartedBuffering.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.FinishedBuffering: // Triggered when buffering has finished
                        OnFinishedBuffering.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.PropertiesChanged: // Triggered when any properties (eg stereo packing are changed) - this has to be triggered manually
                        OnPropertiesChanged.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.PlaylistItemChanged: // Triggered when the new item is played in the playlist
                        OnPlaylistItemChanged.Invoke (_media);
                        break;
                    case MediaPlayerEvent.EventType.PlaylistFinished: // Triggered when the playlist reaches the end
                        OnPlaylistFinished.Invoke (_media);
                        break;
                }
            });
        }

    }

}