using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineController : MonoBehaviour
{

    public Outline _outline;
    // Start is called before the first frame update
    void OnEnable(){
        if(_outline==null)
            _outline = GetComponent<Outline>();
    }

    public void EnterHover(){
        _outline.enabled = true;
    }

    public void ExitHover(){
        _outline.enabled = false;
    }
}
