using System.Collections;
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Fusion.Editor;
using Oculus.Interaction.DebugTree;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class SortHitboxCollider : MonoBehaviour
{
    
    public delegate void CubeSorted();
    public static event CubeSorted OnCubeSorted;

    public enum Color{
        blue,
        red,
        green,
        yellow
    }

    [SerializeField] Color color;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerExit(Collider cube){
        //Destroy cube for now
    
        if(cube.gameObject.CompareTag(color.ToString())){
            Debug.Log("erkannter Cube: "+cube.gameObject.tag +" HitBox-Farbe: "+ color.ToString());
            Destroy(cube.gameObject);
            OnCubeSorted?.Invoke();
        }
        
    }
}
