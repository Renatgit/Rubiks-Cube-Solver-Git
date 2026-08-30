using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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

        public static Phase1PdbGenerationStats LastGenerationStats { get; private set; }

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
