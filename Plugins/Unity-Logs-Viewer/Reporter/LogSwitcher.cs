using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogSwitcher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Reporter>().enabled = !this.GetComponent<Reporter>().enabled;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.LeftShift)&&Input.GetKey(KeyCode.LeftControl)&&Input.GetKeyDown(KeyCode.D)){
            this.GetComponent<Reporter>().enabled = !this.GetComponent<Reporter>().enabled;
        }
    }
}
