using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CubeStateData
{
    // index = position, value = piece
    public List<int> cornerPermutation;

    // index = position, value = orientation of piece in that position
    public List<int> cornerOrientation;

    public List<int> fullEdgePermutation;
    public List<int> fullEdgeOrientation;

    // Derived helper fields used by the PDB/heuristic code.
    public List<int> firstEightEdgePermutation;
    public List<int> lastFourEdgePermutation;
    public List<int> firstEightEdgeOrientation;
    public List<int> lastFourEdgeOrientation;

    public int depth;
    public List<string> moveHistory;
}

public class CubeState : MonoBehaviour
{
    public List<GameObject> front = new List<GameObject>();
    public List<GameObject> back = new List<GameObject>();
    public List<GameObject> right = new List<GameObject>();
    public List<GameObject> left = new List<GameObject>();
    public List<GameObject> up = new List<GameObject>();
    public List<GameObject> down = new List<GameObject>();

    public static bool start = false;

    public static CubeStateData CreateSolvedState()
    {
        return new CubeStateData
        {
            cornerPermutation = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 },
            cornerOrientation = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 },

            fullEdgePermutation = new List<int>
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },

            fullEdgeOrientation = new List<int>
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },

            firstEightEdgePermutation = new List<int>
            { 0, 1, 2, 3, 4, 5, 6, 7 },

            lastFourEdgePermutation = new List<int>
            { 8, 9, 10, 11 },

            firstEightEdgeOrientation = new List<int>
            { 0, 0, 0, 0, 0, 0, 0, 0 },

            lastFourEdgeOrientation = new List<int>
            { 0, 0, 0, 0 },

            depth = 0,
            moveHistory = new List<string>()
        };
    }

    public static CubeStateData CloneState(CubeStateData state)
    {
        if (state == null)
        {
            return null;
        }

        return new CubeStateData
        {
            cornerPermutation = CloneList(state.cornerPermutation),
            cornerOrientation = CloneList(state.cornerOrientation),
            fullEdgePermutation = CloneList(state.fullEdgePermutation),
            fullEdgeOrientation = CloneList(state.fullEdgeOrientation),
            firstEightEdgePermutation = CloneList(state.firstEightEdgePermutation),
            lastFourEdgePermutation = CloneList(state.lastFourEdgePermutation),
            firstEightEdgeOrientation = CloneList(state.firstEightEdgeOrientation),
            lastFourEdgeOrientation = CloneList(state.lastFourEdgeOrientation),
            depth = state.depth,
            moveHistory = CloneList(state.moveHistory)
        };
    }

    private static List<T> CloneList<T>(List<T> list)
    {
        return list == null ? new List<T>() : new List<T>(list);
    }

    public void ParentSidePiecesToCenter(List<GameObject> cubeSide)
    {
        foreach (GameObject piece in cubeSide)
        {
            if (piece != cubeSide[4])
            {
                piece.transform.parent.transform.parent = cubeSide[4].transform.parent;
            }
        }
    }

    public void UngroupSide(List<GameObject> cubeSide, Transform cubeIsParent)
    {
        foreach (GameObject piece in cubeSide)
        {
            if (piece != cubeSide[4])
            {
                piece.transform.parent.transform.parent = cubeIsParent;
            }
        }
    }
}
