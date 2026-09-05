using Assets.Scripts.Core;
using Assets.Scripts.Solver.Coordinates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Solver.PatternDatabases
{
    public class Phase1PdbGenerationStats
    {
        public int MaxDepth;
        public int VisitedStates;
        public long ElapsedMilliseconds;
        public int[] DepthCounts;
    }

    public static class Phase1PDB
    {
        public const byte Unvisited = 255;
        public const string CornerPermutationEdgeOrientationFileName =
            "corner_permutation_edge_orientation.pdb";
        public const string CornerPermutationEdgeOrientationTemporaryFileName =
            CornerPermutationEdgeOrientationFileName + ".tmp";
        public const string CornerPermutationEdgeOrientationProgressFileName =
            CornerPermutationEdgeOrientationFileName + ".inprogress";

        private const int CornerPermutationEdgeOrientationBackwardSearchStartDepth = 7;

        public static Phase1PdbGenerationStats LastGenerationStats { get; private set; }

        public static string CornerPermutationEdgeOrientationFilePath
        {
            get
            {
                return Application.dataPath
                    + "/PatternDatabase/"
                    + CornerPermutationEdgeOrientationFileName;
            }
        }

        public static string CornerPermutationEdgeOrientationTemporaryFilePath
        {
            get
            {
                return Application.dataPath
                    + "/PatternDatabase/"
                    + CornerPermutationEdgeOrientationTemporaryFileName;
            }
        }

        public static string CornerPermutationEdgeOrientationProgressFilePath
        {
            get
            {
                return Application.dataPath
                    + "/PatternDatabase/"
                    + CornerPermutationEdgeOrientationProgressFileName;
            }
        }

        private class PdbNode
        {
            public SolverStateData State;
            public int Depth;
            public string PreviousMove;

            public PdbNode(SolverStateData state, int depth, string previousMove)
            {
                State = state;
                Depth = depth;
                PreviousMove = previousMove;
            }
        }

        public static byte[] GenerateCornerSliceArray(int maxDepth)
        {
            return Generate(maxDepth, true, Phase1Coordinate.CornerSliceCount, Phase1Coordinate.GetCornerSliceIndex);
        }

        public static byte[] GenerateEdgeSliceArray(int maxDepth)
        {
            return Generate(maxDepth, true, Phase1Coordinate.EdgeSliceCount, Phase1Coordinate.GetEdgeSliceIndex);
        }

        public static byte[] GenerateFullCornerSlice()
        {
            return Generate(0, false, Phase1Coordinate.CornerSliceCount, Phase1Coordinate.GetCornerSliceIndex);
        }

        public static byte[] GenerateFullEdgeSlice()
        {
            return Generate(0, false, Phase1Coordinate.EdgeSliceCount, Phase1Coordinate.GetEdgeSliceIndex);
        }

        public static byte[] GenerateFullCornerPermutationSlicePosition()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Phase1MoveTables.BuildIfNeeded();

            byte[] database = new byte[Phase1Coordinate.CornerPermutationSlicePositionCount];
            Array.Fill(database, Unvisited);

            int[] queue = new int[database.Length];
            int solvedSlicePositionIndex = Phase1Coordinate.GetSlicePositionIndexFromPositions(
                new int[] { 8, 9, 10, 11 });
            int solvedIndex = solvedSlicePositionIndex;
            int queueHead = 0;
            int queueTail = 1;
            int maxDepth = 0;
            List<int> depthCounts = new List<int> { 1 };

            database[solvedIndex] = 0;
            queue[0] = solvedIndex;

            while (queueHead < queueTail)
            {
                int currentIndex = queue[queueHead++];
                int currentDepth = database[currentIndex];
                int cornerPermutationIndex =
                    currentIndex / Phase1Coordinate.SlicePositionCount;
                int slicePositionIndex =
                    currentIndex % Phase1Coordinate.SlicePositionCount;

                for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                {
                    int movedCornerPermutationIndex =
                        Phase1MoveTables.GetCornerPermutationAfterMovePrepared(
                            cornerPermutationIndex,
                            moveId);
                    int movedSlicePositionIndex =
                        Phase1MoveTables.GetSlicePositionAfterMovePrepared(
                            slicePositionIndex,
                            moveId);
                    int childIndex =
                        movedCornerPermutationIndex * Phase1Coordinate.SlicePositionCount
                        + movedSlicePositionIndex;

                    if (database[childIndex] != Unvisited)
                    {
                        continue;
                    }

                    int childDepth = currentDepth + 1;
                    database[childIndex] = (byte)childDepth;
                    queue[queueTail++] = childIndex;
                    AddDepthCount(depthCounts, childDepth);

                    if (childDepth > maxDepth)
                    {
                        maxDepth = childDepth;
                    }
                }
            }

            if (queueTail != database.Length)
            {
                throw new InvalidOperationException(
                    "Corner-permutation/slice-position PDB did not reach every entry");
            }

            stopwatch.Stop();
            LastGenerationStats = new Phase1PdbGenerationStats
            {
                MaxDepth = maxDepth,
                VisitedStates = queueTail,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DepthCounts = depthCounts.ToArray()
            };

            return database;
        }

        public static byte[] GenerateFullCornerPermutationEdgeOrientation(
            Action<string> reportProgress = null)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Phase1MoveTables.BuildIfNeeded();

            byte[] database = new byte[Phase1Coordinate.CornerPermutationEdgeOrientationCount];
            Array.Fill(database, Unvisited);

            List<int> depthCounts = new List<int> { 1 };
            database[0] = 0;
            int visitedStates = 1;
            int depth = 0;

            reportProgress?.Invoke(
                "Corner-permutation/edge-orientation PDB depth 0 states: 1 / "
                + database.Length);

            while (visitedStates < database.Length)
            {
                int nextDepth = depth + 1;
                int newStates = depth < CornerPermutationEdgeOrientationBackwardSearchStartDepth
                    ? ExpandCornerPermutationEdgeOrientationForward(database, depth, nextDepth)
                    : ExpandCornerPermutationEdgeOrientationBackward(database, depth, nextDepth);

                if (newStates == 0)
                {
                    throw new InvalidOperationException(
                        "Corner-permutation/edge-orientation PDB generation stopped before every entry was reached");
                }

                visitedStates += newStates;
                depthCounts.Add(newStates);
                depth = nextDepth;

                reportProgress?.Invoke(
                    "Corner-permutation/edge-orientation PDB depth " + depth
                    + " new states: " + newStates
                    + " | total: " + visitedStates + " / " + database.Length);
            }

            stopwatch.Stop();
            LastGenerationStats = new Phase1PdbGenerationStats
            {
                MaxDepth = depth,
                VisitedStates = visitedStates,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DepthCounts = depthCounts.ToArray()
            };

            return database;
        }

        public static byte[] GenerateFullCornerPermutationEdgeOrientationAndSave(
            Action<string> reportProgress = null)
        {
            string folderPath = Path.GetDirectoryName(CornerPermutationEdgeOrientationFilePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (HasValidCornerPermutationEdgeOrientationSavedFile())
            {
                File.Delete(CornerPermutationEdgeOrientationTemporaryFilePath);
                File.Delete(CornerPermutationEdgeOrientationProgressFilePath);
                reportProgress?.Invoke(
                    "Corner-permutation/edge-orientation PDB already exists and has the correct size");
                return LoadCornerPermutationEdgeOrientation(
                    CornerPermutationEdgeOrientationFilePath);
            }

            File.Delete(CornerPermutationEdgeOrientationTemporaryFilePath);
            WriteCornerPermutationEdgeOrientationProgress(
                "Corner-permutation/edge-orientation PDB generation started");

            try
            {
                byte[] database = GenerateFullCornerPermutationEdgeOrientation(message =>
                {
                    WriteCornerPermutationEdgeOrientationProgress(message);
                    reportProgress?.Invoke(message);
                });

                WriteCornerPermutationEdgeOrientationProgress(
                    "Writing corner-permutation/edge-orientation PDB temporary file");
                SaveCornerPermutationEdgeOrientation(
                    database,
                    CornerPermutationEdgeOrientationTemporaryFilePath);
                ValidateCornerPermutationEdgeOrientationSavedFile(
                    CornerPermutationEdgeOrientationTemporaryFilePath);

                if (File.Exists(CornerPermutationEdgeOrientationFilePath))
                {
                    File.Replace(
                        CornerPermutationEdgeOrientationTemporaryFilePath,
                        CornerPermutationEdgeOrientationFilePath,
                        null);
                }
                else
                {
                    File.Move(
                        CornerPermutationEdgeOrientationTemporaryFilePath,
                        CornerPermutationEdgeOrientationFilePath);
                }

                ValidateCornerPermutationEdgeOrientationSavedFile(
                    CornerPermutationEdgeOrientationFilePath);
                File.Delete(CornerPermutationEdgeOrientationProgressFilePath);
                reportProgress?.Invoke(
                    "Corner-permutation/edge-orientation PDB saved and validated");
                return database;
            }
            catch (Exception exception)
            {
                try
                {
                    WriteCornerPermutationEdgeOrientationProgress(
                        "Generation failed: " + exception);
                }
                catch
                {
                }

                throw;
            }
        }

        public static void Save(byte[] database, string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            File.WriteAllBytes(filePath, database);
        }

        public static byte[] LoadCornerSlice(string filePath)
        {
            return Load(filePath, Phase1Coordinate.CornerSliceCount, "corner-slice");
        }

        public static byte[] LoadEdgeSlice(string filePath)
        {
            return Load(filePath, Phase1Coordinate.EdgeSliceCount, "edge-slice");
        }

        public static byte[] LoadCornerPermutationSlicePosition(string filePath)
        {
            return Load(
                filePath,
                Phase1Coordinate.CornerPermutationSlicePositionCount,
                "corner-permutation/slice-position");
        }

        public static byte[] LoadCornerPermutationEdgeOrientation(string filePath)
        {
            return Load(
                filePath,
                Phase1Coordinate.CornerPermutationEdgeOrientationCount,
                "corner-permutation/edge-orientation");
        }

        public static bool HasValidCornerPermutationEdgeOrientationSavedFile()
        {
            return File.Exists(CornerPermutationEdgeOrientationFilePath)
                && new FileInfo(CornerPermutationEdgeOrientationFilePath).Length
                    == Phase1Coordinate.CornerPermutationEdgeOrientationCount;
        }

        public static int CountVisited(byte[] database)
        {
            int visitedCount = 0;

            for (int i = 0; i < database.Length; i++)
            {
                if (database[i] != Unvisited)
                {
                    visitedCount++;
                }
            }

            return visitedCount;
        }

        private static byte[] Generate(
            int maxDepth,
            bool useDepthLimit,
            int databaseSize,
            Func<SolverStateData, int> getIndex)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<int> depthCounts = new List<int>();

            byte[] database = new byte[databaseSize];
            for (int i = 0; i < database.Length; i++)
            {
                database[i] = Unvisited;
            }

            SolverStateData solved = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
            int solvedIndex = getIndex(solved);

            Queue<PdbNode> queue = new Queue<PdbNode>();
            database[solvedIndex] = 0;
            AddDepthCount(depthCounts, 0);
            int visitedStates = 1;
            queue.Enqueue(new PdbNode(solved, 0, null));

            while (queue.Count > 0)
            {
                PdbNode current = queue.Dequeue();

                if (useDepthLimit && current.Depth >= maxDepth)
                {
                    continue;
                }

                foreach (string move in MoveGenerator.GetValidMoves(current.PreviousMove))
                {
                    SolverStateData child = current.State.Clone();
                    MoveProcessor.ApplyMove(child, move);

                    int childIndex = getIndex(child);

                    if (database[childIndex] != Unvisited)
                    {
                        continue;
                    }

                    int childDepth = current.Depth + 1;
                    database[childIndex] = (byte)childDepth;
                    AddDepthCount(depthCounts, childDepth);
                    visitedStates++;
                    queue.Enqueue(new PdbNode(child, childDepth, move));
                }
            }

            stopwatch.Stop();
            LastGenerationStats = new Phase1PdbGenerationStats
            {
                MaxDepth = depthCounts.Count - 1,
                VisitedStates = visitedStates,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DepthCounts = depthCounts.ToArray()
            };

            return database;
        }

        private static int ExpandCornerPermutationEdgeOrientationForward(
            byte[] database,
            int depth,
            int nextDepth)
        {
            int newStates = 0;
            int edgeOrientationCount = Phase1Coordinate.EdgeOrientationCount;

            for (int cornerPermutationIndex = 0;
                cornerPermutationIndex < Phase2Coordinate.CornerPermutationCount;
                cornerPermutationIndex++)
            {
                int cornerBaseIndex = cornerPermutationIndex * edgeOrientationCount;

                for (int edgeOrientationIndex = 0;
                    edgeOrientationIndex < edgeOrientationCount;
                    edgeOrientationIndex++)
                {
                    int index = cornerBaseIndex + edgeOrientationIndex;
                    if (database[index] != depth)
                    {
                        continue;
                    }

                    for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                    {
                        int movedCornerPermutationIndex =
                            Phase1MoveTables.GetCornerPermutationAfterMovePrepared(
                                cornerPermutationIndex,
                                moveId);
                        int movedEdgeOrientationIndex =
                            Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(
                                edgeOrientationIndex,
                                moveId);
                        int childIndex =
                            movedCornerPermutationIndex * edgeOrientationCount
                            + movedEdgeOrientationIndex;

                        if (database[childIndex] != Unvisited)
                        {
                            continue;
                        }

                        database[childIndex] = (byte)nextDepth;
                        newStates++;
                    }
                }
            }

            return newStates;
        }

        private static int ExpandCornerPermutationEdgeOrientationBackward(
            byte[] database,
            int depth,
            int nextDepth)
        {
            int newStates = 0;
            int edgeOrientationCount = Phase1Coordinate.EdgeOrientationCount;

            for (int cornerPermutationIndex = 0;
                cornerPermutationIndex < Phase2Coordinate.CornerPermutationCount;
                cornerPermutationIndex++)
            {
                int cornerBaseIndex = cornerPermutationIndex * edgeOrientationCount;

                for (int edgeOrientationIndex = 0;
                    edgeOrientationIndex < edgeOrientationCount;
                    edgeOrientationIndex++)
                {
                    int index = cornerBaseIndex + edgeOrientationIndex;
                    if (database[index] != Unvisited)
                    {
                        continue;
                    }

                    for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                    {
                        int movedCornerPermutationIndex =
                            Phase1MoveTables.GetCornerPermutationAfterMovePrepared(
                                cornerPermutationIndex,
                                moveId);
                        int movedEdgeOrientationIndex =
                            Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(
                                edgeOrientationIndex,
                                moveId);
                        int neighborIndex =
                            movedCornerPermutationIndex * edgeOrientationCount
                            + movedEdgeOrientationIndex;

                        if (database[neighborIndex] != depth)
                        {
                            continue;
                        }

                        database[index] = (byte)nextDepth;
                        newStates++;
                        break;
                    }
                }
            }

            return newStates;
        }

        private static void SaveCornerPermutationEdgeOrientation(
            byte[] database,
            string filePath)
        {
            if (database == null
                || database.Length != Phase1Coordinate.CornerPermutationEdgeOrientationCount)
            {
                throw new ArgumentException(
                    "Invalid corner-permutation/edge-orientation PDB size",
                    nameof(database));
            }

            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                1024 * 1024,
                FileOptions.WriteThrough))
            {
                stream.Write(database, 0, database.Length);
                stream.Flush(true);
            }
        }

        private static void ValidateCornerPermutationEdgeOrientationSavedFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Corner-permutation/edge-orientation PDB was not written",
                    filePath);
            }

            long fileLength = new FileInfo(filePath).Length;
            if (fileLength != Phase1Coordinate.CornerPermutationEdgeOrientationCount)
            {
                throw new InvalidDataException(
                    "Invalid corner-permutation/edge-orientation PDB file size: "
                    + fileLength + " bytes");
            }
        }

        private static void WriteCornerPermutationEdgeOrientationProgress(string message)
        {
            File.WriteAllText(
                CornerPermutationEdgeOrientationProgressFilePath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + Environment.NewLine
                    + message);
        }

        private static byte[] Load(string filePath, int expectedSize, string databaseName)
        {
            byte[] database = File.ReadAllBytes(filePath);

            if (database.Length != expectedSize)
            {
                throw new Exception("Invalid " + databaseName + " Phase 1 PDB size! Loaded Size: "
                    + database.Length + " bytes");
            }

            return database;
        }

        private static void AddDepthCount(List<int> depthCounts, int depth)
        {
            while (depthCounts.Count <= depth)
            {
                depthCounts.Add(0);
            }

            depthCounts[depth]++;
        }
    }
}
