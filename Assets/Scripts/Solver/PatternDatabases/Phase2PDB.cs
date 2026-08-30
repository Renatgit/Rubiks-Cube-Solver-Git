using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Assets.Scripts.Solver.PatternDatabases
{
    public class Phase2PdbGenerationStats
    {
        public int MaxDepth;
        public int VisitedStates;
        public long ElapsedMilliseconds;
        public int[] DepthCounts;
    }

    public static class Phase2PDB
    {
        public const byte Unvisited = 255;

        public static Phase2PdbGenerationStats LastGenerationStats { get; private set; }

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

        public static byte[] GenerateFullCornerSlicePermutation()
        {
            return Generate(
                Phase2Coordinate.CornerSlicePermutationCount,
                Phase2Coordinate.GetCornerSlicePermutationIndex);
        }

        public static byte[] GenerateFullNonSliceEdgePermutation()
        {
            return Generate(
                Phase2Coordinate.NonSliceEdgePermutationCount,
                Phase2Coordinate.GetNonSliceEdgePermutationIndex);
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

        public static byte[] LoadCornerSlicePermutation(string filePath)
        {
            return Load(filePath, Phase2Coordinate.CornerSlicePermutationCount, "corner-slice permutation");
        }

        public static byte[] LoadNonSliceEdgePermutation(string filePath)
        {
            return Load(filePath, Phase2Coordinate.NonSliceEdgePermutationCount, "non-slice edge permutation");
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

        private static byte[] Generate(int databaseSize, Func<SolverStateData, int> getIndex)
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

                foreach (string move in MoveGenerator.GetValidPhase2Moves(current.PreviousMove))
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
            LastGenerationStats = new Phase2PdbGenerationStats
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
                throw new Exception("Invalid " + databaseName + " Phase 2 PDB size! Loaded Size: "
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
