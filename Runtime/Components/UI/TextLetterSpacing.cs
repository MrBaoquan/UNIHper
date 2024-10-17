using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextLetterSpacing : BaseMeshEffect
{
    public float spacing = 0;

    public override void ModifyMesh(VertexHelper vh)
    {
        List<UIVertex> vertexs = new List<UIVertex>();
        vh.GetUIVertexStream(vertexs);
        int vertexIndexCount = vertexs.Count;
        for (int i = 6; i < vertexIndexCount; i++)
        {
            UIVertex v = vertexs[i];
            v.position += new Vector3(spacing * (i / 6), 0, 0);
            vertexs[i] = v;
            if (i % 6 <= 2)
            {
                vh.SetUIVertex(v, (i / 6) * 4 + i % 6);
            }
            if (i % 6 == 4)
            {
                vh.SetUIVertex(v, (i / 6) * 4 + i % 6 - 1);
            }
        }
    }
}
