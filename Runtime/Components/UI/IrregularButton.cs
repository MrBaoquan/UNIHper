using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 不规则按钮
/// 必要条件:
/// 1. 图片类型设置为Sprite(2D and UI)
/// 2. MeshType = Full Rect
/// 3. Read/Write Enabled = true
/// </summary>
namespace UNIHper
{
    [RequireComponent(typeof(Image))]
    public class IrregularButton : MonoBehaviour
    {
        public float alphaHitTestMinimumThreshold = 0.1f;

        // Start is called before the first frame update
        void Start()
        {
            this.Get<Image>().alphaHitTestMinimumThreshold = alphaHitTestMinimumThreshold;
        }
    }
}
