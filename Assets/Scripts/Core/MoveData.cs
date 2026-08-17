namespace RubiksCubeSim
{
    // Stores permutation maps for each move.
    public class MoveData
    {
        public int[] CornerPermutation { get; set; }
        public int[] FullEdgePermutation { get; set; }

        public MoveData(int[] cornerPermutation, int[] fullEdgePermutation)
        {
            CornerPermutation = cornerPermutation;
            FullEdgePermutation = fullEdgePermutation;
        }
    }
}
