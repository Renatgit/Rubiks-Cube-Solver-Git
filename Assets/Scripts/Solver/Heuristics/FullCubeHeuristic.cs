using System;

namespace Assets.Scripts.Solver.Heuristics
{
    public static class FullCubeHeuristic
    {
        public static int Estimate(SolverStateData state)
        {
            int cornerEstimate = CornerPDBHeuristics.Estimate(state);
            int edgeEstimate = EdgeGroupPDBHeuristics.Estimate(state);

            return Math.Max(cornerEstimate, edgeEstimate);
        }
    }
}
