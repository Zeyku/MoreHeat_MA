using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarSwitcher : MonoBehaviour
{
    public GameObject[] avatars;
    private int currentAvatarIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchAvatar(int index){
        if(index >= 0 && index < avatars.Length){
            avatars[currentAvatarIndex].SetActive(false);
            avatars[index].SetActive(true);
            currentAvatarIndex = index;
        }
    }
}
