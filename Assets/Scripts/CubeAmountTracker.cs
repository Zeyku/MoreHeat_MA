using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CubeAmountTracker : MonoBehaviour
{
    private int cubeAmount;
    private int sortedCubesAmount = 0;

    private void OnEnable() {
        SortHitboxCollider.OnCubeSorted += UpdateSortedCubesAmount;
    }

    private void OnDisable() {
        SortHitboxCollider.OnCubeSorted -= UpdateSortedCubesAmount;
    }


    // Start is called before the first frame update
    void Start()
    {
        cubeAmount = CountCubesWithColor("blue","red","yellow","green");
        GetComponent<TextMeshProUGUI>().text = $"{sortedCubesAmount} / {cubeAmount}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    int CountCubesWithColor(params string[] colors){
        int sum = 0;
        foreach (string color in colors){
            GameObject[] taggedCubes = GameObject.FindGameObjectsWithTag(color);
            sum += taggedCubes.Length;
        }
        return sum;
    }

    private void UpdateSortedCubesAmount(){
        Debug.Log("OnCubeSorted received");
        sortedCubesAmount++;
        GetComponent<TextMeshProUGUI>().text = $"{sortedCubesAmount} / {cubeAmount}";
    }

}
