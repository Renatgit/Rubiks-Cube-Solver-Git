using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Solver;

public class SolveButtonController : MonoBehaviour
{
    private CubeGameController cubeGameController;

    void Start()
    {
        cubeGameController = FindAnyObjectByType<CubeGameController>();
    }

    public void Solve()
    {
        List<string> solution = cubeGameController.SolveCurrentState(10);

        if (solution == null)
        {
            Debug.Log("No solution found within max depth.");
            LogIDAStarStats();
            return;
        }

        Debug.Log("Solution length: " + solution.Count + " moves");
        Debug.Log("Solution: " + string.Join(", ", solution));
        LogIDAStarStats();

        AutomaticMovement.currentMovesList = new List<string>(solution);
    }

    private void LogIDAStarStats()
    {
        IDAStarSearchStats stats = IDAStarSolver.LastSearchStats;

        Debug.Log("IDA* stats: initial bound = " + stats.InitialBound
            + ", final bound = " + stats.FinalBound
            + ", bound iterations = " + stats.BoundIterations
            + ", nodes visited = " + stats.NodesVisited
            + ", pruned = " + stats.PrunedByHeuristic
            + ", time = " + stats.ElapsedMilliseconds + "ms");
    }
}
