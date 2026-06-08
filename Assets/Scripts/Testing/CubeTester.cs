using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Solver;

public class CubeTester : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;

        CubeStateData scrambled = CubeState.CreateSolvedState();
        ApplyMoves(scrambled, "R", "U2", "F'", "L'");

        List<string> solution = BFSSolver.Solve(scrambled, 5);

        Debug.Log("BFS solution found: " + (solution != null));
        Debug.Log("BFS solution: " + FormatSolution(solution));

        if (solution != null)
        {
            CubeStateData solvedCheck = CubeState.CloneState(scrambled);
            ApplyMoves(solvedCheck, solution.ToArray());

            Debug.Log("BFS solution solves cube: " + CubeStateUtility.IsSolved(solvedCheck));
        }
    }

    private void ApplyMoves(CubeStateData cube, params string[] moves)
    {
        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(cube, move);
        }
    }

    private string FormatSolution(List<string> solution)
    {
        return solution == null ? "null" : string.Join(", ", solution);
    }
}
