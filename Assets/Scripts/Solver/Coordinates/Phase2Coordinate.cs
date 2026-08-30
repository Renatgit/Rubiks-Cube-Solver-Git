using Assets.Scripts.Solver;
using System.Collections.Generic;

namespace Assets.Scripts.Solver.Coordinates
{
    public static class Phase2Coordinate
    {
        public const int CornerPermutationCount = 40320;
        public const int SlicePermutationCount = 24;
        public const int NonSliceEdgePermutationCount = 40320;

        public const int CornerSlicePermutationCount = CornerPermutationCount * SlicePermutationCount;

        private const int NonSliceEdgeCount = 8;
        private const int SliceEdgeCount = 4;
        private const int FirstSliceEdge = 8;

        private static readonly int[] Factorials =
        {
            1,
            1,
            2,
            6,
            24,
            120,
            720,
            5040,
            40320
        };

        public static int GetCornerPermutationIndex(SolverStateData state)
        {
            return CornerCoordinate.GetPermutationIndex(state.CornerPermutation);
        }

        public static int GetSlicePermutationIndex(SolverStateData state)
        {
            return GetSlicePermutationIndex(state.FullEdgePermutation);
        }

        public static int GetSlicePermutationIndex(int[] fullEdgePermutation)
        {
            int[] permutation = new int[SliceEdgeCount];

            for (int i = 0; i < SliceEdgeCount; i++)
            {
                permutation[i] = fullEdgePermutation[FirstSliceEdge + i] - FirstSliceEdge;
            }

            return GetPermutationIndex(permutation, SliceEdgeCount);
        }

        public static int[] GetSlicePermutationFromIndex(int index)
        {
            int[] localPermutation = GetPermutationFromIndex(index, SliceEdgeCount);
            int[] slicePermutation = new int[SliceEdgeCount];

            for (int i = 0; i < SliceEdgeCount; i++)
            {
                slicePermutation[i] = localPermutation[i] + FirstSliceEdge;
            }

            return slicePermutation;
        }

        public static int GetNonSliceEdgePermutationIndex(SolverStateData state)
        {
            return GetNonSliceEdgePermutationIndex(state.FullEdgePermutation);
        }

        public static int GetNonSliceEdgePermutationIndex(int[] fullEdgePermutation)
        {
            int[] permutation = new int[NonSliceEdgeCount];

            for (int i = 0; i < NonSliceEdgeCount; i++)
            {
                permutation[i] = fullEdgePermutation[i];
            }

            return GetPermutationIndex(permutation, NonSliceEdgeCount);
        }

        public static int[] GetNonSliceEdgePermutationFromIndex(int index)
        {
            return GetPermutationFromIndex(index, NonSliceEdgeCount);
        }

        public static int GetCornerSlicePermutationIndex(SolverStateData state)
        {
            return GetCornerPermutationIndex(state) * SlicePermutationCount + GetSlicePermutationIndex(state);
        }

        private static int GetPermutationIndex(int[] permutation, int length)
        {
            int index = 0;

            for (int i = 0; i < length; i++)
            {
                int smallerNumbersOnRight = 0;

                for (int j = i + 1; j < length; j++)
                {
                    if (permutation[j] < permutation[i])
                    {
                        smallerNumbersOnRight++;
                    }
                }

                index += smallerNumbersOnRight * Factorials[length - 1 - i];
            }

            return index;
        }

        private static int[] GetPermutationFromIndex(int index, int length)
        {
            int[] permutation = new int[length];
            List<int> remainingPieces = new List<int>();

            for (int i = 0; i < length; i++)
            {
                remainingPieces.Add(i);
            }

            for (int i = 0; i < length; i++)
            {
                int factorial = Factorials[length - 1 - i];
                int selectedIndex = index / factorial;
                index %= factorial;

                permutation[i] = remainingPieces[selectedIndex];
                remainingPieces.RemoveAt(selectedIndex);
            }

            return permutation;
        }
    }
}
