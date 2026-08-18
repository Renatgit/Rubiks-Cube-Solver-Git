using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;

public class CubeTester : MonoBehaviour
{
    private const bool RunPruningTestsAutomatically = false;
    private const bool RunHeuristicTestsAutomatically = false;
    private const bool RunCornerCoordinateTestsAutomatically = false;
    private const bool RunCornerPdbTestsAutomatically = true;

    [SerializeField] private bool runMoveRegressionOnStart = false;
    [SerializeField] private bool runMovePruningOnStart = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        if (RunPruningTestsAutomatically)
        {
            RunMovePruningTests();
        }

        if (RunHeuristicTestsAutomatically)
        {
            RunHeuristicTests();
        }

        if (RunCornerCoordinateTestsAutomatically)
        {
            RunCornerCoordinateTests();
        }

        if (RunCornerPdbTestsAutomatically)
        {
            RunCornerPdbTests();
        }
    }

    IEnumerator Start()
    {
        yield return null;

        if (runMoveRegressionOnStart)
        {
            RunMoveRegressionTests();
        }

        if (runMovePruningOnStart)
        {
            RunMovePruningTests();
        }
    }

    public static void RunMoveRegressionTests()
    {
        TestMoveAndInverse("R", "R'");
        TestMoveAndInverse("L", "L'");
        TestMoveAndInverse("U", "U'");
        TestMoveAndInverse("D", "D'");
        TestMoveAndInverse("F", "F'");
        TestMoveAndInverse("B", "B'");

        Debug.Log("CUBE TESTS - Move regression checks complete.");
    }

    public static void RunMovePruningTests()
    {
        TestMoveCount(null, 18);
        TestBannedFace("R", "R");
        TestBannedFace("R'", "R");
        TestBannedFace("R2", "R");

        TestBannedFace("L", "L");
        TestBannedFace("L", "R");

        TestBannedFace("D", "D");
        TestBannedFace("D", "U");

        TestBannedFace("B", "B");
        TestBannedFace("B", "F");

        Debug.Log("CUBE TESTS - Move pruning checks complete.");
    }

    public static void RunHeuristicTests()
    {
        CubeStateData solved = CubeState.CreateSolvedState();
        Debug.Log("CUBE TESTS - Solved heuristic is 0: " + (CubeHeuristic.Estimate(solved) == 0));

        CubeStateData singleMove = CubeState.CreateSolvedState();
        MoveProcessor.ApplyMove(singleMove, "R", false);
        Debug.Log("CUBE TESTS - R heuristic is at least 1: " + (CubeHeuristic.Estimate(singleMove) >= 1));

        CubeStateData cancelledMove = CubeState.CreateSolvedState();
        MoveProcessor.ApplyMove(cancelledMove, "R", false);
        MoveProcessor.ApplyMove(cancelledMove, "R'", false);
        Debug.Log("CUBE TESTS - R + R' heuristic is 0: " + (CubeHeuristic.Estimate(cancelledMove) == 0));

        CubeStateData scramble = CubeState.CreateSolvedState();
        ApplyMovesWithoutHistory(scramble, "R", "U", "F");

        CubeHeuristicBreakdown breakdown = CubeHeuristic.GetBreakdown(scramble);
        Debug.Log("CUBE TESTS - R U F heuristic estimate: " + breakdown.Estimate
            + " (misplaced corners=" + breakdown.MisplacedCorners
            + ", twisted corners=" + breakdown.TwistedCorners
            + ", misplaced edges=" + breakdown.MisplacedEdges
            + ", flipped edges=" + breakdown.FlippedEdges + ")");

        Debug.Log("CUBE TESTS - Heuristic checks complete.");
    }

    public static void RunCornerCoordinateTests()
    {
        CubeStateData solved = CubeState.CreateSolvedState();

        Debug.Log("CUBE TESTS - Solved corner orientation index is 0: "
            + (CornerCoordinate.GetOrientationIndex(solved) == 0));

        Debug.Log("CUBE TESTS - Solved corner permutation index is 0: "
            + (CornerCoordinate.GetPermutationIndex(solved) == 0));

        Debug.Log("CUBE TESTS - Solved full corner index is 0: "
            + (CornerCoordinate.GetIndex(solved) == 0));

        CubeStateData rMove = CubeState.CreateSolvedState();
        MoveProcessor.ApplyMove(rMove, "R", false);

        Debug.Log("CUBE TESTS - R changes corner orientation index: "
            + (CornerCoordinate.GetOrientationIndex(rMove) != 0));

        Debug.Log("CUBE TESTS - R changes corner permutation index: "
            + (CornerCoordinate.GetPermutationIndex(rMove) != 0));

        Debug.Log("CUBE TESTS - R changes full corner index: "
            + (CornerCoordinate.GetIndex(rMove) != 0));

        CubeStateData rThenRPrime = CubeState.CreateSolvedState();
        ApplyMovesWithoutHistory(rThenRPrime, "R", "R'");

        Debug.Log("CUBE TESTS - R + R' full corner index returns to 0: "
            + (CornerCoordinate.GetIndex(rThenRPrime) == 0));

        CubeStateData uMove = CubeState.CreateSolvedState();
        MoveProcessor.ApplyMove(uMove, "U", false);

        Debug.Log("CUBE TESTS - U keeps corner orientation index at 0: "
            + (CornerCoordinate.GetOrientationIndex(uMove) == 0));

        Debug.Log("CUBE TESTS - U changes corner permutation index: "
            + (CornerCoordinate.GetPermutationIndex(uMove) != 0));

        TestCornerCoordinateRoundTrip("solved");
        TestCornerCoordinateRoundTrip("R", "R");
        TestCornerCoordinateRoundTrip("F", "F");
        TestCornerCoordinateRoundTrip("R U F", "R", "U", "F");

        Debug.Log("CUBE TESTS - Corner coordinate checks complete.");
    }

    public static void RunCornerPdbTests()
    {
        byte[] cornerPdb = CornerPDB.GenerateArray(3);

        TestCornerPdbDepth(cornerPdb, "solved", 0);
        TestCornerPdbDepth(cornerPdb, "R", 1, "R");
        TestCornerPdbMaxDepth(cornerPdb, "R U", 2, "R", "U");
        TestCornerPdbMaxDepth(cornerPdb, "R U F", 3, "R", "U", "F");

        Debug.Log("CUBE TESTS - Tiny corner PDB visited states: " + CornerPDB.CountVisited(cornerPdb));
        Debug.Log("CUBE TESTS - Tiny corner PDB checks complete.");
    }

    private static void TestMoveAndInverse(string move, string inverseMove)
    {
        CubeStateData cube = CubeState.CreateSolvedState();

        MoveProcessor.ApplyMove(cube, move);
        MoveProcessor.ApplyMove(cube, inverseMove);

        Debug.Log("CUBE TESTS - " + move + " + " + inverseMove + " solved: " + CubeStateUtility.IsSolved(cube));
    }

    private static void ApplyMovesWithoutHistory(CubeStateData cube, params string[] moves)
    {
        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(cube, move, false);
        }
    }

    private static void TestCornerCoordinateRoundTrip(string testName, params string[] moves)
    {
        CubeStateData original = CubeState.CreateSolvedState();
        ApplyMovesWithoutHistory(original, moves);

        int originalIndex = CornerCoordinate.GetIndex(original);
        CubeStateData restored = CornerCoordinate.GetStateFromIndex(originalIndex);
        int restoredIndex = CornerCoordinate.GetIndex(restored);

        Debug.Log("CUBE TESTS - Corner coordinate round trip " + testName + ": "
            + (originalIndex == restoredIndex));
    }

    private static void TestCornerPdbDepth(byte[] cornerPdb, string testName, byte expectedDepth, params string[] moves)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMovesWithoutHistory(state, moves);

        int index = CornerCoordinate.GetIndex(state);
        byte actualDepth = cornerPdb[index];

        Debug.Log("CUBE TESTS - Tiny corner PDB " + testName + " depth is " + expectedDepth + ": "
            + (actualDepth == expectedDepth));
    }

    private static void TestCornerPdbMaxDepth(byte[] cornerPdb, string testName, byte maxExpectedDepth, params string[] moves)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMovesWithoutHistory(state, moves);

        int index = CornerCoordinate.GetIndex(state);
        byte actualDepth = cornerPdb[index];

        Debug.Log("CUBE TESTS - Tiny corner PDB " + testName + " depth <= " + maxExpectedDepth + ": "
            + (actualDepth != CornerPDB.Unvisited && actualDepth <= maxExpectedDepth));
    }

    private static void TestMoveCount(string previousMove, int expectedCount)
    {
        List<string> validMoves = MoveGenerator.GetValidMoves(previousMove);
        bool countIsCorrect = validMoves.Count == expectedCount;

        Debug.Log("CUBE TESTS - Previous " + FormatMove(previousMove) + " valid move count is " + expectedCount + ": " + countIsCorrect);
    }

    private static void TestBannedFace(string previousMove, string bannedFace)
    {
        List<string> validMoves = MoveGenerator.GetValidMoves(previousMove);
        bool faceIsBanned = !validMoves.Exists(move => move.StartsWith(bannedFace));

        Debug.Log("CUBE TESTS - Previous " + previousMove + " bans " + bannedFace + " moves: " + faceIsBanned);
    }

    private static string FormatMove(string move)
    {
        return string.IsNullOrEmpty(move) ? "none" : move;
    }
}
