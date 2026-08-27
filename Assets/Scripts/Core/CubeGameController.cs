using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Solver;

public class CubeGameController : MonoBehaviour
{
    public CubeStateData CurrentState { get; private set; }

    void Awake()
    {
        ResetState();
    }

    public void ResetState()
    {
        CurrentState = CubeState.CreateSolvedState();
    }

    public void CompleteMove(string move)
    {
        MoveProcessor.ApplyMove(CurrentState, move);
    }

    public void ClearMoveHistory()
    {
        if (CurrentState.moveHistory == null)
        {
            CurrentState.moveHistory = new List<string>();
            return;
        }

        CurrentState.moveHistory.Clear();
    }

    public List<string> SolveCurrentState(int maxDepth)
    {
        CubeStateData stateToSolve = CubeState.CloneState(CurrentState);
        //return BFSSolver.Solve(stateToSolve, maxDepth);
        return IDAStarSolver.Solve(stateToSolve, maxDepth);
    }

    public CubeStateData GetStateCopy()
    {
        return CubeState.CloneState(CurrentState);
    }

    public bool IsSolved()
    {
        return CubeStateUtility.IsSolved(CurrentState);
    }
}
