using System;
using System.Diagnostics;
using UniRx;
using UnityEngine;

namespace UNIHper
{
    public static class AppUtility
    {
        /// <summary>
        /// 创建新的RenderTexture
        /// </summary>
        /// <param name="InWidth"></param>
        /// <param name="InHeight"></param>
        /// <param name="InDepth"></param>
        /// <param name="InFormat"></param>
        /// <returns></returns>
        public static RenderTexture NewRenderTexture(
            int InWidth,
            int InHeight,
            int InDepth = 0,
            RenderTextureFormat InFormat = RenderTextureFormat.ARGB32
        )
        {
            var renderTexture = new RenderTexture(InWidth, InHeight, InDepth, InFormat);
            return renderTexture;
        }
    }
}
