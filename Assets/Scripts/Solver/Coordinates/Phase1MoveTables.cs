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
        private static ushort[] edgeGroupPositionMoves;
        private static ushort[] edgeGroupSlotReorders;
        private static ushort[] edgeGroupPermutationCompositions;
        private static byte[] edgeGroupOrientationMoves;
        private static ushort[] complementaryEdgeGroupPositions;
        private static byte[] edgeGroupOrientationsFromFullOrientation;

        private static readonly int[] EdgeGroupFactorials =
        {
            1,
            1,
            2,
            6,
            24,
            120,
            720
        };

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetEdgeGroupPositionAfterMovePrepared(int positionIndex, int moveId)
        {
            return edgeGroupPositionMoves[positionIndex * MoveCount + moveId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetEdgeGroupPermutationAfterMovePrepared(
            int positionIndex,
            int permutationIndex,
            int moveId)
        {
            int slotReorder = edgeGroupSlotReorders[positionIndex * MoveCount + moveId];
            return edgeGroupPermutationCompositions[
                permutationIndex * EdgeGroupCoordinate.PermutationCount + slotReorder];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetEdgeGroupOrientationAfterMovePrepared(
            int positionIndex,
            int orientationIndex,
            int moveId)
        {
            return edgeGroupOrientationMoves[
                (positionIndex * EdgeGroupCoordinate.OrientationCount + orientationIndex) * MoveCount
                + moveId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetComplementaryEdgeGroupPositionPrepared(int positionIndex)
        {
            return complementaryEdgeGroupPositions[positionIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetEdgeGroupOrientationFromFullOrientationPrepared(
            int fullEdgeOrientationIndex,
            int positionIndex)
        {
            return edgeGroupOrientationsFromFullOrientation[
                fullEdgeOrientationIndex * EdgeGroupCoordinate.PositionCount + positionIndex];
        }

        public static void BuildIfNeeded()
        {
            if (cornerOrientationMoves != null
                && edgeOrientationMoves != null
                && slicePositionMoves != null
                && cornerPermutationMoves != null
                && sliceArrangementMoves != null
                && edgeGroupPositionMoves != null
                && edgeGroupSlotReorders != null
                && edgeGroupPermutationCompositions != null
                && edgeGroupOrientationMoves != null
                && complementaryEdgeGroupPositions != null
                && edgeGroupOrientationsFromFullOrientation != null)
            {
                return;
            }

            BuildCornerOrientationMoves();
            BuildEdgeOrientationMoves();
            BuildSlicePositionMoves();
            BuildCornerPermutationMoves();
            BuildSliceArrangementMoves();
            BuildEdgeGroupMoves();
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

        private static void BuildEdgeGroupMoves()
        {
            int positionMoveCount = EdgeGroupCoordinate.PositionCount * MoveCount;
            edgeGroupPositionMoves = new ushort[positionMoveCount];
            edgeGroupSlotReorders = new ushort[positionMoveCount];
            edgeGroupOrientationMoves = new byte[
                EdgeGroupCoordinate.PositionCount
                * EdgeGroupCoordinate.OrientationCount
                * MoveCount];
            complementaryEdgeGroupPositions = new ushort[EdgeGroupCoordinate.PositionCount];
            byte[] positionsByIndex = new byte[
                EdgeGroupCoordinate.PositionCount * EdgeGroupCoordinate.TrackedEdgeCount];

            int[] fullSlots = new int[EdgeGroupCoordinate.EdgeCount];
            int[] movedSlots = new int[EdgeGroupCoordinate.EdgeCount];
            int[] emptyOrientation = new int[EdgeGroupCoordinate.EdgeCount];
            int[] movedOrientation = new int[EdgeGroupCoordinate.EdgeCount];
            int[] movedPositions = new int[EdgeGroupCoordinate.TrackedEdgeCount];
            int[] slotReorder = new int[EdgeGroupCoordinate.TrackedEdgeCount];
            int[] orientationFlips = new int[EdgeGroupCoordinate.TrackedEdgeCount];

            for (int positionIndex = 0;
                positionIndex < EdgeGroupCoordinate.PositionCount;
                positionIndex++)
            {
                int[] positions = EdgeGroupCoordinate.GetPositionsFromIndex(positionIndex);
                int positionsOffset = positionIndex * EdgeGroupCoordinate.TrackedEdgeCount;
                for (int slot = 0; slot < EdgeGroupCoordinate.TrackedEdgeCount; slot++)
                {
                    positionsByIndex[positionsOffset + slot] = (byte)positions[slot];
                }

                int[] complementaryPositions = new int[EdgeGroupCoordinate.TrackedEdgeCount];
                int complementSlot = 0;
                int occupiedSlot = 0;
                for (int position = 0; position < EdgeGroupCoordinate.EdgeCount; position++)
                {
                    if (occupiedSlot < EdgeGroupCoordinate.TrackedEdgeCount
                        && positions[occupiedSlot] == position)
                    {
                        occupiedSlot++;
                    }
                    else
                    {
                        complementaryPositions[complementSlot] = position;
                        complementSlot++;
                    }
                }

                complementaryEdgeGroupPositions[positionIndex] =
                    (ushort)EdgeGroupCoordinate.GetPositionIndex(complementaryPositions);

                for (int moveId = 0; moveId < MoveCount; moveId++)
                {
                    for (int position = 0; position < EdgeGroupCoordinate.EdgeCount; position++)
                    {
                        fullSlots[position] = -1;
                        emptyOrientation[position] = 0;
                    }

                    for (int slot = 0; slot < EdgeGroupCoordinate.TrackedEdgeCount; slot++)
                    {
                        fullSlots[positions[slot]] = slot;
                    }

                    MoveProcessor.ApplyEdgePermutationMove(fullSlots, moveId, movedSlots);
                    MoveProcessor.ApplyEdgeOrientationMove(
                        emptyOrientation,
                        moveId,
                        movedOrientation);

                    int movedSlotCount = 0;
                    for (int newPosition = 0;
                        newPosition < EdgeGroupCoordinate.EdgeCount;
                        newPosition++)
                    {
                        int oldSlot = movedSlots[newPosition];
                        if (oldSlot < 0)
                        {
                            continue;
                        }

                        movedPositions[movedSlotCount] = newPosition;
                        slotReorder[movedSlotCount] = oldSlot;
                        orientationFlips[movedSlotCount] = movedOrientation[newPosition];
                        movedSlotCount++;
                    }

                    int moveTableIndex = positionIndex * MoveCount + moveId;
                    edgeGroupPositionMoves[moveTableIndex] =
                        (ushort)EdgeGroupCoordinate.GetPositionIndex(movedPositions);
                    edgeGroupSlotReorders[moveTableIndex] =
                        (ushort)EdgeGroupCoordinate.GetPermutationIndex(slotReorder);

                    for (int orientationIndex = 0;
                        orientationIndex < EdgeGroupCoordinate.OrientationCount;
                        orientationIndex++)
                    {
                        int movedOrientationIndex = 0;
                        for (int newSlot = 0;
                            newSlot < EdgeGroupCoordinate.TrackedEdgeCount;
                            newSlot++)
                        {
                            int oldSlot = slotReorder[newSlot];
                            int oldOrientation =
                                (orientationIndex
                                    >> (EdgeGroupCoordinate.TrackedEdgeCount - 1 - oldSlot))
                                & 1;
                            movedOrientationIndex = movedOrientationIndex * 2
                                + (oldOrientation ^ orientationFlips[newSlot]);
                        }

                        edgeGroupOrientationMoves[
                            (positionIndex * EdgeGroupCoordinate.OrientationCount + orientationIndex)
                            * MoveCount
                            + moveId] = (byte)movedOrientationIndex;
                    }
                }
            }

            BuildEdgeGroupPermutationCompositions();
            BuildEdgeGroupOrientationsFromFullOrientation(positionsByIndex);
        }

        private static void BuildEdgeGroupOrientationsFromFullOrientation(byte[] positionsByIndex)
        {
            edgeGroupOrientationsFromFullOrientation = new byte[
                Phase1Coordinate.EdgeOrientationCount * EdgeGroupCoordinate.PositionCount];

            for (int fullOrientationIndex = 0;
                fullOrientationIndex < Phase1Coordinate.EdgeOrientationCount;
                fullOrientationIndex++)
            {
                int[] fullOrientation =
                    Phase1Coordinate.GetEdgeOrientationFromIndex(fullOrientationIndex);

                for (int positionIndex = 0;
                    positionIndex < EdgeGroupCoordinate.PositionCount;
                    positionIndex++)
                {
                    int positionsOffset =
                        positionIndex * EdgeGroupCoordinate.TrackedEdgeCount;
                    int groupOrientationIndex = 0;

                    for (int slot = 0;
                        slot < EdgeGroupCoordinate.TrackedEdgeCount;
                        slot++)
                    {
                        int position = positionsByIndex[positionsOffset + slot];
                        groupOrientationIndex = groupOrientationIndex * 2
                            + fullOrientation[position];
                    }

                    edgeGroupOrientationsFromFullOrientation[
                        fullOrientationIndex * EdgeGroupCoordinate.PositionCount
                        + positionIndex] = (byte)groupOrientationIndex;
                }
            }
        }

        private static void BuildEdgeGroupPermutationCompositions()
        {
            int permutationCount = EdgeGroupCoordinate.PermutationCount;
            int trackedEdgeCount = EdgeGroupCoordinate.TrackedEdgeCount;
            byte[] permutations = new byte[permutationCount * trackedEdgeCount];

            for (int permutationIndex = 0;
                permutationIndex < permutationCount;
                permutationIndex++)
            {
                int[] permutation = EdgeGroupCoordinate.GetPermutationFromIndex(permutationIndex);
                for (int slot = 0; slot < trackedEdgeCount; slot++)
                {
                    permutations[permutationIndex * trackedEdgeCount + slot] =
                        (byte)permutation[slot];
                }
            }

            edgeGroupPermutationCompositions = new ushort[permutationCount * permutationCount];

            for (int permutationIndex = 0;
                permutationIndex < permutationCount;
                permutationIndex++)
            {
                for (int slotReorderIndex = 0;
                    slotReorderIndex < permutationCount;
                    slotReorderIndex++)
                {
                    int composedIndex = GetComposedEdgePermutationIndex(
                        permutations,
                        permutationIndex,
                        slotReorderIndex);
                    edgeGroupPermutationCompositions[
                        permutationIndex * permutationCount + slotReorderIndex] =
                        (ushort)composedIndex;
                }
            }
        }

        private static int GetComposedEdgePermutationIndex(
            byte[] permutations,
            int permutationIndex,
            int slotReorderIndex)
        {
            int trackedEdgeCount = EdgeGroupCoordinate.TrackedEdgeCount;
            int permutationOffset = permutationIndex * trackedEdgeCount;
            int reorderOffset = slotReorderIndex * trackedEdgeCount;
            int index = 0;

            for (int slot = 0; slot < trackedEdgeCount; slot++)
            {
                int reorderedSlot = permutations[reorderOffset + slot];
                int edge = permutations[permutationOffset + reorderedSlot];
                int smallerEdgesOnRight = 0;

                for (int rightSlot = slot + 1; rightSlot < trackedEdgeCount; rightSlot++)
                {
                    int rightReorderedSlot = permutations[reorderOffset + rightSlot];
                    int rightEdge = permutations[permutationOffset + rightReorderedSlot];
                    if (rightEdge < edge)
                    {
                        smallerEdgesOnRight++;
                    }
                }

                index += smallerEdgesOnRight
                    * EdgeGroupFactorials[trackedEdgeCount - 1 - slot];
            }

            return index;
        }

        private static SolverStateData CreateSolvedSolverState()
        {
            return SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        }

    }
}
