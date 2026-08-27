using UnityEngine;
using System.Linq;
using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;

public class CubeTester : MonoBehaviour
{
    private const bool RunIDAStarSolveTestsAutomatically = true;
    private const bool RunCornerPdbSmokeTestAutomatically = false;
    private const bool RunFullCornerPdbGenerationAutomatically = false;
    private const int CornerPdbTestDepth = 9;
    private const int IDAStarTestMaxDepth = 10;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        if (RunIDAStarSolveTestsAutomatically)
        {
            RunIDAStarSolveTests();
        }

        if (RunCornerPdbSmokeTestAutomatically)
        {
            RunCornerPdbSmokeTest();
        }

        if (RunFullCornerPdbGenerationAutomatically)
        {
            GenerateAndSaveFullCornerPdb();
        }
    }

    public static void RunIDAStarSolveTests()
    {
        TestIDAStarScramble("R", "R");
        TestIDAStarScramble("R U", "R", "U");
        TestIDAStarScramble("R U F", "R", "U", "F");
        TestIDAStarScramble("R U2 F' L", "R", "U2", "F'", "L");
    }

    private static void TestIDAStarScramble(string testName, params string[] scramble)
    {
        CubeStateData state = CubeState.CreateSolvedState();

        foreach (string move in scramble)
        {
            MoveProcessor.ApplyMove(state, move, false);
        }

        System.Collections.Generic.List<string> solution = IDAStarSolver.Solve(state, IDAStarTestMaxDepth);

        bool solved = false;
        if (solution != null)
        {
            foreach (string move in solution)
            {
                MoveProcessor.ApplyMove(state, move, false);
            }

            solved = CubeStateUtility.IsSolved(state);
        }

        Debug.Log("CUBE TESTS - IDA* " + testName + " solution found: " + (solution != null));
        Debug.Log("CUBE TESTS - IDA* " + testName + " solution: "
            + (solution == null ? "null" : string.Join(", ", solution)));
        Debug.Log("CUBE TESTS - IDA* " + testName + " solution solves cube: " + solved);
        LogIDAStarStats();
    }

    private static void LogIDAStarStats()
    {
        IDAStarSearchStats stats = IDAStarSolver.LastSearchStats;

        Debug.Log("CUBE TESTS - IDA* stats initial bound: " + stats.InitialBound);
        Debug.Log("CUBE TESTS - IDA* stats final bound: " + stats.FinalBound);
        Debug.Log("CUBE TESTS - IDA* stats bound iterations: " + stats.BoundIterations);
        Debug.Log("CUBE TESTS - IDA* stats nodes visited: " + stats.NodesVisited);
        Debug.Log("CUBE TESTS - IDA* stats nodes expanded: " + stats.NodesExpanded);
        Debug.Log("CUBE TESTS - IDA* stats pruned by heuristic: " + stats.PrunedByHeuristic);
        Debug.Log("CUBE TESTS - IDA* stats max depth reached: " + stats.MaxDepthReached);
        Debug.Log("CUBE TESTS - IDA* stats time: " + stats.ElapsedMilliseconds + "ms");
    }

    public static void RunCornerPdbSmokeTest()
    {
        byte[] cornerPdb = CornerPDB.GenerateArray(CornerPdbTestDepth);
        byte[] loadedPdb = SaveAndLoadCornerPdb(cornerPdb);

        Debug.Log("CUBE TESTS - Corner PDB generated to depth " + CornerPDB.LastGenerationStats.MaxDepth
            + " in " + CornerPDB.LastGenerationStats.ElapsedMilliseconds + "ms");
        LogCornerPdbDepthCounts();
        Debug.Log("CUBE TESTS - Corner PDB solved depth is 0: "
            + (GetCornerPdbDepth(loadedPdb) == 0));
        Debug.Log("CUBE TESTS - Corner PDB R depth is 1: "
            + (GetCornerPdbDepth(loadedPdb, "R") == 1));
        Debug.Log("CUBE TESTS - Corner PDB R U depth <= 2: "
            + (GetCornerPdbDepth(loadedPdb, "R", "U") <= 2));
        Debug.Log("CUBE TESTS - Corner PDB R U F depth <= 3: "
            + (GetCornerPdbDepth(loadedPdb, "R", "U", "F") <= 3));
        Debug.Log("CUBE TESTS - Corner PDB save/load visited count matches: "
            + (CornerPDB.CountVisited(loadedPdb) == CornerPDB.CountVisited(cornerPdb)));
        Debug.Log("CUBE TESTS - Corner PDB visited states: " + CornerPDB.CountVisited(loadedPdb));
        Debug.Log("CUBE TESTS - Corner PDB file path: " + GetCornerPdbTestFilePath());
    }

    public static void GenerateAndSaveFullCornerPdb()
    {
        string filePath = GetFullCornerPdbFilePath();
        string markerPath = GetFullCornerPdbMarkerFilePath();

        WriteGenerationMarker(markerPath, "Full corner PDB generation started");

        byte[] cornerPdb = CornerPDB.GenerateFull();

        WriteGenerationMarker(markerPath, "Full corner PDB generation finished, saving file");

        CornerPDB.Save(cornerPdb, filePath);
        byte[] loadedPdb = CornerPDB.Load(filePath);

        WriteGenerationMarker(markerPath, "Full corner PDB saved and loaded successfully");

        Debug.Log("CUBE TESTS - Full corner PDB generated in "
            + CornerPDB.LastGenerationStats.ElapsedMilliseconds + "ms");
        Debug.Log("CUBE TESTS - Full corner PDB max depth: " + CornerPDB.LastGenerationStats.MaxDepth);
        LogCornerPdbDepthCounts();
        Debug.Log("CUBE TESTS - Full corner PDB saved file exists: " + System.IO.File.Exists(filePath));
        Debug.Log("CUBE TESTS - Full corner PDB loaded length is correct: "
            + (loadedPdb.Length == CornerPDB.CornerStateCount));
        Debug.Log("CUBE TESTS - Full corner PDB visited every corner state: "
            + (CornerPDB.LastGenerationStats.VisitedStates == CornerPDB.CornerStateCount));
        Debug.Log("CUBE TESTS - Full corner PDB loaded solved depth is 0: "
            + (GetCornerPdbDepth(loadedPdb) == 0));
        Debug.Log("CUBE TESTS - Full corner PDB file path: " + filePath);

        System.IO.File.Delete(markerPath);
    }

    private static byte[] SaveAndLoadCornerPdb(byte[] cornerPdb)
    {
        string filePath = GetCornerPdbTestFilePath();

        CornerPDB.Save(cornerPdb, filePath);

        return CornerPDB.Load(filePath);
    }

    private static void LogCornerPdbDepthCounts()
    {
        for (int depth = 0; depth < CornerPDB.LastGenerationStats.DepthCounts.Length; depth++)
        {
            Debug.Log("CUBE TESTS - Corner PDB depth " + depth
                + " new states: " + CornerPDB.LastGenerationStats.DepthCounts[depth]);
        }
    }

    private static string GetCornerPdbTestFilePath()
    {
        return Application.dataPath + "/PatternDatabase/corner_test_depth" + CornerPdbTestDepth + ".pdb";
    }

    private static string GetFullCornerPdbFilePath()
    {
        return Application.dataPath + "/PatternDatabase/corner.pdb";
    }

    private static string GetFullCornerPdbMarkerFilePath()
    {
        return Application.dataPath + "/PatternDatabase/corner_generation_in_progress.txt";
    }

    private static void WriteGenerationMarker(string markerPath, string message)
    {
        string folderPath = System.IO.Path.GetDirectoryName(markerPath);
        if (!string.IsNullOrEmpty(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        string text = message + "\n"
            + "Time: " + System.DateTime.Now + "\n"
            + "Target file: " + GetFullCornerPdbFilePath() + "\n";

        System.IO.File.WriteAllText(markerPath, text);
    }

    private static byte GetCornerPdbDepth(byte[] cornerPdb, params string[] moves)
    {
        CubeStateData state = CubeState.CreateSolvedState();

        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(state, move, false);
        }

        int index = CornerCoordinate.GetIndex(
            state.cornerPermutation.ToArray(),
            state.cornerOrientation.ToArray());
        return cornerPdb[index];
    }
}
