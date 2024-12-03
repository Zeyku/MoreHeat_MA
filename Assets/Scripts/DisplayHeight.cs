using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DisplayHeight : MonoBehaviour
{
    public TextMeshProUGUI heightText;
    public Transform targetTable;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(targetTable != null && heightText != null){
            float currentHeight = targetTable.position.y;
            heightText.text = "Height: " + currentHeight.ToString("F2");
        }
        else{
            Debug.LogWarning("Target table or TextMeshPro component is not assigned.");
        }
    }
}
