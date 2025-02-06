using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PivotRotation : MonoBehaviour
{
    private List<GameObject> activeSide;
    private Vector3 localFwd;
    private Vector3 mouseRef;
    private bool dragActive = false;
    
    private CubeState cubeState;
    private ReadCube readCube;

    private float sensitivity = 0.4f;
    private Vector3 rotation;

    // Start is called before the first frame update
    void Start()
    {
        cubeState = FindObjectOfType<CubeState>();
        readCube = FindObjectOfType<ReadCube>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public void Rotate(List<GameObject> side)
    {
        //passing the variables 
        activeSide = side;
        mouseRef = Input.mousePosition;
        dragActive = true;

        //create a vector to rotate around
        localFwd = Vector3.zero - side[4].transform.parent.transform.localPosition;
    }
}
