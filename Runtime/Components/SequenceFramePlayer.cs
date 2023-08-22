using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UNIHper;
using System.IO;
using UnityEngine.UI;

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
            _sequencePath.Value = sequenceDir;
        }

        private string sequencePath = "";
        private string sequenceDirectory =>
            Path.Combine(Application.streamingAssetsPath, sequencePath);

        private bool sequenceDirectoryExists =>
            Path.IsPathRooted(sequencePath)
                ? Directory.Exists(sequencePath)
                : Directory.Exists(sequenceDirectory);

        private ReactiveProperty<string> _sequencePath = new ReactiveProperty<string>();
        private int fps = 25;
        private float interval => 1f / fps;

        Indexer sequenceIndexer = new Indexer();
        List<Texture2D> sequenceTextures = new();
        private bool bMatchTextureSize = true;
        private bool bLoop = true;

        // Start is called before the first frame update
        void Start()
        {
            _sequencePath
                .Subscribe(async _path =>
                {
                    if (string.IsNullOrEmpty(_path))
                        return;
                    sequencePath = _path;
                    // check if sequence directory exists
                    if (!sequenceDirectoryExists)
                    {
                        Debug.LogError($"Sequence directory not exists: {sequenceDirectory}");
                        return;
                    }

                    // check if sequence directory is empty
                    var _files = Directory.GetFiles(sequenceDirectory);
                    if (_files.Length == 0)
                    {
                        Debug.LogError($"Sequence directory is empty: {sequenceDirectory}");
                        return;
                    }

                    sequenceTextures = (
                        await Managements.Resource.LoadTexture2Ds(sequenceDirectory)
                    )
                        .Where(_ => _ != null)
                        .ToList();
                    sequenceIndexer.Loop = bLoop;
                    sequenceIndexer.SetMax(sequenceTextures.Count - 1);
                })
                .AddTo(this);

            var _render = this.Get<RawImage>();

            float _time = 0f;
            Observable
                .EveryUpdate()
                .Where(_ =>
                {
                    _time += Time.deltaTime;
                    if (_time >= interval)
                    {
                        _time = 0f;
                        return true;
                    }
                    return false;
                })
                .Subscribe(_ =>
                {
                    if (sequenceTextures.Count <= 0)
                        return;
                    _render.texture = sequenceTextures[sequenceIndexer.Next()];
                    if (bMatchTextureSize)
                    {
                        _render.SetNativeSize();
                    }
                })
                .AddTo(this);
        }

        // Update is called once per frame
        void Update() { }
    }
}
