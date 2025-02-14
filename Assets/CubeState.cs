using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubeState : MonoBehaviour
{

    //sides
    public List<GameObject> front = new List<GameObject>();
    public List<GameObject> back = new List<GameObject>();
    public List<GameObject> right = new List<GameObject>();
    public List<GameObject> left = new List<GameObject>();
    public List<GameObject> up = new List<GameObject>();
    public List<GameObject> down = new List<GameObject>();

    public static bool start = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ParentSidePiecesToCenter(List<GameObject> cubeSide)
    {
        foreach (GameObject piece in cubeSide)
        {
            //attach the parent of each piece
            //to the parent of the 4th piece(the center piece)
            //unless it is the center piece already
            if (piece != cubeSide[4])
            {
                piece.transform.parent.transform.parent = cubeSide[4].transform.parent;
            }
        }
    }

    public void UngroupSide(List<GameObject> cubeSide, Transform cubeIsParent)
    {
        foreach(GameObject piece in cubeSide)
        {
            if (piece != cubeSide[4])
            {
                piece.transform.parent.transform.parent = cubeIsParent;
            }
        }
    }
}
