using Assets.Scripts.Core;
using Assets.Scripts.Solver.Heuristics;
using System.Collections.Generic;
using System.Diagnostics;

namespace Assets.Scripts.Solver
{
    public class IDAStarSearchStats
    {
        public int InitialBound;
        public int FinalBound;
        public int BoundIterations;
        public int NodesVisited;
        public int NodesExpanded;
        public int ChildrenGenerated;
        public int PrunedByHeuristic;
        public int MaxDepthReached;
        public long ElapsedMilliseconds;
    }

    public static class IDAStarSolver
    {
        private const int Found = -1;
        private const int Infinity = int.MaxValue;

        public static IDAStarSearchStats LastSearchStats { get; private set; }

        public static List<string> Solve(CubeStateData startState, int maxDepth)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            CubeStateData start = CubeState.CloneState(startState);

            int bound = CornerPDBHeuristics.Estimate(start);
            LastSearchStats = new IDAStarSearchStats
            {
                InitialBound = bound
            };

            while (bound <= maxDepth)
            {
                List<string> path = new List<string>();
                HashSet<string> visitedOnPath = new HashSet<string>();

                LastSearchStats.BoundIterations++;
                LastSearchStats.FinalBound = bound;

                int result = Search(start, 0, bound, null, path, visitedOnPath);

                if (result == Found)
                {
                    stopwatch.Stop();
                    LastSearchStats.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    return path;
                }

                if (result == Infinity)
                {
                    stopwatch.Stop();
                    LastSearchStats.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    return null;
                }

                bound = result;
            }

            stopwatch.Stop();
            LastSearchStats.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return null;
        }

        private static int Search(CubeStateData state, int depth, int bound, string previousMove, List<string> path, HashSet<string> visitedOnPath)
        {
            LastSearchStats.NodesVisited++;

            if (depth > LastSearchStats.MaxDepthReached)
            {
                LastSearchStats.MaxDepthReached = depth;
            }

            int heuristic = CornerPDBHeuristics.Estimate(state);
            int estimatedTotal = depth + heuristic;

            if (estimatedTotal > bound)
            {
                LastSearchStats.PrunedByHeuristic++;
                return estimatedTotal;
            }

            if (CubeStateUtility.IsSolved(state))
            {
                return Found;
            }

            int minNextBound = Infinity;
            string stateKey = CubeStateUtility.GetStateKey(state);
            visitedOnPath.Add(stateKey);
            LastSearchStats.NodesExpanded++;

            foreach (string move in MoveGenerator.GetValidMoves(previousMove))
            {
                LastSearchStats.ChildrenGenerated++;

                CubeStateData child = CubeState.CloneState(state);
                MoveProcessor.ApplyMove(child, move, false);

                string childKey = CubeStateUtility.GetStateKey(child);
                if (visitedOnPath.Contains(childKey))
                {
                    continue;
                }

                path.Add(move);
                int result = Search(child, depth + 1, bound, move, path, visitedOnPath);

                if (result == Found)
                {
                    return result;
                }

                if (result < minNextBound)
                {
                    minNextBound = result;
                }

                path.RemoveAt(path.Count - 1);
            }

            visitedOnPath.Remove(stateKey);

            return minNextBound;
        }
    }
}
