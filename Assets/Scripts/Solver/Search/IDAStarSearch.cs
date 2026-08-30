using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Assets.Scripts.Solver.Search
{
    public static class IDAStarSearch
    {
        private const int Found = -1;
        private const int Infinity = int.MaxValue;

        public static IDAStarSearchStats LastSearchStats { get; private set; }

        public static List<string> Solve(
            SolverStateData startState,
            int maxDepth,
            Func<SolverStateData, bool> isGoal,
            Func<SolverStateData, int> estimate,
            Func<string, List<string>> getValidMoves)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            SolverStateData start = startState.Clone();

            int bound = estimate(start);
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

                int result = Search(start, 0, bound, null, path, visitedOnPath, isGoal, estimate, getValidMoves);

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

        private static int Search(
            SolverStateData state,
            int depth,
            int bound,
            string previousMove,
            List<string> path,
            HashSet<SolverStateKey> visitedOnPath,
            Func<SolverStateData, bool> isGoal,
            Func<SolverStateData, int> estimate,
            Func<string, List<string>> getValidMoves)
        {
            LastSearchStats.NodesVisited++;

            if (depth > LastSearchStats.MaxDepthReached)
            {
                LastSearchStats.MaxDepthReached = depth;
            }

            int heuristic = estimate(state);
            int estimatedTotal = depth + heuristic;

            if (estimatedTotal > bound)
            {
                LastSearchStats.PrunedByHeuristic++;
                return estimatedTotal;
            }

            if (isGoal(state))
            {
                return Found;
            }

            int minNextBound = Infinity;
            SolverStateKey stateKey = SolverStateKey.FromState(state);
            visitedOnPath.Add(stateKey);
            LastSearchStats.NodesExpanded++;

            foreach (string move in getValidMoves(previousMove))
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
                int result = Search(child, depth + 1, bound, move, path, visitedOnPath, isGoal, estimate, getValidMoves);

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
