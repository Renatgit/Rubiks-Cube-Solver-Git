using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.PatternDatabases;
using Assets.Scripts.Solver.Phases;
using System.IO;
using UnityEngine;

public class CubeTester : MonoBehaviour
{
    private const bool RunRegressionTestsAutomatically = true;
    private const bool RunPhase1SolverTestAutomatically = false;
    private const bool RunPhase2MoveTestAutomatically = false;
    private const bool RunPhase2SolverTestAutomatically = true;
    private const bool RunPhase2CoordinateTestAutomatically = true;
    private const bool RunFullPhase2PdbGenerationAutomatically = false;
    private const int Phase1SolverTestMaxDepth = 12;
    private const int Phase2SolverTestMaxDepth = 18;

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

        if (RunPhase2CoordinateTestAutomatically)
        {
            RunPhase2CoordinateTest();
        }

        if (RunFullPhase2PdbGenerationAutomatically)
        {
            GenerateAndSaveFullPhase2Pdb();
        }

        if (RunPhase2SolverTestAutomatically)
        {
            RunPhase2SolverTest();
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

    private static bool Phase2PdbFilesWork()
    {
        Phase2Heuristic.ClearDatabases();

        string cornerSlicePath = Application.dataPath + "/PatternDatabase/phase2_corner_slice_permutation.pdb";
        string nonSliceEdgePath = Application.dataPath + "/PatternDatabase/phase2_non_slice_edge_permutation.pdb";

        if (!File.Exists(cornerSlicePath) || !File.Exists(nonSliceEdgePath))
        {
            Debug.Log("CUBE TESTS - Phase 2 PDB files missing");
            return false;
        }

        int solvedEstimate = Phase2Estimate();
        int phase2ScrambleEstimate = Phase2Estimate("U", "R2", "F2", "D'", "L2", "B2", "U2");

        return solvedEstimate == 0
            && phase2ScrambleEstimate > 0;
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

    public static void RunPhase2SolverTest()
    {
        Debug.Log("CUBE TESTS - Phase2Solver test started, max depth " + Phase2SolverTestMaxDepth);

        TestPhase2OnlyScramble(
            "phase2-only scramble",
            "U", "R2", "F2", "D'", "L2", "B2", "U2");

        TestTwoPhasePipeline(
            "10-move scramble",
            "R", "U", "F'", "L2", "D", "B'", "R2", "U'", "F", "D2");
    }

    public static void RunPhase2CoordinateTest()
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, "U", "R2", "F2", "D'", "L2", "B2", "U2");

        SolverStateData solverState = SolverStateData.FromCubeStateData(state);

        bool startsInPhase1 = Phase1Goal.IsReached(solverState);

        int cornerPermutationIndex = Phase2Coordinate.GetCornerPermutationIndex(solverState);
        int slicePermutationIndex = Phase2Coordinate.GetSlicePermutationIndex(solverState);
        int nonSliceEdgePermutationIndex = Phase2Coordinate.GetNonSliceEdgePermutationIndex(solverState);
        int cornerSlicePermutationIndex = Phase2Coordinate.GetCornerSlicePermutationIndex(solverState);

        int[] rebuiltCorners = CornerCoordinate.GetPermutationFromIndex(cornerPermutationIndex);
        int[] rebuiltSliceEdges = Phase2Coordinate.GetSlicePermutationFromIndex(slicePermutationIndex);
        int[] rebuiltNonSliceEdges = Phase2Coordinate.GetNonSliceEdgePermutationFromIndex(nonSliceEdgePermutationIndex);

        bool cornerRoundTrip = ArraysMatch(solverState.CornerPermutation, rebuiltCorners);
        bool sliceRoundTrip = SliceEdgesMatch(solverState.FullEdgePermutation, rebuiltSliceEdges);
        bool nonSliceRoundTrip = NonSliceEdgesMatch(solverState.FullEdgePermutation, rebuiltNonSliceEdges);

        bool indexesInRange =
            cornerPermutationIndex < Phase2Coordinate.CornerPermutationCount
            && slicePermutationIndex < Phase2Coordinate.SlicePermutationCount
            && nonSliceEdgePermutationIndex < Phase2Coordinate.NonSliceEdgePermutationCount
            && cornerSlicePermutationIndex < Phase2Coordinate.CornerSlicePermutationCount;

        Debug.Log("CUBE TESTS - Phase2Coordinate"
            + " | starts in phase1: " + startsInPhase1
            + " | corner round trip: " + cornerRoundTrip
            + " | slice round trip: " + sliceRoundTrip
            + " | non-slice round trip: " + nonSliceRoundTrip
            + " | indexes in range: " + indexesInRange
            + " | passed: " + (startsInPhase1 && cornerRoundTrip && sliceRoundTrip && nonSliceRoundTrip && indexesInRange));
    }

