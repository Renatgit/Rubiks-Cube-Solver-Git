namespace Assets.Scripts.Solver.Coordinates
{
    public static class Phase1Coordinate
    {
        public const int CornerOrientationCount = 2187;
        public const int EdgeOrientationCount = 2048;
        public const int SlicePositionCount = 495;
        public const int SlicePermutationCount = 24;
        public const int SliceArrangementCount = SlicePositionCount * SlicePermutationCount;

        public const int CornerSliceCount = CornerOrientationCount * SlicePositionCount;
        public const int EdgeSliceCount = EdgeOrientationCount * SlicePositionCount;
        public const int CornerPermutationSlicePositionCount =
            Phase2Coordinate.CornerPermutationCount * SlicePositionCount;
        public const int CornerPermutationEdgeOrientationCount =
            Phase2Coordinate.CornerPermutationCount * EdgeOrientationCount;

        private const int EdgeCount = 12;
        private const int SliceEdgeCount = 4;
        private const int FirstSliceEdge = 8;

        private static readonly int[][] SlicePositionsByIndex = BuildSlicePositionsByIndex();

        public static int GetCornerOrientationIndex(SolverStateData state)
        {
            return CornerCoordinate.GetOrientationIndex(state.CornerOrientation);
        }

        public static int GetEdgeOrientationIndex(SolverStateData state)
        {
            return GetEdgeOrientationIndex(state.FullEdgeOrientation);
        }

        public static int GetEdgeOrientationIndex(int[] fullEdgeOrientation)
        {
            int index = 0;

            for (int i = 0; i < 11; i++)
            {
                index = index * 2 + fullEdgeOrientation[i];
            }

            return index;
        }

        public static int[] GetEdgeOrientationFromIndex(int index)
        {
            int[] orientation = new int[EdgeCount];
            int orientationSum = 0;

            for (int i = 10; i >= 0; i--)
            {
                orientation[i] = index % 2;
                orientationSum += orientation[i];
                index = index / 2;
            }

            orientation[11] = orientationSum % 2;
            return orientation;
        }

        public static int GetSlicePositionIndex(SolverStateData state)
        {
            return GetSlicePositionIndex(state.FullEdgePermutation);
        }

        public static int GetSlicePositionIndex(int[] fullEdgePermutation)
        {
            int[] positions = new int[SliceEdgeCount];
            int found = 0;

            for (int position = 0; position < EdgeCount; position++)
            {
                if (fullEdgePermutation[position] >= FirstSliceEdge)
                {
                    positions[found] = position;
                    found++;
                }
            }

            return GetSlicePositionIndexFromPositions(positions);
        }

        public static int[] GetSlicePositionsFromIndex(int index)
        {
            int[] positions = new int[SliceEdgeCount];
            System.Array.Copy(SlicePositionsByIndex[index], positions, SliceEdgeCount);
            return positions;
        }

        public static int[] GetEdgePermutationFromSliceIndex(int index)
        {
            int[] edgePermutation = new int[EdgeCount];
            int[] slicePositions = SlicePositionsByIndex[index];
            int nextNonSliceEdge = 0;
            int nextSliceEdge = FirstSliceEdge;

            for (int position = 0; position < EdgeCount; position++)
            {
                bool isSlicePosition = false;

                for (int i = 0; i < slicePositions.Length; i++)
                {
                    if (slicePositions[i] == position)
                    {
                        isSlicePosition = true;
                        break;
                    }
                }

                edgePermutation[position] = isSlicePosition
                    ? nextSliceEdge++
                    : nextNonSliceEdge++;
            }

            return edgePermutation;
        }

        public static int GetSliceArrangementIndex(SolverStateData state)
        {
            return GetSliceArrangementIndex(state.FullEdgePermutation);
        }

        public static int GetSliceArrangementIndex(int[] fullEdgePermutation)
        {
            int[] positions = new int[SliceEdgeCount];
            int[] permutation = new int[SliceEdgeCount];
            int found = 0;

            for (int position = 0; position < EdgeCount; position++)
            {
                int edge = fullEdgePermutation[position];
                if (edge < FirstSliceEdge)
                {
                    continue;
                }

                positions[found] = position;
                permutation[found] = edge - FirstSliceEdge;
                found++;
            }

            int positionIndex = GetSlicePositionIndexFromPositions(positions);
            int permutationIndex = GetFourPiecePermutationIndex(permutation);
            return positionIndex * SlicePermutationCount + permutationIndex;
        }

        public static int[] GetEdgePermutationFromSliceArrangementIndex(int index)
        {
            int positionIndex = index / SlicePermutationCount;
            int permutationIndex = index % SlicePermutationCount;
            int[] positions = SlicePositionsByIndex[positionIndex];
            int[] sliceEdges = Phase2Coordinate.GetSlicePermutationFromIndex(permutationIndex);
            int[] edgePermutation = new int[EdgeCount];
            int nextNonSliceEdge = 0;
            int nextSlicePiece = 0;

            for (int position = 0; position < EdgeCount; position++)
            {
                if (nextSlicePiece < SliceEdgeCount && positions[nextSlicePiece] == position)
                {
                    edgePermutation[position] = sliceEdges[nextSlicePiece];
                    nextSlicePiece++;
                }
                else
                {
                    edgePermutation[position] = nextNonSliceEdge;
                    nextNonSliceEdge++;
                }
            }

            return edgePermutation;
        }

        public static int GetSlicePermutationIndexFromArrangement(int sliceArrangementIndex)
        {
            return sliceArrangementIndex % SlicePermutationCount;
        }

        public static int GetCornerSliceIndex(SolverStateData state)
        {
            return GetCornerOrientationIndex(state) * SlicePositionCount + GetSlicePositionIndex(state);
        }

        public static int GetEdgeSliceIndex(SolverStateData state)
        {
            return GetEdgeOrientationIndex(state) * SlicePositionCount + GetSlicePositionIndex(state);
        }

        public static int GetSlicePositionIndexFromPositions(int[] positions)
        {
            int index = 0;
            int remaining = SliceEdgeCount;

            for (int position = 0; position < EdgeCount; position++)
            {
                bool selected = false;

                for (int i = 0; i < positions.Length; i++)
                {
                    if (positions[i] == position)
                    {
                        selected = true;
                        break;
                    }
                }

                if (selected)
                {
                    remaining--;
                }
                else if (remaining > 0)
                {
                    index += Choose(EdgeCount - position - 1, remaining - 1);
                }
            }

            return index;
        }

        private static int[][] BuildSlicePositionsByIndex()
        {
            int[][] positionsByIndex = new int[SlicePositionCount][];

            for (int a = 0; a <= 8; a++)
            {
                for (int b = a + 1; b <= 9; b++)
                {
                    for (int c = b + 1; c <= 10; c++)
                    {
                        for (int d = c + 1; d <= 11; d++)
                        {
                            int[] positions = { a, b, c, d };
                            positionsByIndex[GetSlicePositionIndexFromPositions(positions)] = positions;
                        }
                    }
                }
            }

            return positionsByIndex;
        }

        private static int Choose(int n, int k)
        {
            if (k < 0 || k > n)
            {
                return 0;
            }

            if (k == 0 || k == n)
            {
                return 1;
            }

            int result = 1;

            for (int i = 1; i <= k; i++)
            {
                result = result * (n - k + i) / i;
            }

            return result;
        }

        private static int GetFourPiecePermutationIndex(int[] permutation)
        {
            int index = 0;

            for (int i = 0; i < SliceEdgeCount; i++)
            {
                int smallerNumbersOnRight = 0;

                for (int j = i + 1; j < SliceEdgeCount; j++)
                {
                    if (permutation[j] < permutation[i])
                    {
                        smallerNumbersOnRight++;
                    }
                }

                if (i == 0)
                {
                    index += smallerNumbersOnRight * 6;
                }
                else if (i == 1)
                {
                    index += smallerNumbersOnRight * 2;
                }
                else if (i == 2)
                {
                    index += smallerNumbersOnRight;
                }
            }

            return index;
        }
    }
}
