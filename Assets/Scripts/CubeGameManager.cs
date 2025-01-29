using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

public class CubeGameManager : MonoBehaviour
{

    public GameObject markerBottomLeft;
    public GameObject markerTopRight;

    //min distance set to diameter of the cubes
    public float minDistance = 0.1f;

    public GameObject redCubePrefab;
    public GameObject blueCubePrefab;
    public GameObject greenCubePrefab;
    public GameObject yellowCubePrefab;

    private List<Vector3> gridPositions = new List<Vector3>();

    private int cubeAmount = 0;
    private int sortedCubesAmount = 0;
    private string pathCubeGame = Application.dataPath + "/CSV-Data/CubeGame.csv";

    private List<DateTime> finishedRoundTimeStamps = new List<DateTime>();
    private TextWriter writerCubeGame;

    // Start is called before the first frame update
    void Start()
    {
        GenerateGrid();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     private void OnEnable() {
        SortHitboxCollider.OnCubeSorted += UpdateSortedCubesAmount;
        QuestionnaireManager.OnQuestionnaireDone += writeCubeGameTimestampsToCSV;
    }

    private void OnDisable() {
        SortHitboxCollider.OnCubeSorted -= UpdateSortedCubesAmount;
        QuestionnaireManager.OnQuestionnaireDone -= writeCubeGameTimestampsToCSV;
    }

    private void UpdateSortedCubesAmount(){
        Debug.Log("OnCubeSorted received");
        sortedCubesAmount++;

        if(sortedCubesAmount == cubeAmount){
            Debug.Log("All cubes sorted!");
            finishedRoundTimeStamps.Add(DateTime.Now);

            //next "round" starts
            cubeAmount = 0;
            sortedCubesAmount = 0;
            GenerateGrid();
        }
    }

    private void writeCubeGameTimestampsToCSV(){
        writerCubeGame = new StreamWriter(pathCubeGame);
        writerCubeGame.WriteLine("Finished Round Timestamp");
        foreach (DateTime timestamp in finishedRoundTimeStamps){
            writerCubeGame.WriteLine(timestamp);
        }
        writerCubeGame.Close();
        Debug.Log("CubeGame timestamps written to CSV");
    }

    private void GenerateGrid(){

        gridPositions.Clear();

        Vector3 bottomLeft = markerBottomLeft.transform.position;
        Vector3 topRight = markerTopRight.transform.position;

        for (float x = bottomLeft.x; x < topRight.x; x += minDistance){
            for (float z = bottomLeft.z; z < topRight.z; z += minDistance){
                float y = bottomLeft.y;
                gridPositions.Add(new Vector3(x,y,z));
                
            }
        }
        Debug.Log("Grid generated with "+gridPositions.Count+" positions");

        List<Vector3> spawnPositions = new List<Vector3>();
        //get 20 random positions from the gridPositions list
        for (int i = 0; i < 20; i++){
            int randomIndex = UnityEngine.Random.Range(0,gridPositions.Count);
            Vector3 randomPosition = gridPositions[randomIndex];
            spawnPositions.Add(randomPosition);
            gridPositions.RemoveAt(randomIndex);
        }

        foreach (Vector3 pos in spawnPositions){
            Debug.Log("Spawned cube at position: "+pos);
            SpawnCubeAtPosition(pos);
            cubeAmount++;
        }
    }

    private void SpawnCubeAtPosition(Vector3 position){
        GameObject cubePrefab = GetRandomCubePrefab();
        Instantiate(cubePrefab,position,Quaternion.identity);
    }

    private GameObject GetRandomCubePrefab(){
        int randomIndex = UnityEngine.Random.Range(0,4);
        switch(randomIndex){
            case 0:
                return redCubePrefab;
            case 1:
                return blueCubePrefab;
            case 2:
                return greenCubePrefab;
            case 3:
                return yellowCubePrefab;
            default:
                return redCubePrefab;
        }
    }

}
