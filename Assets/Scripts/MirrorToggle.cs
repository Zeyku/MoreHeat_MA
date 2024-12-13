using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MirrorToggle : MonoBehaviour
{
    public GameObject mirror;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleMirror(){
        if(mirror.activeSelf){
            mirror.SetActive(false);
        }else{
            mirror.SetActive(true);
        }
    }
}
