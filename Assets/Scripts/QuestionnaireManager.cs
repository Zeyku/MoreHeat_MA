using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class QuestionnaireManager : MonoBehaviour
{
    
    private TextWriter writer;

    //thermal perception questionnaire
    private string thermalSensationQuestionnaireFilePath = Application.dataPath + "/CSV-Data/thermalSensation.csv";
    private float[] thermalSensationValues = new float[3];
    private DateTime[] thermalSensationTimeStamps = new DateTime[3];
    private int thermalSensationCounter = 0;
    private bool isThermalSensationDone = false;
    public Slider slider;
    public GameObject thermalSensationUI;



    // Start is called before the first frame update
    void Start()
    {
        Invoke("showThermalSensationQuestionnaire", 120f); //30+90 seconds after start
        Invoke("showThermalSensationQuestionnaire", 210f);  //30+180 seconds after start
        Invoke("showThermalSensationQuestionnaire", 300f);  //30+270 seconds after start
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable(){
    }

    void OnDisable(){
    }

    private void showThermalSensationQuestionnaire(){
        if(!isThermalSensationDone){
            slider.value = 50;
            thermalSensationUI.SetActive(true); 
        }else{
            Debug.Log("Questionnaire already done");
        }
        
    }

    private void hideThermalSensationQuestionnaire(){
        thermalSensationUI.SetActive(false);
    }

    public void confirmInput(){
        //gets called on button press in canvas 
        if(thermalSensationCounter <= 2){
            thermalSensationValues[thermalSensationCounter] = slider.value;
            thermalSensationTimeStamps[thermalSensationCounter] = DateTime.Now;
            thermalSensationCounter++;
        }

        if(thermalSensationCounter > 2){
            writeThermalSensationDataToCSV();
            isThermalSensationDone = true;
            
        }
        
        hideThermalSensationQuestionnaire();
    }

    private void writeThermalSensationDataToCSV(){
        string header = "thermal_sensation_90;thermal_sensation_180;thermal_sensation_270;timestamp_90;timestamp_180;timestamp_270";
        writer = new StreamWriter(thermalSensationQuestionnaireFilePath, true);
        writer.WriteLine(header);

        string answers = string.Join(";",thermalSensationValues);
        string timestamps = string.Join(";",thermalSensationTimeStamps);
        writer.WriteLine(answers + ";" + timestamps);
        writer.Close();
        Debug.Log("Thermal Sensation data written to CSV");
    }
}
