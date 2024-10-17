using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlitCamera : MonoBehaviour
{
    float shadowDistance = 0.0f;
    public Light[] SoftLights;
    public Light[] HardLights;

    // Start is called before the first frame update
    void Start()
    {
        // Shader unlitShader = Shader.Find("Unlit/Texture");
        //  GetComponent<Camera>().SetReplacementShader(unlitShader,"");
    }

    // Update is called once per frame
    void Update() { }

    void OnPreRender()
    {
        shadowDistance = QualitySettings.shadowDistance;
        QualitySettings.shadowDistance = 0;
        foreach (Light l in SoftLights)
        {
            l.shadows = LightShadows.None;
        }
        foreach (Light l in HardLights)
        {
            l.shadows = LightShadows.None;
        }
    }

    void OnPostRender()
    {
        QualitySettings.shadowDistance = shadowDistance;
        foreach (Light l in SoftLights)
        {
            l.shadows = LightShadows.Soft;
        }
        foreach (Light l in HardLights)
        {
            l.shadows = LightShadows.Hard;
        }
    }
}