    public static void GenerateAndSaveFullPhase2Pdb()
    {
        string cornerSlicePath = Application.dataPath + "/PatternDatabase/phase2_corner_slice_permutation.pdb";
        string nonSliceEdgePath = Application.dataPath + "/PatternDatabase/phase2_non_slice_edge_permutation.pdb";

        Debug.Log("CUBE TESTS - Full Phase 2 PDB generation started");

        byte[] cornerSlicePdb = Phase2PDB.GenerateFullCornerSlicePermutation();
        Phase2PdbGenerationStats cornerSliceStats = Phase2PDB.LastGenerationStats;

        byte[] nonSliceEdgePdb = Phase2PDB.GenerateFullNonSliceEdgePermutation();
        Phase2PdbGenerationStats nonSliceEdgeStats = Phase2PDB.LastGenerationStats;

        Phase2PDB.Save(cornerSlicePdb, cornerSlicePath);
        Phase2PDB.Save(nonSliceEdgePdb, nonSliceEdgePath);

        byte[] loadedCornerSlicePdb = Phase2PDB.LoadCornerSlicePermutation(cornerSlicePath);
        byte[] loadedNonSliceEdgePdb = Phase2PDB.LoadNonSliceEdgePermutation(nonSliceEdgePath);

        Phase2Heuristic.ClearDatabases();
        bool fileCheck = Phase2PdbFilesWork();

        Debug.Log("CUBE TESTS - Full Phase 2 PDB"
            + " | corner-slice states: " + Phase2PDB.CountVisited(loadedCornerSlicePdb)
            + " | corner-slice max depth: " + cornerSliceStats.MaxDepth
            + " | corner-slice time: " + cornerSliceStats.ElapsedMilliseconds + "ms"
            + " | non-slice states: " + Phase2PDB.CountVisited(loadedNonSliceEdgePdb)
            + " | non-slice max depth: " + nonSliceEdgeStats.MaxDepth
            + " | non-slice time: " + nonSliceEdgeStats.ElapsedMilliseconds + "ms"
            + " | saved: " + (File.Exists(cornerSlicePath) && File.Exists(nonSliceEdgePath))
            + " | loaded lengths: " + (loadedCornerSlicePdb.Length == Phase2Coordinate.CornerSlicePermutationCount
                && loadedNonSliceEdgePdb.Length == Phase2Coordinate.NonSliceEdgePermutationCount)
            + " | file check: " + fileCheck
            + " | passed: " + (fileCheck
                && cornerSliceStats.VisitedStates == Phase2Coordinate.CornerSlicePermutationCount
                && nonSliceEdgeStats.VisitedStates == Phase2Coordinate.NonSliceEdgePermutationCount));
    }

