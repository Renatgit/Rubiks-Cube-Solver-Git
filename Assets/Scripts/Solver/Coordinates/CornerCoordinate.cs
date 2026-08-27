using System.Collections.Generic;

namespace Assets.Scripts.Solver.Coordinates
{
    public static class CornerCoordinate 
    {
        private static readonly int[] Factorials =
        {
            1,      // 0!
            1,      // 1!
            2,      // 2!
            6,      // 3!
            24,     // 4!
            120,    // 5!
            720,    // 6!
            5040,   // 7!
            40320   // 8!
        };
        public const int CornerOrientationCount = 2187;

        // Encodes the 7 independent corner orientations as a base-3 number
        // The 8th orientation is ignored because it is determined by the rule
        public static int GetOrientationIndex(int[] orientation)
        {
            int index = 0;

            for (int i = 0; i < 7; i++) {  
                index = (index * 3) + orientation[i];
            }

            return index;
        }

        // Reverse of GetOrientationIndex
        public static int[] GetOrientationFromIndex(int index)
        {
            int[] orientation = new int[8];
            int orientationSum = 0;

            for (int i = 6; i >= 0; i--)
            {
                orientation[i] = index % 3;
                orientationSum += orientation[i];
                index /= 3;
            }

            orientation[7] = (3 - (orientationSum % 3)) % 3;
            return orientation;
        }

        // // Encodes the 8 corner permutation into a Lehmer Code "rank"
        public static int GetPermutationIndex(int[] permutation)
        {
            int index = 0;

            for(int i = 0; i < 8; i++)
            {
                int smallerNumsOnRight = 0;
                // Creates the "Code" based on smaller numbers on the right to current number
                for(int j = i + 1; j < 8; j++)
                {
                    if (permutation[j] < permutation[i])
                    {
                        smallerNumsOnRight++;
                    }
                }
                // Times current number by the factorial based on position
                index += smallerNumsOnRight * Factorials[7-i]; 
            }
            return index;
        }

        // Reverse of GetPermutationIndex
        public static int[] GetPermutationFromIndex(int index)
        {
            int[] permutation = new int[8];
            List<int> remainingCorners = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

            for (int i = 0; i < 8; i++)
            {
                int factorial = Factorials[7 - i];
                int selectedIndex = index / factorial;
                index %= factorial;

                permutation[i] = remainingCorners[selectedIndex];
                remainingCorners.RemoveAt(selectedIndex);
            }

            return permutation;
        }

        // Get full index using formula: permIndex * 2187(3^7) + orientIndex 
        public static int GetIndex(int[] permutation, int[] orientation)
        {
            int permutationIndex = GetPermutationIndex(permutation);
            int orientationIndex = GetOrientationIndex(orientation);

            return permutationIndex * CornerOrientationCount + orientationIndex;
        }
    }
}
