using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace UNIHper
{
    [RequireComponent(typeof(Graphic))]
    public sealed class InverseMask : MonoBehaviour, IMaterialModifier
    {
        private static readonly int _stencilComp = Shader.PropertyToID("_StencilComp");

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            var resultMaterial = new Material(baseMaterial);
            resultMaterial.SetFloat(_stencilComp, Convert.ToSingle(CompareFunction.NotEqual));
            return resultMaterial;
        }
    }
}