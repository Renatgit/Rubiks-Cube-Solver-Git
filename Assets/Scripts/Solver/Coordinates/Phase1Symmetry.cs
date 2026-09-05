using Assets.Scripts.Core;
using System;
using System.Runtime.CompilerServices;

namespace Assets.Scripts.Solver.Coordinates
{
    internal struct Phase1AxisCoordinateView
    {
        public int CornerOrientationIndex;
        public int EdgeOrientationIndex;
        public int SlicePositionIndex;

        public Phase1AxisCoordinateView(
            int cornerOrientationIndex,
            int edgeOrientationIndex,
            int slicePositionIndex)
        {
            CornerOrientationIndex = cornerOrientationIndex;
            EdgeOrientationIndex = edgeOrientationIndex;
            SlicePositionIndex = slicePositionIndex;
        }
    }

    public static class Phase1AxisCoordinate
    {
        public const int UdAxisView = 0;
        public const int FirstRotatedAxisView = 1;
        public const int SecondRotatedAxisView = 2;

        internal static Phase1AxisCoordinateView CreateView(
            SolverStateData state,
            int axisViewIndex)
        {
            SolverStateData transformed =
                Phase1Symmetry.TransformToAxisView(state, axisViewIndex);

            return new Phase1AxisCoordinateView(
                Phase1Coordinate.GetCornerOrientationIndex(transformed),
                Phase1Coordinate.GetEdgeOrientationIndex(transformed),
                Phase1Coordinate.GetSlicePositionIndex(transformed));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Phase1AxisCoordinateView MovePrepared(
            Phase1AxisCoordinateView view,
            int axisViewIndex,
            int originalMoveId)
        {
            int mappedMoveId = Phase1Symmetry.GetAxisConjugatedMoveIdPrepared(
                axisViewIndex,
                originalMoveId);

            return new Phase1AxisCoordinateView(
                Phase1MoveTables.GetCornerOrientationAfterMovePrepared(
                    view.CornerOrientationIndex,
                    mappedMoveId),
                Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(
                    view.EdgeOrientationIndex,
                    mappedMoveId),
                Phase1MoveTables.GetSlicePositionAfterMovePrepared(
                    view.SlicePositionIndex,
                    mappedMoveId));
        }
    }

    public static class Phase1Symmetry
    {
        public const int Count = 16;
        public const int AxisViewCount = 3;

        private static readonly int[] OurCornerToStandard = { 0, 1, 3, 2, 4, 5, 7, 6 };
        private static readonly int[] OurEdgeToStandard = { 0, 2, 3, 1, 4, 6, 7, 5, 8, 9, 11, 10 };

        private static readonly SymmetryCubie[] Symmetries = BuildSymmetries();
        private static readonly int[] InverseIndices = BuildInverseIndices();
        private static readonly int[] ConjugatedMoveIds = BuildConjugatedMoveIds();
        private static readonly SymmetryCubie[] AxisViews = BuildAxisViews();
        private static readonly int[] AxisViewInverseIndices = { 0, 2, 1 };
        private static readonly int[] AxisConjugatedMoveIds = BuildAxisConjugatedMoveIds();

        public static int GetInverseIndex(int symmetryIndex)
        {
            ValidateSymmetryIndex(symmetryIndex);
            return InverseIndices[symmetryIndex];
        }

        public static int GetConjugatedMoveId(int symmetryIndex, int moveId)
        {
            ValidateSymmetryIndex(symmetryIndex);
            return ConjugatedMoveIds[symmetryIndex * MoveGenerator.AllMoves.Length + moveId];
        }

        public static int GetAxisConjugatedMoveId(int axisViewIndex, int moveId)
        {
            ValidateAxisViewIndex(axisViewIndex);

            if (moveId < 0 || moveId >= MoveGenerator.AllMoves.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(moveId));
            }

            return GetAxisConjugatedMoveIdPrepared(axisViewIndex, moveId);
        }

        internal static int GetAxisConjugatedMoveIdPrepared(int axisViewIndex, int moveId)
        {
            return AxisConjugatedMoveIds[
                axisViewIndex * MoveGenerator.AllMoves.Length + moveId];
        }

