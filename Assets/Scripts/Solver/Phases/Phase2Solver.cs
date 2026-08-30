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

        public static List<string> Solve(CubeStateData startState, int maxDepth)
        {
            SolverStateData start = SolverStateData.FromCubeStateData(startState);

            return IDAStarSearch.Solve(
                start,
                maxDepth,
                SolverStateUtility.IsSolved,
                Phase2Heuristic.Estimate,
                MoveGenerator.GetValidPhase2Moves);
        }
    }
}
