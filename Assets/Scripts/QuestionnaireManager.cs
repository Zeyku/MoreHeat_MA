using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public class QuestionnaireManager : MonoBehaviour
{
    //writers for different questionnaires
    private TextWriter writerThermalSensation;
    private TextWriter writerThermalComfort;
    private TextWriter writerIPQ;
    private TextWriter writerBRQ;

    //start and end timestamps of the condition
    private TextWriter writerStartEndTimestamps;
    private string startEndTimestampsFilePath;
    private DateTime[] startEndTimestamps = new DateTime[2];

    //scene counter and player stuff
    private int sceneCounter;
    private int playerID;
    private int conditionIndex;
    private string gender;
    

    //finish screen
    public GameObject finishScreen;

    //avatars to set active by gender
    public GameObject maleAvatar;
    public GameObject femaleAvatar;

    //thermal sensation questionnaire
    [Header("Thermal Sensation Questionnaire")]
    public Slider slider;
    public GameObject thermalSensationUI;
    private string thermalSensationQuestionnaireFilePath;
    private float[] thermalSensationValues = new float[3];
    private DateTime[] thermalSensationTimeStamps = new DateTime[3];
    private int thermalSensationCounter = 0;
    private bool isThermalSensationDone = false;
    

    //thermal comfort questionnaire
    [Header("Thermal Comfort Questionnaire")]
    public GameObject thermalComfortUI;
    private string thermalComfortQuestionnaireFilePath;

    private bool isThermalComfortDone = false;
    public ToggleGroup thermalComfortToggleGroup;

    //IPQ questionnaire
    [Header("IPQ Questionnaire")]
    public GameObject ipqUI;
    public ToggleGroup ipqToggleGroup;
    public TextMeshProUGUI ipqQuestionText;
    public TextMeshPro ipqLeftTextAnchor;
    public TextMeshPro ipqRightTextAnchor;
    private string ipqQuestionnaireFilePath;

    private Q_Question[] ipq_questions;
    private string[] ipqItemCodes;
    private string[] ipq_answers;
    private DateTime[] ipqTimeStamps;
    public TextAsset ipqCSV;
    
    private int ipqCurrentQuestion = 0;
    private bool isIPQDone = false;

    //BRQ questionnaire
    [Header("BRQ Questionnaire")]
    public GameObject brqUI;
    public ToggleGroup brqToggleGroup;

    public TextMeshProUGUI brqQuestionText;
    public TextMeshPro brqLeftTextAnchor;
    public TextMeshPro brqRightTextAnchor;
    private string brqQuestionnaireFilePath;

    private Q_Question[] brq_questions;
    private string[] brqItemCodes;
    private string[] brq_answers;
    private DateTime[] brqTimeStamps;
    public TextAsset brqCSV;
    
    private int brqCurrentQuestion = 0;
    private bool isBRQDone = false;

    public delegate void QuestionnaireDone();
    public static event QuestionnaireDone OnQuestionnaireDone;

    // Start is called before the first frame update
    void Start()
    {
        sceneCounter = PlayerPrefs.GetInt("sceneCounter");
        Debug.Log("Scene Counter on START(): " + sceneCounter);
        playerID = PlayerPrefs.GetInt("playerID");
        conditionIndex = PlayerPrefs.GetInt("s"+sceneCounter);
        gender = PlayerPrefs.GetString("gender").ToLower();

        
        startEndTimestamps[0] = DateTime.Now;

        setCorrectAvatar(gender);

        string directoryPath =  Application.dataPath + "/CSV-Data/"+"playerID_" + playerID + "/"+"counter_"+sceneCounter+"condition_"+ conditionIndex;
        if(!Directory.Exists(directoryPath)){
            Debug.Log("Directory does not exist, creating directory");
            Directory.CreateDirectory(directoryPath);
        }
        string thermalSensationFileName = "thermalSensation.csv";
        thermalSensationQuestionnaireFilePath = Path.Combine(directoryPath, thermalSensationFileName);

        string thermalComfortFileName = "thermalComfort.csv";
        thermalComfortQuestionnaireFilePath = Path.Combine(directoryPath, thermalComfortFileName);

        string ipqFileName = "ipq.csv";
        ipqQuestionnaireFilePath = Path.Combine(directoryPath, ipqFileName);

        string brqFileName = "brq.csv";
        brqQuestionnaireFilePath = Path.Combine(directoryPath, brqFileName);

        string startEndTimestampsFileName = "startEndTimestamps.csv";
        startEndTimestampsFilePath = Path.Combine(directoryPath, startEndTimestampsFileName);

        string cubeGameFileName = "cubeGame.csv";
        string cubeGameFilePath = Path.Combine(directoryPath, cubeGameFileName);

        loadIPQQuestionsFromFile();
        setIPQQuestion(); //set first ipq question
        
        loadBRQQuestionsFromFile();
        setBRQQuestion(); //set first brq question


        //30 seconds acclimatization period
        Invoke("showThermalSensationQuestionnaire", 30f); //30+90 seconds after start
        Invoke("showThermalSensationQuestionnaire", 60f);  //30+180 seconds after start
        Invoke("showThermalSensationQuestionnaire", 90f);  //30+270 seconds after start
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable(){
    }

    void OnDisable(){
    }

    private void setCorrectAvatar(string gender){
        if(gender == "male"){
            maleAvatar.SetActive(true);
        }
        if(gender == "female"){
            femaleAvatar.SetActive(true);
        }
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

    private void loadBRQQuestionsFromFile(){
        string[] lines = brqCSV.text.Split('\n');

        brq_questions = new Q_Question[lines.Length];
        brqItemCodes = new string[lines.Length];
        brq_answers = new string[lines.Length];
        brqTimeStamps = new DateTime[lines.Length];

        for(int i = 0; i < lines.Length; i++){
            string[] values = lines[i].Split(';');
            
            if(values.Length == 4){
                brqItemCodes[i] = values[0];
                brq_questions[i] = new Q_Question(values[1], values[2], values[3]);
            }
        }
    }

    private void setBRQQuestion(){
        brqQuestionText.text = brq_questions[brqCurrentQuestion].get_question();
        brqLeftTextAnchor.text = brq_questions[brqCurrentQuestion].get_negative_anchor();
        brqRightTextAnchor.text = brq_questions[brqCurrentQuestion].get_positive_anchor();
    }

    private void resetBRQToggles(){
        Toggle[] toggles = brqToggleGroup.GetComponentsInChildren<Toggle>();

        foreach(Toggle toggle in toggles){
            toggle.isOn = false;
        }
    }
    
    public void confirmBRQInput(){
        //gets called on button press in canvas
        Toggle toggle = brqToggleGroup.ActiveToggles().FirstOrDefault(); 

        if(toggle != null){
            brq_answers[brqCurrentQuestion] = toggle.name;
            brqTimeStamps[brqCurrentQuestion] = DateTime.Now;
            Debug.Log("BRQ Answer: " + toggle.name);
            
            if(brqCurrentQuestion < brq_questions.Length - 1){
                brqCurrentQuestion++;
                resetBRQToggles();
                setBRQQuestion();
            }else{
                writeBRQDataToCSV();
                brqUI.SetActive(false);
                isBRQDone = true;
                //invoke event for saving cube game data (received in CubeGameManager)
                OnQuestionnaireDone?.Invoke();

                //save end timestamp and write to csv
                startEndTimestamps[1] = DateTime.Now;
                writeStartEndTimestamps();
                
                //setup next scene from latin square by scene-code

                if(sceneCounter < 4){
                    Debug.Log("Scene Counter kleiner 4: " + sceneCounter);
                    sceneCounter+=1;
                    Debug.Log("Scene Counter after +1: " + sceneCounter);
                    PlayerPrefs.SetInt("sceneCounter", sceneCounter);
                    Debug.Log("Check if settingInt worked: " + PlayerPrefs.GetInt("sceneCounter"));
                    string sceneCode = "s" + sceneCounter;
                    Debug.Log("Scene Code: " + sceneCode);
                    int sceneToLoad = PlayerPrefs.GetInt(sceneCode);
                    Debug.Log("Scene to load: " + sceneToLoad);
                    SceneManager.LoadScene(sceneToLoad);
                }else{
                    finishScreen.SetActive(true);
                }
                
            }
        }
        
    }

    private void writeBRQDataToCSV(){
        writerBRQ = new StreamWriter(brqQuestionnaireFilePath, true);

        string header = string.Join(";",brqItemCodes);
        writerBRQ.WriteLine(header);

        string answers = string.Join(";",brq_answers);
        writerBRQ.WriteLine(answers);

        string timestamps = string.Join(";",brqTimeStamps);
        writerBRQ.WriteLine(timestamps);

        writerBRQ.Close();
        Debug.Log("BRQ data written to CSV");
    }

    private void loadIPQQuestionsFromFile(){
        string[] lines = ipqCSV.text.Split('\n');

        ipq_questions = new Q_Question[lines.Length];
        ipqItemCodes = new string[lines.Length];
        ipq_answers = new string[lines.Length];
        ipqTimeStamps = new DateTime[lines.Length];

        for(int i = 0; i < lines.Length; i++){
            string[] values = lines[i].Split(';');
            
            if(values.Length == 4){
                ipqItemCodes[i] = values[0];
                ipq_questions[i] = new Q_Question(values[1], values[2], values[3]);
            }
        }
    }

    private void setIPQQuestion(){
        ipqQuestionText.text = ipq_questions[ipqCurrentQuestion].get_question();
        ipqLeftTextAnchor.text = ipq_questions[ipqCurrentQuestion].get_negative_anchor();
        ipqRightTextAnchor.text = ipq_questions[ipqCurrentQuestion].get_positive_anchor();
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
            ipq_answers[ipqCurrentQuestion] = toggle.name;
            ipqTimeStamps[ipqCurrentQuestion] = DateTime.Now;
            Debug.Log("IPQ Answer: " + toggle.name);
            
            if(ipqCurrentQuestion < ipq_questions.Length - 1){
                ipqCurrentQuestion++;
                resetIPQToggles();
                setIPQQuestion();
            }else{
                writeIPQDataToCSV();
                ipqUI.SetActive(false);
                isIPQDone = true;
                brqUI.SetActive(true);
            }
        }
        
    }

    private void writeIPQDataToCSV(){
        writerIPQ = new StreamWriter(ipqQuestionnaireFilePath, true);

        string header = string.Join(";",ipqItemCodes);
        writerIPQ.WriteLine(header);

        string answers = string.Join(";",ipq_answers);
        writerIPQ.WriteLine(answers);

        string timestamps = string.Join(";",ipqTimeStamps);
        writerIPQ.WriteLine(timestamps);

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

    private void writeStartEndTimestamps(){
        string header = "start;end";
        writerStartEndTimestamps = new StreamWriter(startEndTimestampsFilePath, true);
        writerStartEndTimestamps.WriteLine(header);
        writerStartEndTimestamps.WriteLine(startEndTimestamps[0] + ";" + startEndTimestamps[1]);
        writerStartEndTimestamps.Close();
        Debug.Log("Start and End timestamps written to CSV");
    }
}

public class Q_Question
{
    private string question;
    private string anchor_negative;
    private string anchor_positive;

    public Q_Question(string question, string anchor_negative, string anchor_positive)
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