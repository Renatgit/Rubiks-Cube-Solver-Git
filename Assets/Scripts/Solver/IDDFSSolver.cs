using Assets.Scripts.Core;
using System.Collections.Generic;
using System.Diagnostics;

namespace Assets.Scripts.Solver
{
    public class IDDFSSearchStats
    {
        public int MaxDepth;
        public int DepthReached;
        public long NodesSearched;
        public long ElapsedMilliseconds;
        public bool FoundSolution;
    }

    public static class IDDFSSolver
    {
        public static IDDFSSearchStats LastStats { get; private set; }

        public static List<string> Solve(CubeStateData startState, int maxDepth)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            LastStats = new IDDFSSearchStats
            {
                MaxDepth = maxDepth,
                DepthReached = 0,
                NodesSearched = 0,
                ElapsedMilliseconds = 0,
                FoundSolution = false
            };

            for (int depthLimit = 0; depthLimit <= maxDepth; depthLimit++)
            {
                LastStats.DepthReached = depthLimit;

                List<string> path = new List<string>();
                HashSet<string> visitedOnPath = new HashSet<string>();

                CubeStateData start = CubeState.CloneState(startState);

                bool result = DepthLimitedSearch(
                    start,
                    depthLimit,
                    null,
                    path,
                    visitedOnPath
                );

                if (result)
                {
                    stopwatch.Stop();
                    LastStats.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    LastStats.FoundSolution = true;
                    return path;
                }
            }

            stopwatch.Stop();
            LastStats.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return null;
        }

        private static bool DepthLimitedSearch(CubeStateData state, int depthRemaining, string previousMove, List<string> path, HashSet<string> visitedOnPath)
        {
            LastStats.NodesSearched++;

            if (CubeStateUtility.IsSolved(state))
            {
                return true;
            }

            if (depthRemaining == 0)
            {
                return false;
            }

            string stateKey = CubeStateUtility.GetStateKey(state);
            visitedOnPath.Add(stateKey);

            List<string> validMoves = MoveGenerator.GetValidMoves(previousMove);

            foreach (string move in validMoves)
            {
                CubeStateData child = CubeState.CloneState(state);
                MoveProcessor.ApplyMove(child, move, false);

                string childKey = CubeStateUtility.GetStateKey(child);

                if (visitedOnPath.Contains(childKey))
                {
                    continue;
                }

                path.Add(move);

                bool found = DepthLimitedSearch(child, depthRemaining - 1, move, path, visitedOnPath);

                if (found)
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            visitedOnPath.Remove(stateKey);
            return false;
        }
    }
}
