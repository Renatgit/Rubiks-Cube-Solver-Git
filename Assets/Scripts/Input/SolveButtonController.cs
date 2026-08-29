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
        List<string> solution = cubeGameController.SolveCurrentState(13);

        if (solution == null)
        {
            LogSolution(solution, false);
            return;
        }

        LogSolution(solution, true);

        AutomaticMovement.currentMovesList = new List<string>(solution);
    }

    private void LogSolution(List<string> solution, bool solved)
    {
        IDAStarSearchStats stats = IDAStarSolver.LastSearchStats;
        string solutionText = solution == null ? "null" : string.Join(", ", solution);
        int solutionLength = solution == null ? -1 : solution.Count;

        Debug.Log("IDA* solve"
            + " | solved: " + solved
            + " | length: " + solutionLength
            + " | time: " + stats.ElapsedMilliseconds + "ms"
            + " | nodes: " + stats.NodesVisited
            + " | bounds: " + stats.InitialBound + "->" + stats.FinalBound
            + " | solution: " + solutionText);
    }

}
