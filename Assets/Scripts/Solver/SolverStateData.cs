using Assets.Scripts.Solver.Coordinates;
using System;

namespace Assets.Scripts.Solver
{
    public class SolverStateData
    {
        public int[] CornerPermutation;
        public int[] CornerOrientation;
        public int[] FullEdgePermutation;
        public int[] FullEdgeOrientation;

        public static SolverStateData FromCubeStateData(CubeStateData state)
        {
            return new SolverStateData
            {
                CornerPermutation = state.cornerPermutation.ToArray(),
                CornerOrientation = state.cornerOrientation.ToArray(),
                FullEdgePermutation = state.fullEdgePermutation.ToArray(),
                FullEdgeOrientation = state.fullEdgeOrientation.ToArray()
            };
        }

        public SolverStateData Clone()
        {
            return new SolverStateData
            {
                CornerPermutation = (int[])CornerPermutation.Clone(),
                CornerOrientation = (int[])CornerOrientation.Clone(),
                FullEdgePermutation = (int[])FullEdgePermutation.Clone(),
                FullEdgeOrientation = (int[])FullEdgeOrientation.Clone()
            };
        }
    }

    public readonly struct SolverStateKey : IEquatable<SolverStateKey>
    {
        private static readonly int[] EdgeGroupA = { 0, 1, 2, 3, 4, 5 };
        private static readonly int[] EdgeGroupB = { 6, 7, 8, 9, 10, 11 };

        private readonly int cornerIndex;
        private readonly int edgeGroupAIndex;
        private readonly int edgeGroupBIndex;

        public SolverStateKey(int cornerIndex, int edgeGroupAIndex, int edgeGroupBIndex)
        {
            this.cornerIndex = cornerIndex;
            this.edgeGroupAIndex = edgeGroupAIndex;
            this.edgeGroupBIndex = edgeGroupBIndex;
        }

        public static SolverStateKey FromState(SolverStateData state)
        {
            return new SolverStateKey(
                CornerCoordinate.GetIndex(state.CornerPermutation, state.CornerOrientation),
                EdgeGroupCoordinate.GetIndex(state.FullEdgePermutation, state.FullEdgeOrientation, EdgeGroupA),
                EdgeGroupCoordinate.GetIndex(state.FullEdgePermutation, state.FullEdgeOrientation, EdgeGroupB));
        }

        public bool Equals(SolverStateKey other)
        {
            return cornerIndex == other.cornerIndex
                && edgeGroupAIndex == other.edgeGroupAIndex
                && edgeGroupBIndex == other.edgeGroupBIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is SolverStateKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + cornerIndex;
                hash = hash * 31 + edgeGroupAIndex;
                hash = hash * 31 + edgeGroupBIndex;
                return hash;
            }
        }
    }

    public static class SolverStateUtility
    {
        public static bool IsSolved(SolverStateData state)
        {
            for (int i = 0; i < state.CornerPermutation.Length; i++)
            {
                if (state.CornerPermutation[i] != i || state.CornerOrientation[i] != 0)
                {
                    return false;
                }
            }

            for (int i = 0; i < state.FullEdgePermutation.Length; i++)
            {
                if (state.FullEdgePermutation[i] != i || state.FullEdgeOrientation[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
