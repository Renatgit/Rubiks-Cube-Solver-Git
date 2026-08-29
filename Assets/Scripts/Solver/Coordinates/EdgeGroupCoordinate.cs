using System.Collections.Generic;

namespace Assets.Scripts.Solver.Coordinates
{
    public static class EdgeGroupCoordinate
    {
        public const int EdgeCount = 12;
        public const int TrackedEdgeCount = 6;
        public const int OrientationCount = 64;
        public const int PermutationCount = 720;
        public const int PositionCount = 924;

        private static readonly int[][] PositionsByIndex = BuildPositionsByIndex();

        private static readonly int[] Factorials =
        {
            1,      // 0!
            1,      // 1!
            2,      // 2!
            6,      // 3!
            24,     // 4!
            120,    // 5!
            720,    // 6!
        };

        // Gets full index to store
        public static int GetIndex(int[] fullEdgePermutation, int[] fullEdgeOrientation, int[] trackedEdges)
        {
            List<int> positions = new List<int>();
            List<int> permutation = new List<int>();
            List<int> orientation = new List<int>();

            for (int position = 0; position < EdgeCount; position++)
            {
                int edge = fullEdgePermutation[position];

                if (IsTrackedEdge(edge, trackedEdges))
                {
                    positions.Add(position);
                    permutation.Add(GetTrackedEdgeLocalIndex(edge, trackedEdges));
                    orientation.Add(fullEdgeOrientation[position]);
                }
            }

            int positionIndex = GetPositionIndex(positions.ToArray());
            int permutationIndex = GetPermutationIndex(permutation.ToArray());
            int orientationIndex = GetOrientationIndex(orientation.ToArray());

            return ((positionIndex * PermutationCount) + permutationIndex) * OrientationCount + orientationIndex;
        }


        // Return base-2 number as orientation index
        public static int GetOrientationIndex(int[] groupOrientation)
        {
            int index = 0;

            for (int i = 0; i < TrackedEdgeCount; i++)
            {
                index = index * 2 + groupOrientation[i];
            }

            return index;
        }
        public static int[] GetOrientationFromIndex(int index)
        {
            int[] orientation = new int[TrackedEdgeCount];

            for (int i = TrackedEdgeCount - 1; i >= 0; i--)
            {
                orientation[i] = index % 2;
                index = index / 2;
            }

            return orientation;
        }

        public static int GetPermutationIndex(int[] permutation)
        {
            int index = 0;

            for (int i = 0;i < TrackedEdgeCount; i++)
            {
                int smallerNumbersOnRight = 0;
                for (int j = i + 1; j < TrackedEdgeCount; j++)
                {
                    if (permutation[j] < permutation[i])
                    {
                        smallerNumbersOnRight++;
                    }
                }
                index += smallerNumbersOnRight * Factorials[TrackedEdgeCount - 1 - i];
            }
            return index;
        }

        public static int[] GetPermutationFromIndex(int index)
        {
            List<int> availableEdges = new List<int>();
            int[] permutation = new int[TrackedEdgeCount];

            for (int i = 0; i < TrackedEdgeCount; i++)
            {
                availableEdges.Add(i);
            }

            for (int i = 0; i < TrackedEdgeCount; i++)
            {
                int factorial = Factorials[TrackedEdgeCount - 1 - i];
                int selectedIndex = index / factorial;
                index = index % factorial;

                permutation[i] = availableEdges[selectedIndex];
                availableEdges.RemoveAt(selectedIndex);
            }

            return permutation;
        }

        public static int GetPositionIndex(int[] positions)
        {
            int index = 0;
            int remaining = TrackedEdgeCount;

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

        public static int[] GetPositionsFromIndex(int index)
        {
            int[] positions = new int[TrackedEdgeCount];
            System.Array.Copy(PositionsByIndex[index], positions, TrackedEdgeCount);
            return positions;
        }

        public static void SplitIndex(int edgeGroupIndex, out int positionIndex, out int permutationIndex, out int orientationIndex)
        {
            orientationIndex = edgeGroupIndex % OrientationCount;
            edgeGroupIndex = edgeGroupIndex / OrientationCount;

            permutationIndex = edgeGroupIndex % PermutationCount;
            positionIndex = edgeGroupIndex / PermutationCount;
        }

        private static int[][] BuildPositionsByIndex()
        {
            int[][] positionsByIndex = new int[PositionCount][];

            for (int a = 0; a <= 6; a++)
            {
                for (int b = a + 1; b <= 7; b++)
                {
                    for (int c = b + 1; c <= 8; c++)
                    {
                        for (int d = c + 1; d <= 9; d++)
                        {
                            for (int e = d + 1; e <= 10; e++)
                            {
                                for (int f = e + 1; f <= 11; f++)
                                {
                                    int[] positions = { a, b, c, d, e, f };
                                    positionsByIndex[GetPositionIndex(positions)] = positions;
                                }
                            }
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


        private static bool IsTrackedEdge(int edge, int[] trackedEdges)
        {
            for (int i = 0; i < trackedEdges.Length; i++)
            {
                if (trackedEdges[i] == edge)
                {
                    return true;
                }
            }
            return false;
        }
        private static int GetTrackedEdgeLocalIndex(int edge, int[] trackedEdges)
        {
            for (int i = 0; i < trackedEdges.Length; i++)
            {
                if (trackedEdges[i] == edge)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