    private static void TestPhase2OnlyScramble(string testName, params string[] scramble)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, scramble);

        bool startsInPhase1 = Phase1Goal.IsReached(SolverStateData.FromCubeStateData(state));
        System.Collections.Generic.List<string> phase2Moves = Phase2Solver.Solve(state, Phase2SolverTestMaxDepth);

        bool solved = false;
        if (phase2Moves != null)
        {
            ApplyMoves(state, phase2Moves.ToArray());
            solved = SolverStateUtility.IsSolved(SolverStateData.FromCubeStateData(state));
        }

        IDAStarSearchStats stats = Phase2Solver.LastSearchStats;
        LogPhase2Result(testName, startsInPhase1, solved, phase2Moves, stats);
    }

    private static void TestTwoPhasePipeline(string testName, params string[] scramble)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, scramble);

        System.Collections.Generic.List<string> phase1Moves = Phase1Solver.Solve(state, Phase1SolverTestMaxDepth);

        if (phase1Moves == null)
        {
            Debug.Log("CUBE TESTS - TwoPhase " + testName + " | phase1 failed");
            return;
        }

        ApplyMoves(state, phase1Moves.ToArray());
        bool phase1Reached = Phase1Goal.IsReached(SolverStateData.FromCubeStateData(state));

        System.Collections.Generic.List<string> phase2Moves = Phase2Solver.Solve(state, Phase2SolverTestMaxDepth);

        bool solved = false;
        if (phase2Moves != null)
        {
            ApplyMoves(state, phase2Moves.ToArray());
            solved = SolverStateUtility.IsSolved(SolverStateData.FromCubeStateData(state));
        }

        IDAStarSearchStats phase2Stats = Phase2Solver.LastSearchStats;
        int phase1Length = phase1Moves.Count;
        int phase2Length = phase2Moves == null ? -1 : phase2Moves.Count;
        int totalLength = phase2Moves == null ? -1 : phase1Length + phase2Length;
        string phase1Text = string.Join(", ", phase1Moves);
        string phase2Text = phase2Moves == null ? "null" : string.Join(", ", phase2Moves);

        Debug.Log("CUBE TESTS - TwoPhase " + testName
            + " | phase1 reached: " + phase1Reached
            + " | solved: " + solved
            + " | lengths: " + phase1Length + "+" + phase2Length + "=" + totalLength
            + " | phase2 time: " + phase2Stats.ElapsedMilliseconds + "ms"
            + " | phase2 nodes: " + phase2Stats.NodesVisited
            + " | phase2 bounds: " + phase2Stats.InitialBound + "->" + phase2Stats.FinalBound
            + " | phase1: " + phase1Text
            + " | phase2: " + phase2Text);
    }

    private static void LogPhase2Result(
        string testName,
        bool startsInPhase1,
        bool solved,
        System.Collections.Generic.List<string> moves,
        IDAStarSearchStats stats)
    {
        string movesText = moves == null ? "null" : string.Join(", ", moves);
        int moveCount = moves == null ? -1 : moves.Count;

        Debug.Log("CUBE TESTS - Phase2Solver " + testName
            + " | starts in phase1: " + startsInPhase1
            + " | solved: " + solved
            + " | length: " + moveCount
            + " | time: " + stats.ElapsedMilliseconds + "ms"
            + " | nodes: " + stats.NodesVisited
            + " | bounds: " + stats.InitialBound + "->" + stats.FinalBound
            + " | moves: " + movesText);
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

    private static int Phase2Estimate(params string[] moves)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, moves);

        return Phase2Heuristic.Estimate(SolverStateData.FromCubeStateData(state));
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

    private static bool SliceEdgesMatch(int[] fullEdgePermutation, int[] sliceEdges)
    {
        for (int i = 0; i < sliceEdges.Length; i++)
        {
            if (fullEdgePermutation[8 + i] != sliceEdges[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool NonSliceEdgesMatch(int[] fullEdgePermutation, int[] nonSliceEdges)
    {
        for (int i = 0; i < nonSliceEdges.Length; i++)
        {
            if (fullEdgePermutation[i] != nonSliceEdges[i])
            {
                return false;
            }
        }

        return true;
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
