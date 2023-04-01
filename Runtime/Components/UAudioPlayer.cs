using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UAudioPlayer : MonoBehaviour
{
    public AudioSource GetAudioSource(int audioID)
    {
        var _audioSources = GetComponents<AudioSource>();
        for (var _idx = _audioSources.Length; _idx <= audioID; ++_idx)
        {
            gameObject.AddComponent<AudioSource>();
        }
        return GetComponents<AudioSource>()[audioID];
    }

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }
}
