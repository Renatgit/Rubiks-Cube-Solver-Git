using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CubeTester : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;

        CubeState cubeState = FindObjectOfType<CubeState>();

        CubeStateData scanned = cubeState.GetCubeStateData();

        Debug.Log("SCANNED - Corner permutation: " + string.Join(", ", scanned.cornerPermutation));
        Debug.Log("SCANNED - Corner orientation: " + string.Join(", ", scanned.cornerOrientation));
        Debug.Log("SCANNED - Full edge permutation: " + string.Join(", ", scanned.fullEdgePermutation));
        Debug.Log("SCANNED - Full edge orientation: " + string.Join(", ", scanned.fullEdgeOrientation));

        TestReturnsSolved("Logical R + R'", "R", "R'");
        TestReturnsSolved("Logical L + L'", "L", "L'");
        TestReturnsSolved("Logical F + F'", "F", "F'");
        TestReturnsSolved("Logical B + B'", "B", "B'");
    }

    private void TestCloneDoesNotMutateOriginal()
    {
        CubeStateData solved = CubeState.CreateSolvedState();
        CubeStateData clone = CubeState.CloneState(solved);

        MoveProcessor.ApplyMove(clone, "R");

        Debug.Log("Clone test - original still solved = " + IsSameState(solved, CubeState.CreateSolvedState()));
        Debug.Log("Clone test - clone changed = " + !IsSameState(clone, solved));
    }

    private void TestReturnsSolved(string testName, params string[] moves)
    {
        CubeStateData solved = CubeState.CreateSolvedState();
        CubeStateData cube = CubeState.CreateSolvedState();

        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(cube, move);
        }

        Debug.Log(testName + ": solved = " + IsSameState(cube, solved));
    }

    private bool IsSameState(CubeStateData a, CubeStateData b)
    {
        return ListsMatch(a.cornerPermutation, b.cornerPermutation)
            && ListsMatch(a.cornerOrientation, b.cornerOrientation)
            && ListsMatch(a.fullEdgePermutation, b.fullEdgePermutation)
            && ListsMatch(a.fullEdgeOrientation, b.fullEdgeOrientation);
    }

    private bool ListsMatch(List<int> a, List<int> b)
    {
        if (a == null || b == null || a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }
}
