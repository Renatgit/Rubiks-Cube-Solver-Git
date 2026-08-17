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
        List<string> solution = cubeGameController.SolveCurrentState(7);

        if (solution == null)
        {
            Debug.Log("No solution found within max depth.");
            LogIDDFSStats();
            return;
        }

        Debug.Log("Solution: " + string.Join(", ", solution));
        LogIDDFSStats();

        AutomaticMovement.currentMovesList = new List<string>(solution);
    }

    private void LogIDDFSStats()
    {
        Debug.Log("IDDFS stats: depth reached = " + IDDFSSolver.LastStats.DepthReached
            + ", nodes searched = " + IDDFSSolver.LastStats.NodesSearched
            + ", time = " + IDDFSSolver.LastStats.ElapsedMilliseconds + "ms");
    }
}
