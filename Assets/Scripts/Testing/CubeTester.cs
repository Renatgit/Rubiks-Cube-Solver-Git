using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Core;

public class CubeTester : MonoBehaviour
{
    private const bool RunPruningTestsAutomatically = false;

    [SerializeField] private bool runMoveRegressionOnStart = false;
    [SerializeField] private bool runMovePruningOnStart = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        if (RunPruningTestsAutomatically)
        {
            RunMovePruningTests();
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

    private static void TestMoveAndInverse(string move, string inverseMove)
    {
        CubeStateData cube = CubeState.CreateSolvedState();

        MoveProcessor.ApplyMove(cube, move);
        MoveProcessor.ApplyMove(cube, inverseMove);

        Debug.Log("CUBE TESTS - " + move + " + " + inverseMove + " solved: " + CubeStateUtility.IsSolved(cube));
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