        public static SolverStateData Transform(SolverStateData state, int symmetryIndex)
        {
            ValidateSymmetryIndex(symmetryIndex);

            SymmetryCubie transformed = Symmetries[symmetryIndex].Clone();
            transformed.Multiply(SymmetryCubie.FromState(state));
            transformed.Multiply(Symmetries[InverseIndices[symmetryIndex]]);
            return transformed.ToState();
        }

        public static SolverStateData TransformToAxisView(
            SolverStateData state,
            int axisViewIndex)
        {
            ValidateAxisViewIndex(axisViewIndex);

            SymmetryCubie transformed = AxisViews[axisViewIndex].Clone();
            transformed.Multiply(SymmetryCubie.FromState(state));
            transformed.Multiply(AxisViews[AxisViewInverseIndices[axisViewIndex]]);
            return transformed.ToState();
        }

        internal static int TransformCornerOrientationIndex(int cornerOrientationIndex, int symmetryIndex)
        {
            int[] cornerPermutation = IdentityPermutation(8);
            int[] cornerOrientation = CornerCoordinate.GetOrientationFromIndex(cornerOrientationIndex);
            SymmetryCubie orientationState = SymmetryCubie.FromCorners(cornerPermutation, cornerOrientation);

            SymmetryCubie transformed = Symmetries[symmetryIndex].Clone();
            transformed.CornerMultiply(orientationState);
            transformed.CornerMultiply(Symmetries[InverseIndices[symmetryIndex]]);

            return CornerCoordinate.GetOrientationIndex(transformed.CornerOrientation);
        }

        internal static void TransformEdgesInverseFirst(
            int[] edgePermutation,
            int[] edgeOrientation,
            int symmetryIndex,
            int[] transformedPermutation,
            int[] transformedOrientation)
        {
            ValidateSymmetryIndex(symmetryIndex);

            SymmetryCubie left = Symmetries[InverseIndices[symmetryIndex]];
            SymmetryCubie right = Symmetries[symmetryIndex];

            for (int edge = 0; edge < edgePermutation.Length; edge++)
            {
                int rightPosition = right.EdgePermutation[edge];
                int movedPiece = edgePermutation[rightPosition];

                transformedPermutation[edge] = left.EdgePermutation[movedPiece];
                transformedOrientation[edge] =
                    (right.EdgeOrientation[edge]
                    + edgeOrientation[rightPosition]
                    + left.EdgeOrientation[movedPiece]) % 2;
            }
        }

        internal static void TransformEdges(
            int[] edgePermutation,
            int[] edgeOrientation,
            int symmetryIndex,
            int[] transformedPermutation,
            int[] transformedOrientation)
        {
            ValidateSymmetryIndex(symmetryIndex);

            SymmetryCubie left = Symmetries[symmetryIndex];
            SymmetryCubie right = Symmetries[InverseIndices[symmetryIndex]];

            for (int edge = 0; edge < edgePermutation.Length; edge++)
            {
                int rightPosition = right.EdgePermutation[edge];
                int movedPiece = edgePermutation[rightPosition];

                transformedPermutation[edge] = left.EdgePermutation[movedPiece];
                transformedOrientation[edge] =
                    (right.EdgeOrientation[edge]
                    + edgeOrientation[rightPosition]
                    + left.EdgeOrientation[movedPiece]) % 2;
            }
        }

