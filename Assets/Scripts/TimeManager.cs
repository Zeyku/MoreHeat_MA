using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class TimeManager : MonoBehaviour
{
    public TextMeshProUGUI timer;
    private float elapsedTime = 0.0f;


    // Start is called before the first frame update
    void Start()
    {
        timer = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        elapsedTime += Time.deltaTime;

        int hours = Mathf.FloorToInt(elapsedTime / 3600);
        int minutes = Mathf.FloorToInt(elapsedTime % 3600 / 60); // (elapsedTime % 3600)?
        int seconds = Mathf.FloorToInt(elapsedTime % 60);

        timer.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";

    }
}
