using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectFace : MonoBehaviour
{
    CubeState cubeState;
    ReadCube readCube;

    int layerMask = 1 << 6;
    // Start is called before the first frame update
    void Start()
    {
        cubeState = FindObjectOfType<CubeState>();
        readCube = FindObjectOfType<ReadCube>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!AutomaticMovement.isAnimating)
        {
            if (Input.GetMouseButtonDown(0)) //if left mouse button is clicked
            {
                int indexOfPieceHit = -1;

                //read the current state of the cube
                readCube.ReadState();

                //raycast from the mouse pointer towards the cube to detect if a face is hit
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100.0f, layerMask))
                {
                    GameObject face = hit.collider.gameObject;

                    //make a lsit of all the sides
                    List<List<GameObject>> cubeSides = new List<List<GameObject>>()
                {
                    cubeState.up,
                    cubeState.down,
                    cubeState.left,
                    cubeState.right,
                    cubeState.front,
                    cubeState.back
                };

                    //If the face hit exists within a side
                    foreach (List<GameObject> cubeSide in cubeSides)
                    {
                        if (cubeSide.Contains(face))
                        {
                            indexOfPieceHit = cubeSide.IndexOf(face);
                            //make the pieces in the side children of the central piece
                            cubeState.ParentSidePiecesToCenter(cubeSide);

                            //start the rotation logic
                            cubeSide[4].transform.parent.GetComponent<PivotRotation>().Rotate(cubeSide, indexOfPieceHit);
                        }
                    }
                }
            }
        }
    }
}
