using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RubiksCubeSim;
using System.IO;

[System.Serializable] //make the class JSON-compatible
public class CubeStateData
{
    public List<int> cornerPermutation; //where each corner is(0-7)
    public List<int> cornerOrientation; //how each corner is rotated(0-2)
    public List<int> firstEightEdgePermutation;  //where each edge is(0-7)
    public List<int> lastFourEdgePermutation; // last four edges(0-3)
    public List<int> firstEightEdgeOrientation; //how each corner of the first 8 is flipped (0,1)
    public List<int> lastFourEdgeOrientation; //how each corner of the first 4 is flipped (0,1)
    public List<int> fullEdgePermutation; //where each edge is(0-11) - all edges in the cube
    public List<int> fullEdgeOrientation; //where each edge is(0-11) - all edges in the cube
    public Dictionary<int, string> lastFourEdgeAlignment; // stores the last four edges alignment with the cube sides(mom-white/yellow)
    public Dictionary<int, string> firstEightEdgesAlignment; // stores the first eight edges alignment with the cube sides(white/yellow)
    public int depth; //precomputed solving depth
    public List<string> solution; //stores mvoe sequence 
}



public class CubeState : MonoBehaviour
{
    public string[] cornersNamesPermutation; //stores 8 corner pieces

    public int[] cornerCurrentOrientation; //stores 8 corner current orientations

    public string[] firstEightEdgesNamesPermutation; //stores first 8 edge permutations

    public string[] lastFourEdgesNamesPermutation; //stores last 4 edges permutations

    public int[] firstEightEdgesOrientation; //stores first 8 edges orientation

    public int[] lastFourEdgesOrientation; //stores last 4 edges orientation

    public int[] fullEdgesPermutation; //stores all 12 edges permutation

    public int[] fullEdgesOrientation; //stores all 12 edges orientation

    public Dictionary<int, string> LastFourEdgesAlingment = new Dictionary<int, string>()
    {
        { 8, "None" },
        { 9, "None" },
        { 10, "None" },
        { 11, "None" }
    }; // stores the last four edges alignment with the cube sides(mom-white/yellow)
    public Dictionary<int, string> FirstEightEdgesAlingment = new Dictionary<int, string>()
    {
        { 0, "None" },
        { 1, "None" },
        { 2, "None" },
        { 3, "None" },
        { 4, "None" },
        { 5, "None" },
        { 6, "None" },
        { 7, "None" }
    }; // stores the first eight edges alignment with the cube sides(white/yellow)



    public CubeStateData GetCubeStateData()
    {
        CubeStateData stateData = new CubeStateData
        {
            cornerPermutation = GetCornerPermutation(),
            cornerOrientation = GetCornerOrientation(),
            firstEightEdgePermutation = GetFirstEightEdgePermutation(),
            lastFourEdgePermutation = GetLastFourEdgePermutation(),
            firstEightEdgeOrientation = GetFirstEightEdgeOrientation(),
            lastFourEdgeOrientation = GetLastFourEdgeOrientation(),
            fullEdgePermutation = GetFullEdgePermutation(),
            fullEdgeOrientation = GetFullEdgeOrientation(),
            lastFourEdgeAlignment = LastFourEdgesAlingment,
            firstEightEdgesAlignment = FirstEightEdgesAlingment,
            depth = 0
        };
        return stateData;
    }

