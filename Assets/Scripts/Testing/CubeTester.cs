using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.Phases;
using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public class CubeTester : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        Phase1MoveTables.BuildIfNeeded();
        Phase1Heuristic.Prepare();
        Phase2Heuristic.Prepare();
        CornerPDBHeuristics.Prepare();
        EdgeGroupPDBHeuristics.Prepare();
        FullCubeHeuristic.Prepare();

        TestShortestSolverScramble(
            "17-move scramble",
            "R", "U", "F'", "L2", "D", "B'", "R2", "U'", "F",
            "D2", "L", "B2", "U", "R", "F", "L", "B");
    }

    private static void TestShortestSolverScramble(
        string testName,
        params string[] scramble)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, scramble);

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<string> solution = TwoPhaseShortestSolver.Solve(state, scramble.Length);
        stopwatch.Stop();

        bool solved = false;
        if (solution != null)
        {
            ApplyMoves(state, solution.ToArray());
            solved = SolverStateUtility.IsSolved(SolverStateData.FromCubeStateData(state));
        }

        TwoPhaseShortestSolverStats stats = TwoPhaseShortestSolver.LastStats;
        int solutionLength = solution == null ? -1 : solution.Count;
        string solutionText = solution == null ? "null" : string.Join(", ", solution);
        long phase1NodesPerSecond = stats.TotalElapsedMilliseconds == 0
            ? stats.Phase1NodesVisited
            : stats.Phase1NodesVisited * 1000 / stats.TotalElapsedMilliseconds;

        Debug.Log("CUBE TESTS - TwoPhaseShortestSolver " + testName
            + " | axis mode: " + stats.AxisHeuristicMode
            + " | workers: " + stats.ParallelWorkers
            + " | cancelled branches: " + stats.CancelledBranches
            + " | solved: " + solved
            + " | exact PDB: " + Phase1Heuristic.IsUsingExactSymmetryDatabase
            + " | length: " + solutionLength
            + " | time: " + stopwatch.ElapsedMilliseconds + "ms"
            + " | internal time: " + stats.TotalElapsedMilliseconds + "ms"
            + " | max depth: " + scramble.Length
            + " | lower bound: " + stats.InitialLowerBound
            + " | final depth: " + stats.FinalDepth
            + " | depths tried: " + stats.TotalDepthsTried
            + " | candidates: " + stats.CandidatesFound
            + " | phase1 goals: " + stats.Phase1GoalsReached
            + " | prefiltered: " + stats.Phase1CandidatesPrefiltered
            + " | rebuilt: " + stats.Phase1CandidatesRebuilt
            + " | phase1 nodes: " + stats.Phase1NodesVisited
            + " | triple-axis lookups: " + stats.Phase1TripleAxisLookups
            + " | triple-axis pruned: " + stats.Phase1PrunedByTripleAxisLowerBound
            + " | corner pruned: " + stats.Phase1PrunedByCornerLowerBound
            + " | corner-edge lookups: " + stats.Phase1CornerEdgeLookups
            + " | corner-edge pruned: " + stats.Phase1PrunedByCornerEdgeLowerBound
            + " | edge A lookups: " + stats.Phase1EdgeGroupALookups
            + " | edge A pruned: " + stats.Phase1PrunedByEdgeGroupALowerBound
            + " | edge B lookups: " + stats.Phase1EdgeGroupBLookups
            + " | edge B pruned: " + stats.Phase1PrunedByEdgeGroupBLowerBound
            + " | phase1 nodes/s: " + phase1NodesPerSecond
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
