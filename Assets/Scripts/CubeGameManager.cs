using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeGameManager : MonoBehaviour
{

    public GameObject markerBottomLeft;
    public GameObject markerTopRight;

    public float minDistance = 0.1f;

    public GameObject redCubePrefab;
    public GameObject blueCubePrefab;
    public GameObject greenCubePrefab;
    public GameObject yellowCubePrefab;

    private List<Vector3> gridPositions = new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        GenerateGrid();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GenerateGrid(){

        gridPositions.Clear();

        Vector3 bottomLeft = markerBottomLeft.transform.position;
        Vector3 topRight = markerTopRight.transform.position;

        for (float x = bottomLeft.x; x < topRight.x; x += minDistance){
            for (float z = bottomLeft.y; z < topRight.z; z += minDistance){
                float y = minDistance;
                gridPositions.Add(new Vector3(x,y,z));
                
            }
        }
        Debug.Log("Grid generated with "+gridPositions.Count+" positions");

        List<Vector3> spawnPositions = new List<Vector3>();
        //get 20 random positions from the gridPositions list
        for (int i = 0; i < 20; i++){
            int randomIndex = Random.Range(0,gridPositions.Count);
            Vector3 randomPosition = gridPositions[randomIndex];
            spawnPositions.Add(randomPosition);
            gridPositions.RemoveAt(randomIndex);
        }

        foreach (Vector3 pos in spawnPositions){
            Debug.Log("Spawned cube at position: "+pos);
            SpawnCubeAtPosition(pos);
        }
    }

    private void SpawnCubeAtPosition(Vector3 position){
        GameObject cubePrefab = GetRandomCubePrefab();
        Instantiate(cubePrefab,position,Quaternion.identity);
    }

    private GameObject GetRandomCubePrefab(){
        int randomIndex = Random.Range(0,4);
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
