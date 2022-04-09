using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RenderHeads.Media.AVProVideo;
using UniRx;
using UnityEngine;
namespace UNIHper {
    [RequireComponent (typeof (DisplayUGUI))]
    public class UAVProMultiplePlayer : MonoBehaviour {

        public void AddVideo (string InVideoPath) {
            if (_playersDict.ContainsKey (InVideoPath)) return;

            var _newMediaPlayer = new GameObject ();
            _newMediaPlayer.transform.parent = this.transform;
            var _uavPlayer = _newMediaPlayer.AddComponent<UAVProPlayer> ();
            var _avPlayer = _newMediaPlayer.GetComponent<MediaPlayer> ();
            _avPlayer.AutoStart = false;
            _avPlayer.AutoOpen = false;
            _playersDict.Add (InVideoPath, _uavPlayer);
        }

        public IObservable<IList<UAVProPlayer>> ApplyVideos () {
            return Observable.Zip (
                _playersDict.Where (_player => !_player.Value.IsMediaOpened)
                .Select (_player => _player.Value.OpenMedia (_player.Key))
            );
        }

        public void Play (int InIndex, Action<UAVProPlayer> OnFinished, bool bLoop, double StartTime = 0, double EndTime = 0) {
            if (_playersDict.Count <= InIndex) return;
            m_mediaDisplayer.CurrentMediaPlayer = _playersDict.ElementAt (InIndex).Value.MediaPlayer;
            _playersDict.ElementAt (InIndex).Value.Play (OnFinished, bLoop, StartTime, EndTime);
            _playersDict.Except (new KeyValuePair<string, UAVProPlayer>[] { _playersDict.ElementAt (InIndex) }).ToList ().ForEach (_player => _player.Value.Stop ());
        }

        private Dictionary<string, UAVProPlayer> _playersDict = new Dictionary<string, UAVProPlayer> ();
        private DisplayUGUI m_mediaDisplayer {
            get => GetComponent<DisplayUGUI> ();
        }
        // Start is called before the first frame update
        void Start () {

        }

        // Update is called once per frame
        void Update () {

        }
    }
}