    private List<int> GetCornerPermutation()
    {
        List<int> cornerPermutation = new List<int>();

        //Correct order of corner GameObjects in the solved state
        string[] correctCornerOrder = { "UFR", "UFL", "URB", "ULB", "DRF", "DLF", "DRB", "DLB" };

        // Compare scanned corners to solved order and get permutation
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                if(correctCornerOrder[i] == cornersNamesPermutation[j])
                {
                    cornerPermutation.Add(j);
                    break;
                }
            }
        }

        return cornerPermutation;
    }

    private List<int> GetCornerOrientation()
    {
        List<int> cornerOrientation = new List<int>();
        for(int i = 0; i < 8; i++)
        {
            cornerOrientation.Add(cornerCurrentOrientation[i]);
        }
        return cornerOrientation;
    }

    private List<int> GetFirstEightEdgePermutation()
    {
        List<int> firstEightEdgePermutation = new List<int>();

        // Compare scanned edges to solved order
        for (int i = 0; i < 12; i++)
        {
            if (fullEdgesPermutation[i] < 8)
            {
                firstEightEdgePermutation.Add(fullEdgesPermutation[i]);
            }
        }
        return firstEightEdgePermutation;
    }
    private List<int> GetLastFourEdgePermutation()
    {
        List<int> lastFourEdgePermutation = new List<int>();

        // Compare scanned edges to solved order
        for (int i = 0; i < 12; i++)
        {
            if (fullEdgesPermutation[i] >= 8)
            {
                lastFourEdgePermutation.Add(fullEdgesPermutation[i]);
            }
        }

        return lastFourEdgePermutation;
    }
    private List<int> GetFullEdgePermutation()
    {
        List<int> EdgePermutation = new List<int>();
        for (int i = 0; i < fullEdgesPermutation.Length; i++)
        {
            EdgePermutation.Add(fullEdgesPermutation[i]);
        }
        return EdgePermutation;
    }
    private List<int> GetFullEdgeOrientation()
    {
        List<int> EdgeOrientation = new List<int>();
        for (int i = 0; i < fullEdgesOrientation.Length; i++)
        {
            EdgeOrientation.Add(fullEdgesOrientation[i]);
        }
        return EdgeOrientation;
    }

    private List<int> GetFirstEightEdgeOrientation()
    {
        List<int> orientation = new List<int>();
        for(int i = 0; i < 12; i++)
        {
            if (fullEdgesPermutation[i] < 8)
            {
                orientation.Add(fullEdgesOrientation[i]);
            }
        }
        return orientation;
    }
    private List<int> GetLastFourEdgeOrientation()
    {
        List<int> orientation = new List<int>();
        for (int i = 0; i < 12; i++)
        {
            if (fullEdgesPermutation[i] >= 8)
            {
                orientation.Add(fullEdgesOrientation[i]);
            }
        }
        return orientation;
    }



    public int CalculateCornerOrientation(string stickerRightRotation, string stickerClockwise, string stickerAnticlockwise)
    {
        if(stickerRightRotation == "Up" || stickerRightRotation == "Down") //if yellow or white sticker facing Up or Down sides 
        {
            return 0; //orientation is correct
        }
        if (stickerClockwise == "Up" || stickerClockwise == "Down") //if yellow or white stickers facing clockwise side relatively to their local rotation
        {
            return 1; //orientation set to 1 - clockwise
        }
        else
        {
            return 2; //if facing anticlockwise side then orientation is 2 - anticlockwise
        }
    }

    public int CalculateEdgeOrientation(string edgeName, string stickerHit, string sideAligned, string secondSideAligned = "None", string secondStickerHit = "None")
    {
        if (edgeName[0] == 'U' && stickerHit == "Up" && sideAligned == "Up") //if piece with yellow sticker aligns ONLY with yellow side
        {
            FirstEightEdgesAlingment[edgeIndices[edgeName]] = "U";
            return 0;
        }
        else if(edgeName[0] == 'D' && stickerHit == "Down" && sideAligned == "Down") //if piece with white sticker aligns ONLY with white side
        {
            FirstEightEdgesAlingment[edgeIndices[edgeName]] = "D";
            return 0;
        }
        //For pieces FR, FL, BR and BL:
        //The piece should align with one of the side of the same colour
        else if (edgeName[0] == 'F' || edgeName[0] == 'B')
        {

            //=>
            //-------Block of code for FR, FL, BR and BL pieces for their alignment info-------
            if (sideAligned[0] == 'U' || sideAligned[0] == 'D')
            {
                //if the piece is aligned with the top or bottom side
                //-> set the value of the corresponding key index to the secondStickerHit


                LastFourEdgesAlingment[edgeIndices[edgeName]] = secondStickerHit[0].ToString(); 
            }
            else
            {
                LastFourEdgesAlingment[edgeIndices[edgeName]] = LastFourEdgesAlingment[edgeIndices[edgeName]];
            }
            if(edgeName == "BL")
            {
                Debug.Log(edgeIndices["BL"] + " -> " + LastFourEdgesAlingment[edgeIndices["BL"]]);
            }
            //----------------------------------------------------------------------------------
            //=>

            if (stickerHit[0] == sideAligned[0])// if the first sticker is aligned with teh side of the same colour 
            {
                return 0;
            }
            else if (secondStickerHit[0] == secondSideAligned[0]) // if the second sticker is aligned with teh side of the same colour 
            {
                return 0;
            }
        }

        //-------Block of code for white/yellow edges for their alignment info-------
        else if (edgeName[0] == 'U' || edgeName[0] == 'D')
        {
            if (stickerHit[0] == 'U' || stickerHit[0] == 'D')
            {
                FirstEightEdgesAlingment[edgeIndices[edgeName]] = sideAligned[0].ToString();
            }
            else
            {
                FirstEightEdgesAlingment[edgeIndices[edgeName]] = secondSideAligned[0].ToString();
            }
            //Debug.Log(edgeIndices[edgeName] + " -> " + FirstEightEdgesAlingment[edgeIndices[edgeName]]);
        }


        return 1;
    }

    Dictionary<string, int> edgeIndices = new Dictionary<string, int>
        {
            {"UR", 0}, {"UL", 1}, {"UB", 2}, {"UF", 3},
            {"DR", 4}, {"DL", 5}, {"DB", 6}, {"DF", 7},
            {"FR", 8}, {"FL", 9}, {"BR", 10}, {"BL", 11}
        };
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
        PatternDatabaseManager.SaveStateToPatternDatabase(GetCubeStateData());
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
