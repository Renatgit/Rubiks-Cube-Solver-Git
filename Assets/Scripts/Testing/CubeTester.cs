using UnityEngine;
using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;

public class CubeTester : MonoBehaviour
{
    private const bool RunSolverStateMoveTestAutomatically = false;
    private const bool RunEdgeGroupPdbSmokeTestAutomatically = false;
    private const bool RunFullEdgeGroupAPdbGenerationAutomatically = false;
    private const bool RunFullEdgeGroupBPdbGenerationAutomatically = false;
    private const bool RunIDAStarSolveTestsAutomatically = false;
    private const bool RunCornerPdbSmokeTestAutomatically = false;
    private const bool RunFullCornerPdbGenerationAutomatically = false;
    private const int EdgeGroupPdbTestDepth = 8;
    private const int CornerPdbTestDepth = 9; // Off
    private const int IDAStarTestMaxDepth = 12;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        if (RunSolverStateMoveTestAutomatically)
        {
            RunSolverStateMoveTest();
        }

        if (RunEdgeGroupPdbSmokeTestAutomatically)
        {
            RunEdgeGroupPdbSmokeTest();
        }

        if (RunFullEdgeGroupAPdbGenerationAutomatically)
        {
            GenerateAndSaveFullEdgeGroupAPdb();
        }

        if (RunFullEdgeGroupBPdbGenerationAutomatically)
        {
            GenerateAndSaveFullEdgeGroupBPdb();
        }

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

