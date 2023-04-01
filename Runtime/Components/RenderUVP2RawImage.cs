using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    [RequireComponent(typeof(RawImage), typeof(UVPManager))]
    public class RenderUVP2RawImage : MonoBehaviour
    {
        RawImage rawImage = null;

        private void Awake()
        {
            rawImage = this.GetComponent<RawImage>();
        }

        // Start is called before the first frame update
        void Start() { }

        // Update is called once per frame
        void Update() { }

        void OnPlayByUrl(UVideoPlayer InVideoPlayer)
        {
            this.GetComponent<RawImage>().texture = InVideoPlayer.RenderTexture;
        }
    }
}
