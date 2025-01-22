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
    private TextWriter writerIPQ;

    //thermal perception questionnaire
    [Header("Thermal Sensation Questionnaire")]
    private string thermalSensationQuestionnaireFilePath = Application.dataPath + "/CSV-Data/thermalSensation.csv";
    private float[] thermalSensationValues = new float[3];
    private DateTime[] thermalSensationTimeStamps = new DateTime[3];
    private int thermalSensationCounter = 0;
    private bool isThermalSensationDone = false;
    public Slider slider;
    public GameObject thermalSensationUI;

    //thermal comfort questionnaire
    [Header("Thermal Comfort Questionnaire")]
    public GameObject thermalComfortUI;
    private string thermalComfortQuestionnaireFilePath = Application.dataPath + "/CSV-Data/thermalComfort.csv";

    private bool isThermalComfortDone = false;
    public ToggleGroup thermalComfortToggleGroup;

    //IPQ questionnaire
    [Header("IPQ Questionnaire")]
    public GameObject ipqUI;
    public ToggleGroup ipqToggleGroup;

    public TextMeshProUGUI ipqQuestionText;
    public TextMeshPro leftTextAnchor;
    public TextMeshPro rightTextAnchor;
    private string ipqQuestionnaireFilePath = Application.dataPath + "/CSV-Data/ipq.csv";

    private IPQ_Question[] ipq_questions;
    private string[] ipqItemCodes;
    private string[] ipq_answers;
    public TextAsset ipqCSV;
    
    private int currentQuestion = 0;
    private bool isIPQDone = false;

    //BRQ questionnaire
    [Header("BRQ Questionnaire")]
    public GameObject brqUI;
    public ToggleGroup brqToggleGroup;

    


    // Start is called before the first frame update
    void Start()
    {
        loadIPQQuestionsFromFile();
        setIPQQuestion(); //set first question
        
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
            
            writeThermalComfortDataToCSV(answer);
            thermalComfortUI.SetActive(false);
            isThermalComfortDone = true;
            ipqUI.SetActive(true);
        } 
    }

    private void loadIPQQuestionsFromFile(){
        string[] lines = ipqCSV.text.Split('\n');

        ipq_questions = new IPQ_Question[lines.Length];
        ipqItemCodes = new string[lines.Length];
        ipq_answers = new string[lines.Length];

        for(int i = 0; i < lines.Length; i++){
            string[] values = lines[i].Split(';');
            
            if(values.Length == 4){
                ipqItemCodes[i] = values[0];
                ipq_questions[i] = new IPQ_Question(values[1], values[2], values[3]);
            }
        }
    }

    private void setIPQQuestion(){
        ipqQuestionText.text = ipq_questions[currentQuestion].get_question();
        leftTextAnchor.text = ipq_questions[currentQuestion].get_negative_anchor();
        rightTextAnchor.text = ipq_questions[currentQuestion].get_positive_anchor();
    }

    private void resetIPQToggles(){
        Toggle[] toggles = ipqToggleGroup.GetComponentsInChildren<Toggle>();

        foreach(Toggle toggle in toggles){
            toggle.isOn = false;
        }
    }

    public void confirmIPQInput(){
        //gets called on button press in canvas
        Toggle toggle = ipqToggleGroup.ActiveToggles().FirstOrDefault(); 

        if(toggle != null){
            ipq_answers[currentQuestion] = toggle.name;
            Debug.Log("IPQ Answer: " + toggle.name);
            
            if(currentQuestion < ipq_questions.Length - 1){
                currentQuestion++;
                resetIPQToggles();
                setIPQQuestion();
            }else{
                writeIPQDataToCSV();
                ipqUI.SetActive(false);
                isIPQDone = true;
            }
        }
        
    }

    private void writeIPQDataToCSV(){
        writerIPQ = new StreamWriter(ipqQuestionnaireFilePath, true);
        string header = string.Join(";",ipqItemCodes);
        writerIPQ.WriteLine(header);
        string answers = string.Join(";",ipq_answers);
        writerIPQ.WriteLine(answers);
        writerIPQ.Close();
        Debug.Log("IPQ data written to CSV");
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

public class IPQ_Question
{
    private string question;
    private string anchor_negative;
    private string anchor_positive;

    public IPQ_Question(string question, string anchor_negative, string anchor_positive)
    {
        this.question = question;
        this.anchor_negative = anchor_negative;
        this.anchor_positive = anchor_positive;
    }

    public string get_question()
    {
        return question;
    }

    public string get_positive_anchor()
    {
        return anchor_positive;
    }

    public string get_negative_anchor()
    {
        return anchor_negative;
    }
}