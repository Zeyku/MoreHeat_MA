using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Linq;

public class QuestionnaireManager : MonoBehaviour
{
    
    private TextWriter writerThermalSensation;
    private TextWriter writerThermalComfort;

    //thermal perception questionnaire
    private string thermalSensationQuestionnaireFilePath = Application.dataPath + "/CSV-Data/thermalSensation.csv";
    private float[] thermalSensationValues = new float[3];
    private DateTime[] thermalSensationTimeStamps = new DateTime[3];
    private int thermalSensationCounter = 0;
    private bool isThermalSensationDone = false;
    public Slider slider;
    public GameObject thermalSensationUI;

    //thermal comfort questionnaire
    public GameObject thermalComfortUI;
    private string thermalComfortQuestionnaireFilePath = Application.dataPath + "/CSV-Data/thermalComfort.csv";

    private bool isThermalComfortDone = false;
    public ToggleGroup thermalComfortToggleGroup;

    //IPQ questionnaire
    //BRQ questionnaire

    // Start is called before the first frame update
    void Start()
    {
        //30 seconds acclimatization period
        Invoke("showThermalSensationQuestionnaire", 10f); //30+90 seconds after start
        Invoke("showThermalSensationQuestionnaire", 20f);  //30+180 seconds after start
        Invoke("showThermalSensationQuestionnaire", 30f);  //30+270 seconds after start
        
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

    public void confirmThermalSensationInput(){
        //gets called on button press in canvas 
        if(thermalSensationCounter <= 2){
            thermalSensationValues[thermalSensationCounter] = slider.value;
            thermalSensationTimeStamps[thermalSensationCounter] = DateTime.Now;
            thermalSensationCounter++;
        }

        if(thermalSensationCounter > 2){
            //thermal sensation questionnaire done
            writeThermalSensationDataToCSV();
            isThermalSensationDone = true;
            thermalComfortUI.SetActive(true);
        }
        
        hideThermalSensationQuestionnaire();   
    }

    public void confirmThermalComfortInput(){
        //gets called on button press in canvas
        Toggle toggle = thermalComfortToggleGroup.ActiveToggles().FirstOrDefault(); 
        if(toggle != null){
            string answer = toggle.name;
            Debug.Log("Comfort Answer: " + answer);
            //conditionally set active oben bei invoke
            writeThermalComfortDataToCSV(answer);
            thermalComfortUI.SetActive(false);
            isThermalComfortDone = true;
        } 
    }

    private void writeThermalComfortDataToCSV(string answer){
        string header = "thermal_comfort;timestamp";
        writerThermalComfort = new StreamWriter(thermalComfortQuestionnaireFilePath, true);
        writerThermalComfort.WriteLine(header);

        writerThermalComfort.WriteLine(answer + ";" + DateTime.Now);
        writerThermalComfort.Close();
        Debug.Log("Thermal Comfort data written to CSV");
    }
    private void writeThermalSensationDataToCSV(){
        string header = "thermal_sensation_90;thermal_sensation_180;thermal_sensation_270;timestamp_90;timestamp_180;timestamp_270";
        writerThermalSensation = new StreamWriter(thermalSensationQuestionnaireFilePath, true);
        writerThermalSensation.WriteLine(header);

        string answers = string.Join(";",thermalSensationValues);
        string timestamps = string.Join(";",thermalSensationTimeStamps);
        writerThermalSensation.WriteLine(answers + ";" + timestamps);
        writerThermalSensation.Close();
        Debug.Log("Thermal Sensation data written to CSV");
    }
}
