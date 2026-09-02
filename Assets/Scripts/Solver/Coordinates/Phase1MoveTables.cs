using Assets.Scripts.Core;
using System.Runtime.CompilerServices;

namespace Assets.Scripts.Solver.Coordinates
{
    public static class Phase1MoveTables
    {
        private static readonly int MoveCount = MoveGenerator.AllMoves.Length;
        private static int[] cornerOrientationMoves;
        private static int[] edgeOrientationMoves;
        private static int[] slicePositionMoves;
        private static ushort[] cornerPermutationMoves;
        private static ushort[] sliceArrangementMoves;

        public static int GetCornerOrientationAfterMove(int cornerOrientationIndex, int moveId)
        {
            BuildIfNeeded();
            return GetCornerOrientationAfterMovePrepared(cornerOrientationIndex, moveId);
        }

        public static int GetEdgeOrientationAfterMove(int edgeOrientationIndex, int moveId)
        {
            BuildIfNeeded();
            return GetEdgeOrientationAfterMovePrepared(edgeOrientationIndex, moveId);
        }

        public static int GetSlicePositionAfterMove(int slicePositionIndex, int moveId)
        {
            BuildIfNeeded();
            return GetSlicePositionAfterMovePrepared(slicePositionIndex, moveId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetCornerOrientationAfterMovePrepared(int cornerOrientationIndex, int moveId)
        {
            return cornerOrientationMoves[cornerOrientationIndex * MoveCount + moveId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetEdgeOrientationAfterMovePrepared(int edgeOrientationIndex, int moveId)
        {
            return edgeOrientationMoves[edgeOrientationIndex * MoveCount + moveId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSlicePositionAfterMovePrepared(int slicePositionIndex, int moveId)
        {
            return slicePositionMoves[slicePositionIndex * MoveCount + moveId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetCornerPermutationAfterMovePrepared(int cornerPermutationIndex, int moveId)
        {
            return cornerPermutationMoves[cornerPermutationIndex * MoveCount + moveId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSliceArrangementAfterMovePrepared(int sliceArrangementIndex, int moveId)
        {
            return sliceArrangementMoves[sliceArrangementIndex * MoveCount + moveId];
        }

        public static void BuildIfNeeded()
        {
            if (cornerOrientationMoves != null
                && edgeOrientationMoves != null
                && slicePositionMoves != null
                && cornerPermutationMoves != null
                && sliceArrangementMoves != null)
            {
                return;
            }

            BuildCornerOrientationMoves();
            BuildEdgeOrientationMoves();
            BuildSlicePositionMoves();
            BuildCornerPermutationMoves();
            BuildSliceArrangementMoves();
        }

        private static void BuildCornerOrientationMoves()
        {
            cornerOrientationMoves = new int[Phase1Coordinate.CornerOrientationCount * MoveCount];

            for (int index = 0; index < Phase1Coordinate.CornerOrientationCount; index++)
            {
                for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                {
                    SolverStateData state = CreateSolvedSolverState();
                    state.CornerOrientation = CornerCoordinate.GetOrientationFromIndex(index);
                    MoveProcessor.ApplyMove(state, moveId);
                    cornerOrientationMoves[index * MoveCount + moveId] =
                        Phase1Coordinate.GetCornerOrientationIndex(state);
                }
            }
        }

        private static void BuildEdgeOrientationMoves()
        {
            edgeOrientationMoves = new int[Phase1Coordinate.EdgeOrientationCount * MoveCount];

            for (int index = 0; index < Phase1Coordinate.EdgeOrientationCount; index++)
            {
                for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                {
                    SolverStateData state = CreateSolvedSolverState();
                    state.FullEdgeOrientation = Phase1Coordinate.GetEdgeOrientationFromIndex(index);
                    MoveProcessor.ApplyMove(state, moveId);
                    edgeOrientationMoves[index * MoveCount + moveId] =
                        Phase1Coordinate.GetEdgeOrientationIndex(state);
                }
            }
        }

        private static void BuildSlicePositionMoves()
        {
            slicePositionMoves = new int[Phase1Coordinate.SlicePositionCount * MoveCount];

            for (int index = 0; index < Phase1Coordinate.SlicePositionCount; index++)
            {
                for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                {
                    SolverStateData state = CreateSolvedSolverState();
                    state.FullEdgePermutation = Phase1Coordinate.GetEdgePermutationFromSliceIndex(index);
                    MoveProcessor.ApplyMove(state, moveId);
                    slicePositionMoves[index * MoveCount + moveId] =
                        Phase1Coordinate.GetSlicePositionIndex(state);
                }
            }
        }

        private static void BuildCornerPermutationMoves()
        {
            cornerPermutationMoves = new ushort[Phase2Coordinate.CornerPermutationCount * MoveCount];
            int[] movedPermutation = new int[8];

            for (int index = 0; index < Phase2Coordinate.CornerPermutationCount; index++)
            {
                int[] permutation = CornerCoordinate.GetPermutationFromIndex(index);

                for (int moveId = 0; moveId < MoveCount; moveId++)
                {
                    MoveProcessor.ApplyCornerPermutationMove(permutation, moveId, movedPermutation);
                    cornerPermutationMoves[index * MoveCount + moveId] =
                        (ushort)CornerCoordinate.GetPermutationIndex(movedPermutation);
                }
            }
        }

        private static void BuildSliceArrangementMoves()
        {
            sliceArrangementMoves = new ushort[Phase1Coordinate.SliceArrangementCount * MoveCount];
            int[] movedPermutation = new int[12];

            for (int index = 0; index < Phase1Coordinate.SliceArrangementCount; index++)
            {
                int[] permutation = Phase1Coordinate.GetEdgePermutationFromSliceArrangementIndex(index);

                for (int moveId = 0; moveId < MoveCount; moveId++)
                {
                    MoveProcessor.ApplyEdgePermutationMove(permutation, moveId, movedPermutation);
                    sliceArrangementMoves[index * MoveCount + moveId] =
                        (ushort)Phase1Coordinate.GetSliceArrangementIndex(movedPermutation);
                }
            }
        }

        private static SolverStateData CreateSolvedSolverState()
        {
            return SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        }

    }
}
