using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Phases;
using UnityEngine;

public class CubeTester : MonoBehaviour
{
    private const int MaxTotalDepth = 12;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        TestShortestSolverScramble(
            "12-move scramble",
            "R", "U", "F'", "L2", "D", "B'", "R2", "U'", "F", "D2", "L", "B2");
    }

    private static void TestShortestSolverScramble(string testName, params string[] scramble)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, scramble);

        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        System.Collections.Generic.List<string> solution = TwoPhaseShortestSolver.Solve(
            state,
            MaxTotalDepth);
        stopwatch.Stop();

        bool solved = false;
        if (solution != null)
        {
            ApplyMoves(state, solution.ToArray());
            solved = SolverStateUtility.IsSolved(SolverStateData.FromCubeStateData(state));
        }

        TwoPhaseShortestSolverStats stats = TwoPhaseShortestSolver.LastStats;
        string solutionText = solution == null ? "null" : string.Join(", ", solution);
        int solutionLength = solution == null ? -1 : solution.Count;

        Debug.Log("CUBE TESTS - TwoPhaseShortestSolver " + testName
            + " | solved: " + solved
            + " | length: " + solutionLength
            + " | time: " + stopwatch.ElapsedMilliseconds + "ms"
            + " | internal time: " + stats.TotalElapsedMilliseconds + "ms"
            + " | max depth: " + MaxTotalDepth
            + " | lower bound: " + stats.InitialLowerBound
            + " | final depth: " + stats.FinalDepth
            + " | depths tried: " + stats.TotalDepthsTried
            + " | candidates: " + stats.CandidatesFound
            + " | phase1 nodes: " + stats.Phase1NodesVisited
            + " | phase2 attempts: " + stats.Phase2Attempts
            + " | skipped h: " + stats.SkippedByPhase2Heuristic
            + " | phase2 total: " + stats.TotalPhase2Milliseconds + "ms"
            + " | solution: " + solutionText);
    }

    private static void ApplyMoves(CubeStateData state, params string[] moves)
    {
        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(state, move, false);
        }
    }
}
