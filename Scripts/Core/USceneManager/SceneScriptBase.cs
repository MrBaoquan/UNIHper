using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UNIHper
{

public abstract class SceneScriptBase
{
    /// <summary>
    /// called once when the scene has been loaded
    /// </summary>
    public virtual void Start(){}

    /// <summary>
    /// called every frame after scene has been loaded
    /// </summary>
    public virtual void Update(){}

    /// <summary>
    /// called when the scene has been unloaded
    /// </summary>
    public virtual void OnDestroy(){}
    
    /// <summary>
    /// called when application quit
    /// </summary>
    public virtual void OnApplicationQuit(){}

}

}
