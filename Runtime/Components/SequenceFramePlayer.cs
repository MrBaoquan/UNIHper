using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UNIHper;
using System.IO;
using UnityEngine.UI;
using System;

namespace UNIHper
{
    [RequireComponent(typeof(RawImage))]
    public class SequenceFramePlayer : MonoBehaviour
    {
        public void SetSequenceDirectory(
            string sequenceDir,
            bool matchTextureSize = true,
            int fps = 25
        )
        {
            this.fps = fps;
            bMatchTextureSize = matchTextureSize;
            sequencePath.Value = sequenceDir;
        }

        private string sequenceDirectory =>
            Path.Combine(Application.streamingAssetsPath, sequencePath.Value);

        private bool sequenceDirectoryExists =>
            Path.IsPathRooted(sequencePath.Value)
                ? Directory.Exists(sequencePath.Value)
                : Directory.Exists(sequenceDirectory);

        private ReactiveProperty<string> sequencePath = new ReactiveProperty<string>();
        private int fps = 25;
        private float interval => 1f / fps;

        Indexer sequenceIndexer = new Indexer();
        List<Texture2D> sequenceTextures = new();
        private bool bMatchTextureSize = true;

        private ReactiveProperty<bool> Loop = new ReactiveProperty<bool>(true);
        private ReactiveProperty<bool> paused = new ReactiveProperty<bool>(false);

        public void SetTextures(List<Texture2D> textures)
        {
            sequenceTextures = textures.OrderBy(_ => _.name).ToList();
            sequenceIndexer.SetMax(sequenceTextures.Count - 1);
            sequenceIndexer.SetValueAndForceNotify(sequenceIndexer.Current);
        }

        public IObservable<int> OnFrameChangedAsObservable()
        {
            return sequenceIndexer.OnValueChangedAsObservable();
        }

        public IObservable<int> OnReachEndAsObservable()
        {
            return sequenceIndexer.OnValueChangedToMaxAsObservable();
        }

        public void SetLoop(bool bLoop)
        {
            Loop.Value = bLoop;
        }

        public void SetMatchTextureSize(bool bMatch)
        {
            bMatchTextureSize = bMatch;
        }

        public void SeekToFrame(int frame)
        {
            sequenceIndexer.Set(frame);
        }

        public void Play()
        {
            paused.Value = false;
        }

        public void Pause()
        {
            paused.Value = true;
        }

        public void Stop()
        {
            sequenceIndexer.Set(0);
            Pause();
        }

        public void Rewind()
        {
            sequenceIndexer.Set(0);
            Play();
        }

        // Start is called before the first frame update
        void Start()
        {
            sequencePath
                .Subscribe(async _path =>
                {
                    if (string.IsNullOrEmpty(_path))
                        return;
                    // sequencePath = _path;

                    if (!sequenceDirectoryExists)
                    {
                        Debug.LogError($"Sequence directory not exists: {sequenceDirectory}");
                        return;
                    }

                    var _files = Directory.GetFiles(sequenceDirectory);
                    if (_files.Length == 0)
                    {
                        Debug.LogError($"Sequence directory is empty: {sequenceDirectory}");
                        return;
                    }

                    var _textures = (await Managements.Resource.LoadTexture2Ds(sequenceDirectory))
                        .Where(_ => _ != null)
                        .ToList();
                    SetTextures(_textures);
                })
                .AddTo(this);

            var _render = this.Get<RawImage>();

            Loop.Subscribe(_ =>
                {
                    sequenceIndexer.Loop = _;
                })
                .AddTo(this);

            sequenceIndexer
                .OnValueChangedAsObservable()
                .Subscribe(frame =>
                {
                    _render.texture = sequenceTextures[frame];
                    if (bMatchTextureSize)
                    {
                        _render.SetNativeSize();
                    }
                });

            Observable
                .EveryUpdate()
                .ThrottleFirst(TimeSpan.FromSeconds(interval))
                .Subscribe(_ =>
                {
                    if (sequenceTextures.Count <= 0 || paused.Value)
                        return;
                    sequenceIndexer.Next();
                })
                .AddTo(this);
        }
    }
}
