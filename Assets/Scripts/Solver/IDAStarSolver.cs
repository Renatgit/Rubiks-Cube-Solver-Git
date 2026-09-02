using Assets.Scripts.Core;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.Search;
using System.Collections.Generic;

namespace Assets.Scripts.Solver
{
    public class IDAStarSearchStats
    {
        public int InitialBound;
        public int FinalBound;
        public int BoundIterations;
        public long NodesVisited;
        public long NodesExpanded;
        public long ChildrenGenerated;
        public long PrunedByHeuristic;
        public int MaxDepthReached;
        public long ElapsedMilliseconds;
    }

    public static class IDAStarSolver
    {
        public static IDAStarSearchStats LastSearchStats
        {
            get { return IDAStarSearch.LastSearchStats; }
        }

        public static List<string> Solve(CubeStateData startState, int maxDepth)
        {
            SolverStateData start = SolverStateData.FromCubeStateData(startState);

            return IDAStarSearch.Solve(
                start,
                maxDepth,
                SolverStateUtility.IsSolved,
                FullCubeHeuristic.Estimate,
                MoveGenerator.GetValidMoves);
        }
    }
}
