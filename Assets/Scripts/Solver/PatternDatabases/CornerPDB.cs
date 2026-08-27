using System;
using System.IO;
using Assets.Scripts.Solver.Coordinates;
using System.Collections.Generic;
using System.Diagnostics;
using Assets.Scripts.Core;

namespace Assets.Scripts.Solver.PatternDatabases
{
    public class CornerPdbGenerationStats
    {
        public int MaxDepth;
        public int VisitedStates;
        public long ElapsedMilliseconds;
        public int[] DepthCounts;
    }

    public static class CornerPDB
    {
        public const int CornerStateCount = 88179840;
        public const byte Unvisited = 255;
        public static CornerPdbGenerationStats LastGenerationStats { get; private set; }

        private class PdbNode
        {
            public int CornerIndex;
            public int Depth;
            public string PreviousMove;
            public PdbNode(int cornerIndex, int depth, string previousMove)
            {
                CornerIndex = cornerIndex;
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

            if (database.Length != CornerStateCount)
            {
                throw new Exception($"Invalid corner PDB size! \nLoaded Size: {database.Length} bytes");
            }
            return database;
        } 

        public static byte[] GenerateArray(int maxDepth)
        {
            return Generate(maxDepth, true);
        }

        public static byte[] GenerateFull()
        {
            return Generate(0, false);
        }

        private static byte[] Generate(int maxDepth, bool useDepthLimit)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<int> depthCounts = new List<int>();

            // Unvisited entries are marked with 255
            byte[] database = new byte[CornerStateCount];
            for (int i = 0; i < database.Length; i++)
            {
                database[i] = Unvisited;
            }

            Queue<PdbNode> queue = new Queue<PdbNode>();

            int[] solvedPermutation = { 0, 1, 2, 3, 4, 5, 6, 7 };
            int[] solvedOrientation = { 0, 0, 0, 0, 0, 0, 0, 0 };
            int solvedIndex = CornerCoordinate.GetIndex(solvedPermutation, solvedOrientation);

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
                    int childIndex = MoveProcessor.ApplyCornerMoveToIndex(current.CornerIndex, move);

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
            LastGenerationStats = new CornerPdbGenerationStats
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
