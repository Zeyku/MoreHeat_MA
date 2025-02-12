using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StudyManager : MonoBehaviour
{

    public TextAsset latinSquareCSV;
    public int playerID;

    public float playerHeight;

    public enum Gender{
        male,
        female
    }

    [SerializeField] Gender gender;

    // Start is called before the first frame update
    void Start()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("playerID", playerID);
        PlayerPrefs.SetInt("sceneCounter", 1);
        PlayerPrefs.SetString("gender",gender.ToString());
        PlayerPrefs.SetFloat("playerHeight", playerHeight);

        loadLatinSquare();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void loadLatinSquare()
    {
        string[] lines = latinSquareCSV.text.Split('\n');
        foreach (string line in lines) {

            string[] values = line.Split(";");
            
            if (values[0] == "ID") {
                continue;
            }

            int listID = int.Parse(values[0]);

            if (listID == playerID) {
                Debug.Log("Set Latin Square Line: " + line);

                PlayerPrefs.SetInt("s1", int.Parse(values[1]));
                PlayerPrefs.SetInt("s2", int.Parse(values[2]));
                PlayerPrefs.SetInt("s3", int.Parse(values[3]));
                PlayerPrefs.SetInt("s4", int.Parse(values[4]));

                Debug.Log(PlayerPrefs.GetInt("s1"));
                Debug.Log(PlayerPrefs.GetInt("s2"));
                Debug.Log(PlayerPrefs.GetInt("s3"));
                Debug.Log(PlayerPrefs.GetInt("s4"));
                break;
            }
        }
    }

    //called on button press in UI (startscene)
    public void LoadFirstCondition()
    {
        int sceneToLoad = PlayerPrefs.GetInt("s1");
        SceneManager.LoadScene(sceneToLoad);

    }
}
