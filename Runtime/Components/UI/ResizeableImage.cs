using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace UNIHper
{
    public class ResizeableImage : BaseMeshEffect
    {
        public Vector3 LeftLower = new Vector3(-100, -100, 0);
        public Vector3 LeftUpper = new Vector3(-100, 100, 0);
        public Vector3 RightUpper = new Vector3(100, 100, 0);
        public Vector3 RightLower = new Vector3(100, -100, 0);

        private Bounds bounds;
        public Bounds Bounds
        {
            get { return bounds; }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Resize2Rect(Vector3 InMin, Vector3 InMax)
        {
            bounds = new Bounds((InMax + InMin) / 2, InMax - InMin);
            //Debug.LogFormat("min:{0}, max:{1}, center:{2}, size:{3}",InMin, InMax, bounds.center, bounds.size);
            LeftUpper.x = InMin.x;
            LeftUpper.y = InMax.y;

            LeftLower = InMin;
            RightUpper = InMax;

            RightLower.x = InMax.x;
            RightLower.y = InMin.y;

            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            List<UIVertex> vertexs = new List<UIVertex>();
            vh.GetUIVertexStream(vertexs);

            var _vertex = vertexs[0];
            _vertex.position = LeftLower;
            vh.SetUIVertex(_vertex, 0);

            _vertex = vertexs[1];
            _vertex.position = LeftUpper;
            vh.SetUIVertex(_vertex, 1);

            _vertex = vertexs[2];
            _vertex.position = RightUpper;
            vh.SetUIVertex(_vertex, 2);

            _vertex = vertexs[4];
            _vertex.position = RightLower;
            vh.SetUIVertex(_vertex, 3);
            vh.GetUIVertexStream(vertexs);
        }
    }
}
