using Assets.Scripts.Core;
using Assets.Scripts.Solver.Coordinates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Assets.Scripts.Solver.PatternDatabases
{
    public class EdgeGroupPdbGenerationStats
    {
        public int MaxDepth;
        public int VisitedStates;
        public long ElapsedMilliseconds;
        public int[] DepthCounts;
    }

    public static class EdgeGroupPDB
    {
        public const int EdgeGroupStateCount = 42577920;
        public const byte Unvisited = 255;

        public static EdgeGroupPdbGenerationStats LastGenerationStats { get; private set; }

        private class PdbNode
        {
            public int EdgeGroupIndex;
            public int Depth;
            public string PreviousMove;

            public PdbNode(int edgeGroupIndex, int depth, string previousMove)
            {
                EdgeGroupIndex = edgeGroupIndex;
                Depth = depth;
                PreviousMove = previousMove;
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

        public static byte[] Load(string filePath)
        {
            byte[] database = File.ReadAllBytes(filePath);

            if (database.Length != EdgeGroupStateCount)
            {
                throw new Exception($"Invalid edge group PDB size! \nLoaded Size: {database.Length} bytes");
            }

            return database;
        }

        public static byte[] GenerateArray(int maxDepth, int[] trackedEdges)
        {
            return Generate(maxDepth, true, trackedEdges);
        }

        public static byte[] GenerateFull(int[] trackedEdges)
        {
            return Generate(0, false, trackedEdges);
        }

        private static byte[] Generate(int maxDepth, bool useDepthLimit, int[] trackedEdges)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<int> depthCounts = new List<int>();

            byte[] database = new byte[EdgeGroupStateCount];
            for (int i = 0; i < database.Length; i++)
            {
                database[i] = Unvisited;
            }

            Queue<PdbNode> queue = new Queue<PdbNode>();
            CubeStateData solved = CubeState.CreateSolvedState();
            int solvedIndex = EdgeGroupCoordinate.GetIndex(
                solved.fullEdgePermutation.ToArray(),
                solved.fullEdgeOrientation.ToArray(),
                trackedEdges);

            database[solvedIndex] = 0;
            AddDepthCount(depthCounts, 0);
            int visitedStates = 1;
            queue.Enqueue(new PdbNode(solvedIndex, 0, null));

            while (queue.Count > 0)
            {
                PdbNode current = queue.Dequeue();

                if (useDepthLimit && current.Depth >= maxDepth)
                {
                    continue;
                }

                foreach (string move in MoveGenerator.GetValidMoves(current.PreviousMove))
                {
                    int childIndex = MoveProcessor.ApplyEdgeGroupMoveToIndex(
                        current.EdgeGroupIndex,
                        move,
                        trackedEdges);

                    if (database[childIndex] != Unvisited)
                    {
                        continue;
                    }

                    int childDepth = current.Depth + 1;
                    database[childIndex] = (byte)childDepth;
                    AddDepthCount(depthCounts, childDepth);
                    visitedStates++;
                    queue.Enqueue(new PdbNode(childIndex, childDepth, move));
                }
            }

            stopwatch.Stop();
            LastGenerationStats = new EdgeGroupPdbGenerationStats
            {
                MaxDepth = depthCounts.Count - 1,
                VisitedStates = visitedStates,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DepthCounts = depthCounts.ToArray()
            };

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
    }
}
