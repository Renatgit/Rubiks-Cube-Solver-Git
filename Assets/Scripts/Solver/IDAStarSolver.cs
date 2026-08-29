using Assets.Scripts.Core;
using Assets.Scripts.Solver.Heuristics;
using System;
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
            SolverStateData start = SolverStateData.FromCubeStateData(startState);

            int bound = Estimate(start);
            LastSearchStats = new IDAStarSearchStats
            {
                InitialBound = bound
            };

            while (bound <= maxDepth)
            {
                List<string> path = new List<string>();
                HashSet<SolverStateKey> visitedOnPath = new HashSet<SolverStateKey>();

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

        private static int Search(SolverStateData state, int depth, int bound, string previousMove, List<string> path, HashSet<SolverStateKey> visitedOnPath)
        {
            LastSearchStats.NodesVisited++;

            if (depth > LastSearchStats.MaxDepthReached)
            {
                LastSearchStats.MaxDepthReached = depth;
            }

            int heuristic = Estimate(state);
            int estimatedTotal = depth + heuristic;

            if (estimatedTotal > bound)
            {
                LastSearchStats.PrunedByHeuristic++;
                return estimatedTotal;
            }

            if (SolverStateUtility.IsSolved(state))
            {
                return Found;
            }

            int minNextBound = Infinity;
            SolverStateKey stateKey = SolverStateKey.FromState(state);
            visitedOnPath.Add(stateKey);
            LastSearchStats.NodesExpanded++;

            foreach (string move in MoveGenerator.GetValidMoves(previousMove))
            {
                LastSearchStats.ChildrenGenerated++;

                SolverStateData child = state.Clone();
                MoveProcessor.ApplyMove(child, move);

                SolverStateKey childKey = SolverStateKey.FromState(child);
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

        private static int Estimate(SolverStateData state)
        {
            int cornerEstimate = CornerPDBHeuristics.Estimate(state);
            int edgeEstimate = EdgeGroupPDBHeuristics.Estimate(state);

            return Math.Max(cornerEstimate, edgeEstimate);
        }
    }
}
