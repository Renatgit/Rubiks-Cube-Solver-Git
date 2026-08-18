using Assets.Scripts.Core;
using Assets.Scripts.Solver.Coordinates;
using System.Collections.Generic;

namespace Assets.Scripts.Solver.PatternDatabases
{
    public static class CornerPDB
    {
        public const int CornerStateCount = 88179840;
        public const byte Unvisited = 255;

        private class PdbNode
        {
            public CubeStateData State;
            public int Depth;
            public string PreviousMove;

            public PdbNode(CubeStateData state, int depth, string previousMove)
            {
                State = state;
                Depth = depth;
                PreviousMove = previousMove;
            }
        }

        public static byte[] GenerateArray(int maxDepth)
        {
            // Unvisited entries are marked with 255
            byte[] database = new byte[CornerStateCount];
            for (int i = 0; i < database.Length; i++)
            {
                database[i] = Unvisited;
            }

            Queue<PdbNode> queue = new Queue<PdbNode>();

            CubeStateData solved = CubeState.CreateSolvedState();
            int solvedIndex = CornerCoordinate.GetIndex(solved);

            database[solvedIndex] = 0;
            queue.Enqueue(new PdbNode(solved, 0, null));

            while (queue.Count > 0)
            {
                PdbNode current = queue.Dequeue();

                if (current.Depth >= maxDepth)
                {
                    continue;
                }

                foreach (string move in MoveGenerator.GetValidMoves(current.PreviousMove))
                {
                    CubeStateData child = CubeState.CloneState(current.State);
                    MoveProcessor.ApplyMove(child, move, false);

                    int childIndex = CornerCoordinate.GetIndex(child);

                    if (database[childIndex] != Unvisited)
                    {
                        continue;
                    }

                    int childDepth = current.Depth + 1;
                    database[childIndex] = (byte)childDepth;
                    queue.Enqueue(new PdbNode(child, childDepth, move));
                }
            }

            return database;
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
