using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ControlsScaler : MonoBehaviour
{
    [SerializeField, Range(0.5f, 4f)]
    public float scale = 1.0f;

    public void RefreshScaler()
    {
        float _delta = scale - 1;
        var _playerWidth = transform.parent.GetComponent<RectTransform>().rect.width;
        var _offsetMinX = _playerWidth * _delta * 0.5f;

        var _rectTransform = GetComponent<RectTransform>();
        transform.localScale = Vector3.one * scale;

        _offsetMinX /= scale;
        var _offsetMaxX = -_offsetMinX;

        _rectTransform.offsetMin = new Vector2(_offsetMinX, _rectTransform.offsetMin.y);
        _rectTransform.offsetMax = new Vector2(_offsetMaxX, _rectTransform.offsetMax.y);
    }

    void Reset()
    {
        RefreshScaler();
    }

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        RefreshScaler();
#endif
    }
}