        private static SymmetryCubie[] BuildSymmetries()
        {
            SymmetryCubie rotationF2 = FromStandard(
                new int[] { 5, 4, 7, 6, 1, 0, 3, 2 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 6, 5, 4, 7, 2, 1, 0, 3, 9, 8, 11, 10 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            SymmetryCubie rotationU4 = FromStandard(
                new int[] { 3, 0, 1, 2, 7, 4, 5, 6 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 3, 0, 1, 2, 7, 4, 5, 6, 11, 8, 9, 10 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1 });

            SymmetryCubie mirrorLR = FromStandard(
                new int[] { 1, 0, 3, 2, 5, 4, 7, 6 },
                new int[] { 3, 3, 3, 3, 3, 3, 3, 3 },
                new int[] { 2, 1, 0, 3, 6, 5, 4, 7, 9, 8, 11, 10 },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            SymmetryCubie[] symmetries = new SymmetryCubie[Count];
            SymmetryCubie current = SymmetryCubie.Identity();
            int index = 0;

            for (int f2 = 0; f2 < 2; f2++)
            {
                for (int u4 = 0; u4 < 4; u4++)
                {
                    for (int mirror = 0; mirror < 2; mirror++)
                    {
                        symmetries[index] = current.Clone();
                        index++;
                        current.Multiply(mirrorLR);
                    }

                    current.Multiply(rotationU4);
                }

                current.Multiply(rotationF2);
            }

            if (index != Count)
            {
                throw new InvalidOperationException("Incorrect number of Phase 1 symmetries");
            }

            return symmetries;
        }

        private static SymmetryCubie[] BuildAxisViews()
        {
            SymmetryCubie rotationUrf3 = FromStandard(
                new int[] { 0, 4, 5, 1, 3, 7, 6, 2 },
                new int[] { 1, 2, 1, 2, 2, 1, 2, 1 },
                new int[] { 1, 8, 5, 9, 3, 11, 7, 10, 0, 4, 6, 2 },
                new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, 1 });

            SymmetryCubie[] axisViews = new SymmetryCubie[AxisViewCount];
            axisViews[0] = SymmetryCubie.Identity();
            axisViews[1] = rotationUrf3;
            axisViews[2] = rotationUrf3.Clone();
            axisViews[2].Multiply(rotationUrf3);

            SymmetryCubie fullCycle = axisViews[2].Clone();
            fullCycle.Multiply(rotationUrf3);
            if (!fullCycle.IsIdentity())
            {
                throw new InvalidOperationException(
                    "The three Phase 1 axis views did not form a complete rotation cycle");
            }

            return axisViews;
        }

        private static int[] BuildInverseIndices()
        {
            int[] inverseIndices = new int[Count];

            for (int symmetryIndex = 0; symmetryIndex < Count; symmetryIndex++)
            {
                inverseIndices[symmetryIndex] = -1;

                for (int candidateIndex = 0; candidateIndex < Count; candidateIndex++)
                {
                    SymmetryCubie product = Symmetries[symmetryIndex].Clone();
                    product.Multiply(Symmetries[candidateIndex]);

                    if (product.IsIdentity())
                    {
                        inverseIndices[symmetryIndex] = candidateIndex;
                        break;
                    }
                }

                if (inverseIndices[symmetryIndex] < 0)
                {
                    throw new InvalidOperationException("Phase 1 symmetry has no inverse");
                }
            }

            return inverseIndices;
        }

        private static int[] BuildConjugatedMoveIds()
        {
            int moveCount = MoveGenerator.AllMoves.Length;
            int[] conjugatedMoveIds = new int[Count * moveCount];
            SymmetryCubie[] moveCubies = new SymmetryCubie[moveCount];

            for (int moveId = 0; moveId < moveCount; moveId++)
            {
                SolverStateData state = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
                MoveProcessor.ApplyMove(state, moveId);
                moveCubies[moveId] = SymmetryCubie.FromState(state);
            }

            for (int symmetryIndex = 0; symmetryIndex < Count; symmetryIndex++)
            {
                for (int moveId = 0; moveId < moveCount; moveId++)
                {
                    SymmetryCubie transformedMove = Symmetries[symmetryIndex].Clone();
                    transformedMove.Multiply(moveCubies[moveId]);
                    transformedMove.Multiply(Symmetries[InverseIndices[symmetryIndex]]);

                    int matchingMoveId = -1;

                    for (int candidateMoveId = 0; candidateMoveId < moveCount; candidateMoveId++)
                    {
                        if (transformedMove.Equals(moveCubies[candidateMoveId]))
                        {
                            matchingMoveId = candidateMoveId;
                            break;
                        }
                    }

                    if (matchingMoveId < 0)
                    {
                        throw new InvalidOperationException(
                            "Symmetry " + symmetryIndex
                            + " did not map move " + MoveGenerator.GetMoveName(moveId)
                            + " to another legal move");
                    }

                    conjugatedMoveIds[symmetryIndex * moveCount + moveId] = matchingMoveId;
                }
            }

            return conjugatedMoveIds;
        }

        private static int[] BuildAxisConjugatedMoveIds()
        {
            int moveCount = MoveGenerator.AllMoves.Length;
            int[] conjugatedMoveIds = new int[AxisViewCount * moveCount];
            SymmetryCubie[] moveCubies = new SymmetryCubie[moveCount];

            for (int moveId = 0; moveId < moveCount; moveId++)
            {
                SolverStateData state =
                    SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
                MoveProcessor.ApplyMove(state, moveId);
                moveCubies[moveId] = SymmetryCubie.FromState(state);
            }

            for (int axisViewIndex = 0; axisViewIndex < AxisViewCount; axisViewIndex++)
            {
                for (int moveId = 0; moveId < moveCount; moveId++)
                {
                    SymmetryCubie transformedMove = AxisViews[axisViewIndex].Clone();
                    transformedMove.Multiply(moveCubies[moveId]);
                    transformedMove.Multiply(
                        AxisViews[AxisViewInverseIndices[axisViewIndex]]);

                    int matchingMoveId = -1;

                    for (int candidateMoveId = 0;
                        candidateMoveId < moveCount;
                        candidateMoveId++)
                    {
                        if (transformedMove.Equals(moveCubies[candidateMoveId]))
                        {
                            matchingMoveId = candidateMoveId;
                            break;
                        }
                    }

                    if (matchingMoveId < 0)
                    {
                        throw new InvalidOperationException(
                            "Axis view " + axisViewIndex
                            + " did not map move " + MoveGenerator.GetMoveName(moveId)
                            + " to another legal move");
                    }

                    conjugatedMoveIds[axisViewIndex * moveCount + moveId] = matchingMoveId;
                }
            }

            return conjugatedMoveIds;
        }

        private static SymmetryCubie FromStandard(
            int[] standardCornerPermutation,
            int[] standardCornerOrientation,
            int[] standardEdgePermutation,
            int[] standardEdgeOrientation)
        {
            int[] standardCornerToOur = InvertMapping(OurCornerToStandard);
            int[] standardEdgeToOur = InvertMapping(OurEdgeToStandard);
            int[] cornerPermutation = new int[8];
            int[] cornerOrientation = new int[8];
            int[] edgePermutation = new int[12];
            int[] edgeOrientation = new int[12];

            for (int ourPosition = 0; ourPosition < cornerPermutation.Length; ourPosition++)
            {
                int standardPosition = OurCornerToStandard[ourPosition];
                cornerPermutation[ourPosition] = standardCornerToOur[standardCornerPermutation[standardPosition]];
                cornerOrientation[ourPosition] = standardCornerOrientation[standardPosition];
            }

            for (int ourPosition = 0; ourPosition < edgePermutation.Length; ourPosition++)
            {
                int standardPosition = OurEdgeToStandard[ourPosition];
                edgePermutation[ourPosition] = standardEdgeToOur[standardEdgePermutation[standardPosition]];
                edgeOrientation[ourPosition] = standardEdgeOrientation[standardPosition];
            }

            return new SymmetryCubie(
                cornerPermutation,
                cornerOrientation,
                edgePermutation,
                edgeOrientation);
        }

        private static int[] InvertMapping(int[] mapping)
        {
            int[] inverse = new int[mapping.Length];

            for (int i = 0; i < mapping.Length; i++)
            {
                inverse[mapping[i]] = i;
            }

            return inverse;
        }

        private static int[] IdentityPermutation(int length)
        {
            int[] permutation = new int[length];

            for (int i = 0; i < length; i++)
            {
                permutation[i] = i;
            }

            return permutation;
        }

        private static void ValidateSymmetryIndex(int symmetryIndex)
        {
            if (symmetryIndex < 0 || symmetryIndex >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(symmetryIndex));
            }
        }

        private static void ValidateAxisViewIndex(int axisViewIndex)
        {
            if (axisViewIndex < 0 || axisViewIndex >= AxisViewCount)
            {
                throw new ArgumentOutOfRangeException(nameof(axisViewIndex));
            }
        }

        private sealed class SymmetryCubie : IEquatable<SymmetryCubie>
        {
            public int[] CornerPermutation { get; private set; }
            public int[] CornerOrientation { get; private set; }
            public int[] EdgePermutation { get; private set; }
            public int[] EdgeOrientation { get; private set; }

            public SymmetryCubie(
                int[] cornerPermutation,
                int[] cornerOrientation,
                int[] edgePermutation,
                int[] edgeOrientation)
            {
                CornerPermutation = (int[])cornerPermutation.Clone();
                CornerOrientation = (int[])cornerOrientation.Clone();
                EdgePermutation = (int[])edgePermutation.Clone();
                EdgeOrientation = (int[])edgeOrientation.Clone();
            }

            public static SymmetryCubie Identity()
            {
                return new SymmetryCubie(
                    IdentityPermutation(8),
                    new int[8],
                    IdentityPermutation(12),
                    new int[12]);
            }

            public static SymmetryCubie FromState(SolverStateData state)
            {
                return new SymmetryCubie(
                    state.CornerPermutation,
                    state.CornerOrientation,
                    state.FullEdgePermutation,
                    state.FullEdgeOrientation);
            }

            public static SymmetryCubie FromCorners(int[] cornerPermutation, int[] cornerOrientation)
            {
                return new SymmetryCubie(
                    cornerPermutation,
                    cornerOrientation,
                    IdentityPermutation(12),
                    new int[12]);
            }

            public SymmetryCubie Clone()
            {
                return new SymmetryCubie(
                    CornerPermutation,
                    CornerOrientation,
                    EdgePermutation,
                    EdgeOrientation);
            }

            public SolverStateData ToState()
            {
                int[] cornerOrientation = (int[])CornerOrientation.Clone();

                for (int i = 0; i < cornerOrientation.Length; i++)
                {
                    if (cornerOrientation[i] >= 3)
                    {
                        throw new InvalidOperationException("A transformed cube remained mirrored");
                    }
                }

                return new SolverStateData
                {
                    CornerPermutation = (int[])CornerPermutation.Clone(),
                    CornerOrientation = cornerOrientation,
                    FullEdgePermutation = (int[])EdgePermutation.Clone(),
                    FullEdgeOrientation = (int[])EdgeOrientation.Clone()
                };
            }

            public void Multiply(SymmetryCubie other)
            {
                CornerMultiply(other);
                EdgeMultiply(other);
            }

            public void CornerMultiply(SymmetryCubie other)
            {
                int[] newPermutation = new int[8];
                int[] newOrientation = new int[8];

                for (int corner = 0; corner < newPermutation.Length; corner++)
                {
                    newPermutation[corner] = CornerPermutation[other.CornerPermutation[corner]];

                    int orientationA = CornerOrientation[other.CornerPermutation[corner]];
                    int orientationB = other.CornerOrientation[corner];
                    int orientation;

                    if (orientationA < 3 && orientationB < 3)
                    {
                        orientation = orientationA + orientationB;
                        if (orientation >= 3)
                        {
                            orientation -= 3;
                        }
                    }
                    else if (orientationA < 3)
                    {
                        orientation = orientationA + orientationB;
                        if (orientation >= 6)
                        {
                            orientation -= 3;
                        }
                    }
                    else if (orientationB < 3)
                    {
                        orientation = orientationA - orientationB;
                        if (orientation < 3)
                        {
                            orientation += 3;
                        }
                    }
                    else
                    {
                        orientation = orientationA - orientationB;
                        if (orientation < 0)
                        {
                            orientation += 3;
                        }
                    }

                    newOrientation[corner] = orientation;
                }

                CornerPermutation = newPermutation;
                CornerOrientation = newOrientation;
            }

            public void EdgeMultiply(SymmetryCubie other)
            {
                int[] newPermutation = new int[12];
                int[] newOrientation = new int[12];

                for (int edge = 0; edge < newPermutation.Length; edge++)
                {
                    newPermutation[edge] = EdgePermutation[other.EdgePermutation[edge]];
                    newOrientation[edge] =
                        (other.EdgeOrientation[edge]
                        + EdgeOrientation[other.EdgePermutation[edge]]) % 2;
                }

                EdgePermutation = newPermutation;
                EdgeOrientation = newOrientation;
            }

            public bool IsIdentity()
            {
                return Equals(Identity());
            }

            public bool Equals(SymmetryCubie other)
            {
                return other != null
                    && ArraysEqual(CornerPermutation, other.CornerPermutation)
                    && ArraysEqual(CornerOrientation, other.CornerOrientation)
                    && ArraysEqual(EdgePermutation, other.EdgePermutation)
                    && ArraysEqual(EdgeOrientation, other.EdgeOrientation);
            }

            private static bool ArraysEqual(int[] first, int[] second)
            {
                if (first.Length != second.Length)
                {
                    return false;
                }

                for (int i = 0; i < first.Length; i++)
                {
                    if (first[i] != second[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
