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

namespace UNIHper {

    [RequireComponent (typeof (DisplayUGUI))]
    public class MultipleAVProPlayer : MonoBehaviour {

        public double CurrentTime {
            get {
                if (currentPlayer == null) return 0;
                return this.currentPlayer.CurrentTime;
            }
        }

        private Indexer videoIndex = new Indexer (0);
        private List<string> videoPathes = new List<string> ();
        public async Task PrepareVideos (List<string> VideoPathes) {
            videoIndex.SetMax (VideoPathes.Count - 1);
            this.videoPathes = VideoPathes;

            var _defaultPlayer = new GameObject ("mediaPlayer_default");
            var _avproPlayer = _defaultPlayer.AddComponent<UAVProPlayer> ();
            _avproPlayer.MediaPlayer.AutoStart = false;
            _avproPlayer.MediaPlayer.AutoOpen = false;

            var _playerPrefabPool = new PrefabPool (_defaultPlayer.transform);
            _playerPrefabPool.cullDespawned = true;
            _playerPrefabPool.preloadAmount = 3;

            var _mediaPlayerPool = PoolManager.Pools.Create ("UNIPlayerPool");
            _mediaPlayerPool.CreatePrefabPool (_playerPrefabPool);

            await Observable.Zip (
                VideoPathes
                .Select (_path => {
                    var _mediaPlayer = _mediaPlayerPool.Spawn (_defaultPlayer.transform, transform);
                    _mediaPlayer.SetAsLastSibling ();
                    var _avproPlayer = _mediaPlayer.GetComponent<UAVProPlayer> ();
                    var _mediaPathType = MediaPathType.RelativeToStreamingAssetsFolder;
                    if (Path.IsPathRooted (_path)) {
                        _mediaPathType = MediaPathType.AbsolutePathOrURL;
                    }
                    _avproPlayer.MediaPlayer.OpenMedia (_mediaPathType, _path, false);
                    return _avproPlayer.OnMetaDataReadyAsObservable ().First ();
                }));
        }

        public void Play (bool FadeEffect = true) {
            playVideo (FadeEffect);
        }

        public void Pause () {
            if (currentPlayer == null) return;
            currentPlayer.Pause ();
        }

        public bool IsPaused {
            get {
                if (currentPlayer == null) return false;
                return currentPlayer.IsPaused;
            }
        }

        public void Play (string Path, Action<UAVProPlayer> OnCompleted, bool Loop = false, double StartTime = 0f, double EndTime = 0f) {
            var _idx = videoPathes.FindIndex (_path => _path == Path);
            if (_idx == -1) return;
            stopVideo ();
            videoIndex.Set (_idx);

            currentPlayer.Play (Path, OnCompleted, Loop, StartTime, EndTime);
            DisplayUGUI.CurrentMediaPlayer = currentPlayer.GetComponent<MediaPlayer> ();
            playFadeEffect ();
        }

        public bool FadePlay { get; set; } = true;

        public void Stop () {
            stopVideo ();
        }

        public void PlayNext () {
            stopVideo ();
            videoIndex.Next ();
            playVideo ();
        }

        public void PlayPrev () {
            stopVideo ();
            videoIndex.Prev ();
            playVideo ();
        }

        private UAVProPlayer currentPlayer { get => transform.GetChild (videoIndex.Current).GetComponent<UAVProPlayer> (); }
        private DisplayUGUI displayUGUI = null;
        public DisplayUGUI DisplayUGUI {
            get {
                if (displayUGUI == null) {
                    displayUGUI = this.Get<DisplayUGUI> ();
                }
                return displayUGUI;
            }
        }

        private void playVideo (bool FadeEffect = true) {
            var _mediaPlayer = currentPlayer;
            DisplayUGUI.CurrentMediaPlayer = _mediaPlayer.GetComponent<MediaPlayer> ();
            _mediaPlayer.Play ();
            if (FadeEffect) playFadeEffect ();
        }

        private void playFadeEffect () {
            if (!FadePlay) return;
            DOTween.Sequence ()
                .Append (DisplayUGUI.DOColor (Color.clear, 0.15f))
                .Append (DisplayUGUI.DOColor (Color.white, 0.45f))
                .PlayForward ();
        }

        private void stopVideo () {
            currentPlayer.Rewind (true);
        }

        // Start is called before the first frame update
        void Start () {

        }

        // Update is called once per frame
        void Update () {

        }
    }
}