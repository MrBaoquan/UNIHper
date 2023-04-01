using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace UNIHper
{
    public class UVPManager : MonoBehaviour
    {
        private Dictionary<string, string> rawUrls = new Dictionary<string, string>();
        ReactiveProperty<RenderTexture> renderTexture = new ReactiveProperty<RenderTexture>();
        public RenderTexture RenderTexture
        {
            get { return renderTexture.Value; }
        }
        Dictionary<VideoClip, UVideoPlayer> clipPlayers = new Dictionary<VideoClip, UVideoPlayer>();
        Dictionary<string, UVideoPlayer> urlPlayers = new Dictionary<string, UVideoPlayer>();
        UVideoPlayer currentPlayer = null;

        public void PreparePlayers(string[] InUrls)
        {
            urlPlayers.Values
                .ToList()
                .ForEach(_videoPlayer =>
                {
                    Destroy(_videoPlayer.gameObject);
                });

            rawUrls = InUrls.ToDictionary(
                _path => Path.GetFileNameWithoutExtension(_path),
                _path => _path
            );

            int _width = -1,
                _height = -1;
            var _rectTransform = this.transform as RectTransform;
            if (_rectTransform)
            {
                _width = (int)_rectTransform.rect.width;
                _height = (int)_rectTransform.rect.height;
            }
            InUrls
                .ToList()
                .ForEach(_url =>
                {
                    var _videoName = Path.GetFileNameWithoutExtension(_url);
                    var _newPlayer = new GameObject(_videoName);
                    _newPlayer.transform.parent = this.transform;
                    _newPlayer.AddComponent<RectTransform>();
                    var _urlPlayer = _newPlayer.AddComponent<UVideoPlayer>();
                    _urlPlayer.BuildRender(_width, _height, false);
                    urlPlayers.Add(_videoName, buildUrlPlayer(_urlPlayer, _url));
                });
        }

        private UVideoPlayer buildUrlPlayer(UVideoPlayer InPlayer, string InUrl)
        {
            InPlayer.DisablePlayOnAwake().DisableLoop().Prepare(InUrl);
            return InPlayer;
        }

        public void Play(
            string InUrl,
            VideoPlayer.EventHandler OnReachEndHandler = null,
            int loop = -1,
            float StartTime = 0,
            float InEndTime = 0
        )
        {
            var _videoName = Path.GetFileNameWithoutExtension(InUrl);
            if (!urlPlayers.ContainsKey(_videoName))
            {
                Debug.LogWarning("{0} not exists");
                return;
            }

            if (currentPlayer != null)
            {
                currentPlayer.Stop();
            }
            currentPlayer = urlPlayers[_videoName];
            if (!currentPlayer.isPrepared)
            {
                // TODO 多个视频频繁切换会出现重叠 待优化
                urlPlayers.Values
                    .Where(_vp => _vp != currentPlayer)
                    .ToList()
                    .ForEach(_vp => _vp.Stop());
                currentPlayer.Prepare(_ =>
                {
                    currentPlayer.Play(OnReachEndHandler, loop, StartTime, InEndTime);
                });
            }
            else
            {
                currentPlayer.Play(OnReachEndHandler, loop, StartTime, InEndTime);
            }

            renderTexture.Value = currentPlayer.RenderTexture;

            this.BroadcastMessage(
                "OnPlayByUrl",
                currentPlayer,
                SendMessageOptions.DontRequireReceiver
            );
        }

        public void Pause(bool Reset = false)
        {
            if (currentPlayer == null)
                return;
            if (currentPlayer.isPlaying)
                currentPlayer.Pause();
        }

        public void Stop()
        {
            if (currentPlayer == null)
                return;
            currentPlayer.Stop();
        }

        //public string[] urls = null;
        // Start is called before the first frame update
        void Start()
        {
            var _render = this.GetComponent<RawImage>();
            if (_render)
            {
                renderTexture.Subscribe(_ =>
                {
                    Debug.Log(_);
                    Debug.Log("start set render...");
                    _render.texture = _;
                });
            }

            // Application.targetFrameRate = -1;
            // string _videoPath = Path.Combine(Application.streamingAssetsPath,"Assets/Videos");
            // urls = (new DirectoryInfo(_videoPath)).GetFiles("*.mp4").Select(_=>_.FullName).ToArray();
            // PreparePlayers(urls);
        }

        // Update is called once per frame
        void Update()
        {
            // if(Input.GetKeyDown(KeyCode.T)){
            //     PlayByUrl(urls[3],_=>{
            //         Debug.LogWarning("=============== Play completed ==============");
            //     },1,12,15);
            // }
        }
    }
}
