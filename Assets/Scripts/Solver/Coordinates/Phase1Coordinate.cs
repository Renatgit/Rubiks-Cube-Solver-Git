namespace Assets.Scripts.Solver.Coordinates
{
    public static class Phase1Coordinate
    {
        public const int CornerOrientationCount = 2187;
        public const int EdgeOrientationCount = 2048;
        public const int SlicePositionCount = 495;

        public const int CornerSliceCount = CornerOrientationCount * SlicePositionCount;
        public const int EdgeSliceCount = EdgeOrientationCount * SlicePositionCount;

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
    }
}
