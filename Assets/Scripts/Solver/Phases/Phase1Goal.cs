using Assets.Scripts.Solver;

namespace Assets.Scripts.Solver.Phases
{
    public static class Phase1Goal
    {
        public static bool IsReached(SolverStateData state)
        {
            return CornerOrientationSolved(state.CornerOrientation)
                && EdgeOrientationSolved(state.FullEdgeOrientation)
                && SliceEdgesInSlicePositions(state.FullEdgePermutation);
        }

        private static bool CornerOrientationSolved(int[] orientation)
        {
            for (int i = 0; i < orientation.Length; i++)
            {
                if (orientation[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EdgeOrientationSolved(int[] orientation)
        {
            for (int i = 0; i < orientation.Length; i++)
            {
                if (orientation[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SliceEdgesInSlicePositions(int[] edgePermutation)
        {
            for (int i = 8; i <= 11; i++)
            {
                if (edgePermutation[i] < 8)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
