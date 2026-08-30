using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.Phases;
using System.IO;
using UnityEngine;

public class CubeTester : MonoBehaviour
{
    private const bool RunRegressionTestsAutomatically = true;
    private const bool RunPhase1SolverTestAutomatically = false;
    private const bool RunPhase2MoveTestAutomatically = true;
    private const int Phase1SolverTestMaxDepth = 12;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        if (RunRegressionTestsAutomatically)
        {
            RunRegressionTests();
        }

        if (RunPhase1SolverTestAutomatically)
        {
            RunPhase1SolverTest();
        }

        if (RunPhase2MoveTestAutomatically)
        {
            RunPhase2MoveTest();
        }
    }

    public static void RunRegressionTests()
    {
        bool coordinatesPass = Phase1CoordinatesWork();
        bool phase1GoalPass = Phase1GoalWorks();
        bool solverStatePass = SolverStateMovePipelineWorks();
        bool phase1PdbPass = Phase1PdbFilesWork();

        Debug.Log("CUBE TESTS - Regression"
            + " | coordinates: " + coordinatesPass
            + " | phase1 goal: " + phase1GoalPass
            + " | solver state moves: " + solverStatePass
            + " | phase1 PDB files: " + phase1PdbPass
            + " | passed: " + (coordinatesPass && phase1GoalPass && solverStatePass && phase1PdbPass));
    }

    public static void RunPhase1SolverTest()
    {
        Debug.Log("CUBE TESTS - Phase1Solver test started, max depth " + Phase1SolverTestMaxDepth);
        TestPhase1SolverScramble(
            "10-move scramble",
            "R", "U", "F'", "L2", "D", "B'", "R2", "U'", "F", "D2");

        TestPhase1SolverScramble(
            "12-move scramble",
            "R", "F", "U'", "L", "B", "D2", "R'", "F2", "U", "B'", "L2", "D");
    }

    private static bool Phase1CoordinatesWork()
    {
        CubeStateData solvedCube = CubeState.CreateSolvedState();
        SolverStateData solvedState = SolverStateData.FromCubeStateData(solvedCube);

        int solvedCornerOrientation = Phase1Coordinate.GetCornerOrientationIndex(solvedState);
        int solvedEdgeOrientation = Phase1Coordinate.GetEdgeOrientationIndex(solvedState);
        int solvedSlicePosition = Phase1Coordinate.GetSlicePositionIndex(solvedState);

        int[] solvedSlicePositions = Phase1Coordinate.GetSlicePositionsFromIndex(solvedSlicePosition);
        bool sliceRoundTrip = Phase1Coordinate.GetSlicePositionIndexFromPositions(solvedSlicePositions) == solvedSlicePosition;

        CubeStateData movedCube = CubeState.CreateSolvedState();
        ApplyMoves(movedCube, "R", "U", "F");

        SolverStateData movedState = SolverStateData.FromCubeStateData(movedCube);
        int movedEdgeOrientation = Phase1Coordinate.GetEdgeOrientationIndex(movedState);
        int[] rebuiltEdgeOrientation = Phase1Coordinate.GetEdgeOrientationFromIndex(movedEdgeOrientation);

        bool edgeOrientationRoundTrip = ArraysMatch(movedState.FullEdgeOrientation, rebuiltEdgeOrientation);
        bool combinedIndexesInRange =
            Phase1Coordinate.GetCornerSliceIndex(movedState) < Phase1Coordinate.CornerSliceCount
            && Phase1Coordinate.GetEdgeSliceIndex(movedState) < Phase1Coordinate.EdgeSliceCount;

        return solvedCornerOrientation == 0
            && solvedEdgeOrientation == 0
            && sliceRoundTrip
            && edgeOrientationRoundTrip
            && combinedIndexesInRange;
    }

    private static bool Phase1GoalWorks()
    {
        return Phase1GoalReached() == true
            && Phase1GoalReached("U") == true
            && Phase1GoalReached("U", "D2") == true
            && Phase1GoalReached("R") == false
            && Phase1GoalReached("F") == false
            && Phase1GoalReached("R2") == true
            && Phase1GoalReached("F2") == true;
    }

    private static bool SolverStateMovePipelineWorks()
    {
        string[][] testSequences =
        {
            new string[] { "R" },
            new string[] { "R", "U", "F" },
            new string[] { "R", "U2", "F'", "L" },
            new string[] { "B", "D", "R'", "F2", "L", "U" }
        };

        foreach (string[] sequence in testSequences)
        {
            CubeStateData cubeState = CubeState.CreateSolvedState();
            SolverStateData solverState = SolverStateData.FromCubeStateData(cubeState);

            foreach (string move in sequence)
            {
                MoveProcessor.ApplyMove(cubeState, move, false);
                MoveProcessor.ApplyMove(solverState, move);
            }

            if (!SolverStateMatchesCubeState(solverState, cubeState))
            {
                Debug.Log("CUBE TESTS - SolverState mismatch on sequence: " + string.Join(", ", sequence));
                return false;
            }
        }

        return true;
    }

    private static bool Phase1PdbFilesWork()
    {
        Phase1Heuristic.ClearDatabases();

        string cornerSlicePath = Application.dataPath + "/PatternDatabase/phase1_corner_slice.pdb";
        string edgeSlicePath = Application.dataPath + "/PatternDatabase/phase1_edge_slice.pdb";

        if (!File.Exists(cornerSlicePath) || !File.Exists(edgeSlicePath))
        {
            Debug.Log("CUBE TESTS - Phase 1 PDB files missing");
            return false;
        }

        int solvedEstimate = Phase1Estimate();
        int rEstimate = Phase1Estimate("R");
        int fEstimate = Phase1Estimate("F");
        int rufEstimate = Phase1Estimate("R", "U", "F");

        return solvedEstimate == 0
            && rEstimate > 0
            && fEstimate > 0
            && rufEstimate > 0;
    }

    private static void TestPhase1SolverScramble(string testName, params string[] scramble)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, scramble);

        System.Collections.Generic.List<string> phase1Moves = Phase1Solver.Solve(state, Phase1SolverTestMaxDepth);

        bool reached = false;
        if (phase1Moves != null)
        {
            ApplyMoves(state, phase1Moves.ToArray());
            reached = Phase1Goal.IsReached(SolverStateData.FromCubeStateData(state));
        }

        IDAStarSearchStats stats = Phase1Solver.LastSearchStats;
        string movesText = phase1Moves == null ? "null" : string.Join(", ", phase1Moves);
        int moveCount = phase1Moves == null ? -1 : phase1Moves.Count;

        Debug.Log("CUBE TESTS - Phase1Solver " + testName
            + " | reached: " + reached
            + " | length: " + moveCount
            + " | time: " + stats.ElapsedMilliseconds + "ms"
            + " | nodes: " + stats.NodesVisited
            + " | bounds: " + stats.InitialBound + "->" + stats.FinalBound
            + " | moves: " + movesText);
    }

    public static void RunPhase2MoveTest()
    {
        CubeStateData phase1Cube = CubeState.CreateSolvedState();
        ApplyMoves(phase1Cube, "U", "R2", "F2", "D", "L2", "B2");

        bool startInPhase1 = Phase1Goal.IsReached(SolverStateData.FromCubeStateData(phase1Cube));
        bool allPhase2MovesPreservePhase1 = true;

        foreach (string move in MoveGenerator.GetValidPhase2Moves(null))
        {
            CubeStateData child = CubeState.CloneState(phase1Cube);
            MoveProcessor.ApplyMove(child, move, false);

            if (!Phase1Goal.IsReached(SolverStateData.FromCubeStateData(child)))
            {
                allPhase2MovesPreservePhase1 = false;
                Debug.Log("CUBE TESTS - Phase2 move broke Phase1Goal: " + move);
            }
        }

        bool moveCountCorrect = MoveGenerator.GetValidPhase2Moves(null).Count == 10;
        bool sameFacePruningWorks = MoveGenerator.GetValidPhase2Moves("U").Count == 7
            && MoveGenerator.GetValidPhase2Moves("R2").Count == 9;

        Debug.Log("CUBE TESTS - Phase2Moves"
            + " | count 10: " + moveCountCorrect
            + " | start in phase1: " + startInPhase1
            + " | preserve phase1: " + allPhase2MovesPreservePhase1
            + " | pruning: " + sameFacePruningWorks
            + " | passed: " + (moveCountCorrect && startInPhase1 && allPhase2MovesPreservePhase1 && sameFacePruningWorks));
    }

    private static bool Phase1GoalReached(params string[] moves)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, moves);

        return Phase1Goal.IsReached(SolverStateData.FromCubeStateData(state));
    }

    private static int Phase1Estimate(params string[] moves)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, moves);

        return Phase1Heuristic.Estimate(SolverStateData.FromCubeStateData(state));
    }

    private static void ApplyMoves(CubeStateData state, params string[] moves)
    {
        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(state, move, false);
        }
    }

    private static bool SolverStateMatchesCubeState(SolverStateData solverState, CubeStateData cubeState)
    {
        return ArraysMatch(solverState.CornerPermutation, cubeState.cornerPermutation.ToArray())
            && ArraysMatch(solverState.CornerOrientation, cubeState.cornerOrientation.ToArray())
            && ArraysMatch(solverState.FullEdgePermutation, cubeState.fullEdgePermutation.ToArray())
            && ArraysMatch(solverState.FullEdgeOrientation, cubeState.fullEdgeOrientation.ToArray());
    }

    private static bool ArraysMatch(int[] a, int[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }
}
