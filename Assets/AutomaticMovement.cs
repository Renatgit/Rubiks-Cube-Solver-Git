using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using RubiksCubeSim;

public class AutomaticMovement : MonoBehaviour
{
    private CubeState cubeState;
    private ReadCube readCube;
    private TMP_InputField inputField;
    private TMP_Text tmpSeq;
    private TMP_Text tmpShuffleText;

    public static bool automaticMovementIsActive = false;
    public static bool isAnimating = false;

    public static List<string> currentMovesList = new List<string>() {};
    private static readonly List<string> allMovesList = new List<string>()
    {
        "U", "D", "F", "B", "L", "R",
        "U'", "D'", "F'", "B'", "L'", "R'",
        "U2", "D2", "F2", "B2", "L2", "R2"
    };

    private static Dictionary<string, string> oppositeSides = new Dictionary<string, string>()
    {
        {"U", "D"},
        {"D", "U"},
        {"L", "R"},
        {"R", "L"},
        {"F", "B"},
        {"B", "F"},
    };

    // Start is called before the first frame update
    void Start()
    {
        cubeState = FindObjectOfType<CubeState>();
        readCube = FindObjectOfType<ReadCube>();

        inputField = FindObjectOfType<TMP_InputField>();
        tmpSeq = GameObject.Find("ShuffleSolveSequence").GetComponent<TMP_Text>();
        tmpShuffleText = GameObject.Find("ShuffleDisplay").GetComponent<TMP_Text>();

        Color tempColor = tmpShuffleText.color;
        tempColor.a = 0;  // Fully Transparent
        tmpShuffleText.color = tempColor;
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

    //Shuffle() method is called when the user presses the button
    public void Shuffle()
    {
        tmpSeq.text = "";
        int numOfMoves = Random.Range(20, 35);
        List<string> createdListOfMoves = new List<string>();

        createdListOfMoves = GenerateShuffleSeq(numOfMoves);
        currentMovesList = createdListOfMoves;

        Color tempColor = tmpShuffleText.color;
        tempColor.a = 1;  // Not Transparent
        tmpShuffleText.color = tempColor;
        DisplaySequence(createdListOfMoves);
    }

    //Function that returns a list of moves that represent shuffle sequence
    List<string> GenerateShuffleSeq(int seqLength)
    {
        List<string> possibleMoves = allMovesList;
        List<string> sequence = new List<string>();

        MoveNode currentNode = null;

        //for loop that creates the actual sequence
        //using a tree structure
        //generating only nodes that are randomly chosen
        //without generating the whole tree of possible moves
        for(int i = 0; i < seqLength; i++)
        {
            List<string> validMoves = new List<string>(possibleMoves);


            if(currentNode != null)
            {
                //remove 'previous' move from valid moves of a type
                //for example if it was "U2" -> "U", "U'" and "U2" will be removed
                validMoves.RemoveAll(move => move.StartsWith(currentNode.Move[0]));
                //if previous move was an opposite side then remove it from valid moves
                //so only the adjacent sides are left
                if(currentNode.Parent != null)
                {
                    if (oppositeSides[currentNode.Move[0].ToString()] == currentNode.Parent.Move[0].ToString())
                    {
                        validMoves.RemoveAll(move => move.StartsWith(currentNode.Parent.Move[0]));
                    }
                }
            }

            //select a random move
            int randomIndex = Random.Range(0, validMoves.Count);
            string randomMove = validMoves[randomIndex];

            //add this move to the sequence
            sequence.Add(randomMove);
            currentNode = new MoveNode(randomMove, currentNode);
        }

        return sequence;
    }


    public void InputFieldNotationInput(string move)
    {
        string formatMove = move.ToUpper();

        if(allMovesList.Contains(formatMove) && !isAnimating){
            currentMovesList.Add(formatMove);
        }
        //else
        //{

        //}
        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();
    }

    private void DisplaySequence(List<string> moves)
    {
        for(int i = 0; i < moves.Count - 1; i++)
        {
            tmpSeq.text += moves[i] + " ";
        }
        tmpSeq.text += moves[moves.Count - 1];
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
