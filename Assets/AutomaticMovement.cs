using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutomaticMovement : MonoBehaviour
{
    private CubeState cubeState;
    private ReadCube readCube;

    public static bool automaticMovementIsActive = false;
    public static bool isAnimating = false;
    public static List<string> currentMovesList = new List<string>() {};
    private static readonly List<string> allMovesList = new List<string>()
    {
        "U", "D", "F", "B", "L", "R",
        "U'", "D'", "F'", "B'", "L'", "R'",
        "U2", "D2", "F2", "B2", "L2", "R2"
    };

    // Start is called before the first frame update
    void Start()
    {
        cubeState = FindObjectOfType<CubeState>();
        readCube = FindObjectOfType<ReadCube>();
    }

    // Update is called once per frame
    void Update()
    {
        //if the list of current moves is not empty and sides are not currently moving
        //do a first move from the list & remove this move
        if (currentMovesList.Count > 0)
        {
            isAnimating = true;
            if (!automaticMovementIsActive && CubeState.start) 
            {
                //do first move at index 0
                OnEachMoveDo(currentMovesList[0]);
                //remove this move
                currentMovesList.Remove(currentMovesList[0]);
            }
        }
        else
        {
            isAnimating = false;
        }
    }

    //automatic rotation of the side by the passed angle
    void AutomaticRotateSide(List<GameObject> side, float angle)
    {
        PivotRotation pivotRotation = side[4].transform.parent.GetComponent<PivotRotation>();
        pivotRotation.InitAutomaticMove(side, angle);
    }

    //Does each move in the currentMoveList
    void OnEachMoveDo(string move)
    {
        automaticMovementIsActive = true;
        //do an automatic move depending on the notation letter
        switch (move)
        {
            //"F"s - front
            case "F":
                AutomaticRotateSide(cubeState.front, -90);
                break;
            case "F'":
                AutomaticRotateSide(cubeState.front, 90);
                break;
            case "F2":
                AutomaticRotateSide(cubeState.front, -180);
                break;

            //"B"s - back
            case "B":
                AutomaticRotateSide(cubeState.back, -90);
                break;
            case "B'":
                AutomaticRotateSide(cubeState.back, 90);
                break;
            case "B2":
                AutomaticRotateSide(cubeState.back, -180);
                break;

            //"U"s - up
            case "U":
                AutomaticRotateSide(cubeState.up, -90);
                break;
            case "U'":
                AutomaticRotateSide(cubeState.up, 90);
                break;
            case "U2":
                AutomaticRotateSide(cubeState.up, -180);
                break;

            //"D"s - down
            case "D":
                AutomaticRotateSide(cubeState.down, -90);
                break;
            case "D'":
                AutomaticRotateSide(cubeState.down, 90);
                break;
            case "D2":
                AutomaticRotateSide(cubeState.down, -180);
                break;

            //"L"s - left
            case "L":
                AutomaticRotateSide(cubeState.left, -90);
                break;
            case "L'":
                AutomaticRotateSide(cubeState.left, 90);
                break;
            case "L2":
                AutomaticRotateSide(cubeState.left, -180);
                break;

            //"R"s - right
            case "R":
                AutomaticRotateSide(cubeState.right, -90);
                break;
            case "R'":
                AutomaticRotateSide(cubeState.right, 90);
                break;
            case "R2":
                AutomaticRotateSide(cubeState.right, -180);
                break;
            //Unrecognised move
            default:
                Debug.Log("Unknown move: " + move);
                break;
        }
    }
}