    public static void RunSolverStateMoveTest()
    {
        string[][] testSequences =
        {
            new string[] { "R" },
            new string[] { "R", "U" },
            new string[] { "R", "U", "F" },
            new string[] { "R", "U2", "F'", "L" },
            new string[] { "B", "D", "R'", "F2", "L", "U" }
        };

        bool allMatch = true;

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
                allMatch = false;
                Debug.Log("CUBE TESTS - SolverState mismatch on sequence: " + string.Join(", ", sequence));
            }
        }

        Debug.Log("CUBE TESTS - SolverState move pipeline matches CubeStateData: " + allMatch);
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

    public static void RunEdgeGroupPdbSmokeTest()
    {
        int[] groupA = { 0, 1, 2, 3, 4, 5 };
        int[] groupB = { 6, 7, 8, 9, 10, 11 };

        Debug.Log("CUBE TESTS - Edge group A fast move matches full state: "
            + EdgeGroupFastMoveMatchesFullState(groupA));
        Debug.Log("CUBE TESTS - Edge group B fast move matches full state: "
            + EdgeGroupFastMoveMatchesFullState(groupB));

        byte[] edgePdb = EdgeGroupPDB.GenerateArray(EdgeGroupPdbTestDepth, groupA);
        byte[] loadedPdb = SaveAndLoadEdgeGroupPdb(edgePdb);

        Debug.Log("CUBE TESTS - Edge group PDB generated to depth " + EdgeGroupPDB.LastGenerationStats.MaxDepth
            + " in " + EdgeGroupPDB.LastGenerationStats.ElapsedMilliseconds + "ms");
        LogEdgeGroupPdbDepthCounts();
        Debug.Log("CUBE TESTS - Edge group PDB reached requested depth: "
            + (EdgeGroupPDB.LastGenerationStats.MaxDepth == EdgeGroupPdbTestDepth));
        Debug.Log("CUBE TESTS - Edge group PDB solved depth is 0: "
            + (GetEdgeGroupPdbDepth(loadedPdb, groupA) == 0));
        Debug.Log("CUBE TESTS - Edge group PDB R depth is stored: "
            + (GetEdgeGroupPdbDepth(loadedPdb, groupA, "R") != EdgeGroupPDB.Unvisited));
        Debug.Log("CUBE TESTS - Edge group PDB R U F depth <= 3: "
            + (GetEdgeGroupPdbDepth(loadedPdb, groupA, "R", "U", "F") <= 3));
        Debug.Log("CUBE TESTS - Edge group PDB save/load visited count matches: "
            + (EdgeGroupPDB.CountVisited(loadedPdb) == EdgeGroupPDB.CountVisited(edgePdb)));
        Debug.Log("CUBE TESTS - Edge group PDB visited states: " + EdgeGroupPDB.CountVisited(loadedPdb));
        Debug.Log("CUBE TESTS - Edge group PDB file path: " + GetEdgeGroupPdbTestFilePath());
    }

    public static void GenerateAndSaveFullEdgeGroupAPdb()
    {
        int[] groupA = { 0, 1, 2, 3, 4, 5 };
        string filePath = GetFullEdgeGroupAPdbFilePath();
        string markerPath = GetFullEdgeGroupAPdbMarkerFilePath();

        Debug.Log("CUBE TESTS - Full edge group A PDB generation started");
        WriteGenerationMarker(markerPath, "Full edge group A PDB generation started", filePath);

        byte[] edgePdb = EdgeGroupPDB.GenerateFull(groupA);

        WriteGenerationMarker(markerPath, "Full edge group A PDB generation finished, saving file", filePath);

        EdgeGroupPDB.Save(edgePdb, filePath);
        byte[] loadedPdb = EdgeGroupPDB.Load(filePath);

        WriteGenerationMarker(markerPath, "Full edge group A PDB saved and loaded successfully", filePath);

        Debug.Log("CUBE TESTS - Full edge group A PDB generated in "
            + EdgeGroupPDB.LastGenerationStats.ElapsedMilliseconds + "ms");
        Debug.Log("CUBE TESTS - Full edge group A PDB max depth: " + EdgeGroupPDB.LastGenerationStats.MaxDepth);
        LogEdgeGroupPdbDepthCounts();
        Debug.Log("CUBE TESTS - Full edge group A PDB saved file exists: " + System.IO.File.Exists(filePath));
        Debug.Log("CUBE TESTS - Full edge group A PDB loaded length is correct: "
            + (loadedPdb.Length == EdgeGroupPDB.EdgeGroupStateCount));
        Debug.Log("CUBE TESTS - Full edge group A PDB visited every edge group state: "
            + (EdgeGroupPDB.LastGenerationStats.VisitedStates == EdgeGroupPDB.EdgeGroupStateCount));
        Debug.Log("CUBE TESTS - Full edge group A PDB loaded solved depth is 0: "
            + (GetEdgeGroupPdbDepth(loadedPdb, groupA) == 0));
        Debug.Log("CUBE TESTS - Full edge group A PDB file path: " + filePath);

        System.IO.File.Delete(markerPath);
    }

    public static void GenerateAndSaveFullEdgeGroupBPdb()
    {
        int[] groupB = { 6, 7, 8, 9, 10, 11 };
        string filePath = GetFullEdgeGroupBPdbFilePath();
        string markerPath = GetFullEdgeGroupBPdbMarkerFilePath();

        Debug.Log("CUBE TESTS - Full edge group B PDB generation started");
        WriteGenerationMarker(markerPath, "Full edge group B PDB generation started", filePath);

        byte[] edgePdb = EdgeGroupPDB.GenerateFull(groupB);

        WriteGenerationMarker(markerPath, "Full edge group B PDB generation finished, saving file", filePath);

        EdgeGroupPDB.Save(edgePdb, filePath);
        byte[] loadedPdb = EdgeGroupPDB.Load(filePath);

        WriteGenerationMarker(markerPath, "Full edge group B PDB saved and loaded successfully", filePath);

        Debug.Log("CUBE TESTS - Full edge group B PDB generated in "
            + EdgeGroupPDB.LastGenerationStats.ElapsedMilliseconds + "ms");
        Debug.Log("CUBE TESTS - Full edge group B PDB max depth: " + EdgeGroupPDB.LastGenerationStats.MaxDepth);
        LogEdgeGroupPdbDepthCounts();
        Debug.Log("CUBE TESTS - Full edge group B PDB saved file exists: " + System.IO.File.Exists(filePath));
        Debug.Log("CUBE TESTS - Full edge group B PDB loaded length is correct: "
            + (loadedPdb.Length == EdgeGroupPDB.EdgeGroupStateCount));
        Debug.Log("CUBE TESTS - Full edge group B PDB visited every edge group state: "
            + (EdgeGroupPDB.LastGenerationStats.VisitedStates == EdgeGroupPDB.EdgeGroupStateCount));
        Debug.Log("CUBE TESTS - Full edge group B PDB loaded solved depth is 0: "
            + (GetEdgeGroupPdbDepth(loadedPdb, groupB) == 0));
        Debug.Log("CUBE TESTS - Full edge group B PDB file path: " + filePath);

        System.IO.File.Delete(markerPath);
    }

    private static bool EdgeGroupFastMoveMatchesFullState(int[] trackedEdges)
    {
        string[][] testSequences =
        {
            new string[] { "R" },
            new string[] { "F" },
            new string[] { "R", "U", "F" },
            new string[] { "R", "U2", "F'", "L" },
            new string[] { "B", "D", "R'", "F2" }
        };

        foreach (string[] sequence in testSequences)
        {
            CubeStateData fullState = CubeState.CreateSolvedState();
            int fastIndex = EdgeGroupCoordinate.GetIndex(
                fullState.fullEdgePermutation.ToArray(),
                fullState.fullEdgeOrientation.ToArray(),
                trackedEdges);

            foreach (string move in sequence)
            {
                MoveProcessor.ApplyMove(fullState, move, false);
                fastIndex = MoveProcessor.ApplyEdgeGroupMoveToIndex(fastIndex, move, trackedEdges);
            }

            int fullStateIndex = EdgeGroupCoordinate.GetIndex(
                fullState.fullEdgePermutation.ToArray(),
                fullState.fullEdgeOrientation.ToArray(),
                trackedEdges);

            if (fastIndex != fullStateIndex)
            {
                Debug.Log("CUBE TESTS - Edge fast move mismatch on sequence: " + string.Join(", ", sequence));
                return false;
            }
        }

        return true;
    }

    public static void RunIDAStarSolveTests()
    {
        Debug.Log("CUBE TESTS - IDA* compact solve test started, max depth " + IDAStarTestMaxDepth);
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

        IDAStarSearchStats stats = IDAStarSolver.LastSearchStats;
        string solutionText = solution == null ? "null" : string.Join(", ", solution);
        int solutionLength = solution == null ? -1 : solution.Count;

        Debug.Log("CUBE TESTS - IDA* " + testName
            + " | solved: " + solved
            + " | length: " + solutionLength
            + " | time: " + stats.ElapsedMilliseconds + "ms"
            + " | nodes: " + stats.NodesVisited
            + " | bounds: " + stats.InitialBound + "->" + stats.FinalBound
            + " | solution: " + solutionText);
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

        WriteGenerationMarker(markerPath, "Full corner PDB generation started", filePath);

        byte[] cornerPdb = CornerPDB.GenerateFull();

        WriteGenerationMarker(markerPath, "Full corner PDB generation finished, saving file", filePath);

        CornerPDB.Save(cornerPdb, filePath);
        byte[] loadedPdb = CornerPDB.Load(filePath);

        WriteGenerationMarker(markerPath, "Full corner PDB saved and loaded successfully", filePath);

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

    private static byte[] SaveAndLoadEdgeGroupPdb(byte[] edgePdb)
    {
        string filePath = GetEdgeGroupPdbTestFilePath();

        EdgeGroupPDB.Save(edgePdb, filePath);

        return EdgeGroupPDB.Load(filePath);
    }

    private static void LogEdgeGroupPdbDepthCounts()
    {
        for (int depth = 0; depth < EdgeGroupPDB.LastGenerationStats.DepthCounts.Length; depth++)
        {
            Debug.Log("CUBE TESTS - Edge group PDB depth " + depth
                + " new states: " + EdgeGroupPDB.LastGenerationStats.DepthCounts[depth]);
        }
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

    private static string GetEdgeGroupPdbTestFilePath()
    {
        return Application.dataPath + "/PatternDatabase/edge_group_a_test_depth" + EdgeGroupPdbTestDepth + ".pdb";
    }

    private static string GetFullEdgeGroupAPdbFilePath()
    {
        return Application.dataPath + "/PatternDatabase/edge_group_a.pdb";
    }

    private static string GetFullEdgeGroupAPdbMarkerFilePath()
    {
        return Application.dataPath + "/PatternDatabase/edge_group_a_generation_in_progress.txt";
    }

    private static string GetFullEdgeGroupBPdbFilePath()
    {
        return Application.dataPath + "/PatternDatabase/edge_group_b.pdb";
    }

    private static string GetFullEdgeGroupBPdbMarkerFilePath()
    {
        return Application.dataPath + "/PatternDatabase/edge_group_b_generation_in_progress.txt";
    }

    private static string GetFullCornerPdbFilePath()
    {
        return Application.dataPath + "/PatternDatabase/corner.pdb";
    }

    private static string GetFullCornerPdbMarkerFilePath()
    {
        return Application.dataPath + "/PatternDatabase/corner_generation_in_progress.txt";
    }

    private static void WriteGenerationMarker(string markerPath, string message, string targetFilePath)
    {
        string folderPath = System.IO.Path.GetDirectoryName(markerPath);
        if (!string.IsNullOrEmpty(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        string text = message + "\n"
            + "Time: " + System.DateTime.Now + "\n"
            + "Target file: " + targetFilePath + "\n";

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

    private static byte GetEdgeGroupPdbDepth(byte[] edgePdb, int[] trackedEdges, params string[] moves)
    {
        CubeStateData state = CubeState.CreateSolvedState();

        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(state, move, false);
        }

        int index = EdgeGroupCoordinate.GetIndex(
            state.fullEdgePermutation.ToArray(),
            state.fullEdgeOrientation.ToArray(),
            trackedEdges);

        return edgePdb[index];
    }
}
