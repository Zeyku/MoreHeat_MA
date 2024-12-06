using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class QuestionnaireManager : MonoBehaviour
{

    private string questionnaireFilePath = Application.dataPath + "/CSV-Data/thermalPerception.csv";
    private TextWriter writer;
    private float[] thermalSensationValues = new float[3];
    private DateTime[] thermalSensationTimeStamps = new DateTime[3];
    private int questionnaireCounter = 0;
    private bool isQuestionnaireDone = false;

    public Slider slider;
    public GameObject thermalSensationUI;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable(){
        TimeManager.OnNinetySecondsPassed += showQuestionnaire;
    }

    void OnDisable(){
        TimeManager.OnNinetySecondsPassed -= showQuestionnaire;
    }

    private void showQuestionnaire(){
        if(!isQuestionnaireDone){
            slider.value = 50;
            thermalSensationUI.SetActive(true); 
        }else{
            Debug.Log("Questionnaire already done");
        }
        
    }

    private void hideQuestionnaire(){
        thermalSensationUI.SetActive(false);
    }

    public void confirmInput(){
        if(questionnaireCounter <= 2){
            thermalSensationValues[questionnaireCounter] = slider.value;
            thermalSensationTimeStamps[questionnaireCounter] = DateTime.Now;
            questionnaireCounter++;
        }

        if(questionnaireCounter > 2){
            writeDataToCSV();
            isQuestionnaireDone = true;
            
        }
        
        hideQuestionnaire();
    }

    private void writeDataToCSV(){
        string header = "thermal_sensation_90;thermal_sensation_180;thermal_sensation_270;timestamp_90;timestamp_180;timestamp_270";
        writer = new StreamWriter(questionnaireFilePath, true);
        writer.WriteLine(header);

        string answers = string.Join(";",thermalSensationValues);
        string timestamps = string.Join(";",thermalSensationTimeStamps);
        writer.WriteLine(answers + ";" + timestamps);
        writer.Close();
        Debug.Log("Questionnaire data written to CSV");
    }
}
