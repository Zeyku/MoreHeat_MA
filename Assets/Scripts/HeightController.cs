using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightController : MonoBehaviour
{

    public GameObject table;


    public void MoveUp(){
        
        Vector3 currentPosition = table.transform.position;
        float newY = Mathf.Lerp(currentPosition.y,currentPosition.y+0.1f,Time.deltaTime*5.0f);
        table.transform.position = new Vector3(currentPosition.x, newY, currentPosition.z); 
    }
    public void MoveDown(){
        
        Vector3 currentPosition = table.transform.position;
        float newY = Mathf.Lerp(currentPosition.y,currentPosition.y-0.1f,Time.deltaTime*5.0f);
        table.transform.position = new Vector3(currentPosition.x, newY, currentPosition.z); 
    }
}
