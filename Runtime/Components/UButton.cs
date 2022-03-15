using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;


namespace UNIHper
{
    

[RequireComponent(typeof(Button))]
public class UButton : MonoBehaviour
{
    public string Title = "确认";

    private void OnValidate() {
        this.Get<Text>("text_title").text = Title;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}




}

