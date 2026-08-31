using Assets.Scripts.Core;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.Search;
using System.Collections.Generic;

namespace Assets.Scripts.Solver.Phases
{
    public static class Phase2Solver
    {
        public static IDAStarSearchStats LastSearchStats
        {
            get { return IDAStarSearch.LastSearchStats; }
        }

        public static List<string> Solve(SolverStateData startState, int maxDepth)
        {
            return Solve(startState, maxDepth, null);
        }

        public static List<string> Solve(SolverStateData startState, int maxDepth, string previousMove)
        {
            return IDAStarSearch.Solve(
                startState,
                maxDepth,
                previousMove,
                SolverStateUtility.IsSolved,
                Phase2Heuristic.Estimate,
                MoveGenerator.GetValidPhase2Moves);
        }
    }
}
