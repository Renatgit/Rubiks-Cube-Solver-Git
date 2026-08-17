using System;

namespace Assets.Scripts.Solver
{
    public class CubeHeuristicBreakdown
    {
        public int MisplacedCorners;
        public int TwistedCorners;
        public int MisplacedEdges;
        public int FlippedEdges;
        public int Estimate;
    }

    public static class CubeHeuristic
    {
        public static int Estimate(CubeStateData state)
        {
            return GetBreakdown(state).Estimate;
        }

        public static CubeHeuristicBreakdown GetBreakdown(CubeStateData state)
        {
            int misplacedCorners = CountMisplacedCorners(state);
            int twistedCorners = CountTwistedCorners(state);
            int misplacedEdges = CountMisplacedEdges(state);
            int flippedEdges = CountFlippedEdges(state);

            int cornerPermutationBound = CeilDivideByFour(misplacedCorners);
            int cornerOrientationBound = CeilDivideByFour(twistedCorners);
            int edgePermutationBound = CeilDivideByFour(misplacedEdges);
            int edgeOrientationBound = CeilDivideByFour(flippedEdges);

            return new CubeHeuristicBreakdown
            {
                MisplacedCorners = misplacedCorners,
                TwistedCorners = twistedCorners,
                MisplacedEdges = misplacedEdges,
                FlippedEdges = flippedEdges,
                Estimate = Math.Max(
                    Math.Max(cornerPermutationBound, cornerOrientationBound),
                    Math.Max(edgePermutationBound, edgeOrientationBound))
            };
        }

        private static int CountMisplacedCorners(CubeStateData state)
        {
            int count = 0;

            for (int position = 0; position < state.cornerPermutation.Count; position++)
            {
                if (state.cornerPermutation[position] != position)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountTwistedCorners(CubeStateData state)
        {
            int count = 0;

            foreach (int orientation in state.cornerOrientation)
            {
                if (orientation != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountMisplacedEdges(CubeStateData state)
        {
            int count = 0;

            for (int position = 0; position < state.fullEdgePermutation.Count; position++)
            {
                if (state.fullEdgePermutation[position] != position)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountFlippedEdges(CubeStateData state)
        {
            int count = 0;

            foreach (int orientation in state.fullEdgeOrientation)
            {
                if (orientation != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CeilDivideByFour(int value)
        {
            return (value + 3) / 4;
        }
    }
}
