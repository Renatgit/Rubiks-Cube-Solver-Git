using System.Collections.Generic;
using RubiksCubeSim;
public static class MoveProcessor
{
    //MoveMaps maps Rubik's Cube moves to their corresponding MoveData
    //Each MoveData contains:
    //- CornerPermutation: Defines how the corners are permuted for the move
    //- EdgePermutation: Defines how the edges are permuted for the move
    private static Dictionary<string, MoveData> MoveMaps = new Dictionary<string, MoveData>()
    {
        { "U", new MoveData(
            new int[] {1, 3, 0, 2, 4, 5, 6, 7}, //Corner Permutation Map
            new int[] { 2, 3, 1, 0, 4, 5, 6, 7, 8, 9, 10, 11 } //FullEdgePermutation map
        ) },
        { "U'", new MoveData( 
            new int[] {2, 0, 3, 1, 4, 5, 6, 7}, 
            new int[] { 3, 2, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11 }
        ) },
        { "U2", new MoveData( 
            new int[] {3, 2, 1, 0, 4, 5, 6, 7}, 
            new int[] { 1, 0, 3, 2, 4, 5, 6, 7, 8, 9, 10, 11 }
        ) },
        { "L", new MoveData( 
            new int[] { 0, 5, 2, 1, 4, 7, 6, 3 },
            new int[] { 0, 11, 2, 3, 4, 9, 6, 7, 8, 1, 10, 5 }
        ) },
        { "L'", new MoveData(
            new int[] { 0, 3, 2, 7, 4, 1, 6, 5 },
            new int[] { 0, 9, 2, 3, 4, 11, 6, 7, 8, 5, 10, 1 }
        ) },
        { "L2", new MoveData(
            new int[] { 0, 7, 2, 5, 4, 3, 6, 1 },
            new int[] { 0, 5, 2, 3, 4, 1, 6, 7, 8, 11, 10, 9 }
        ) },
        { "R", new MoveData(
            new int[] { 2, 1, 6, 3, 0, 5, 4, 7 },
            new int[] { 8, 1, 2, 3, 10, 5, 6, 7, 4, 9, 0, 11 }
        ) },
        { "R'", new MoveData(
            new int[] { 4, 1, 0, 3, 6, 5, 2, 7 },
            new int[] { 10, 1, 2, 3, 8, 5, 6, 7, 0, 9, 4, 11 }
        ) },
        { "R2", new MoveData(
            new int[] { 6, 1, 4, 3, 2, 5, 0, 7 },
            new int[] { 4, 1, 2, 3, 0, 5, 6, 7, 10, 9, 8, 11 }
        ) },
        { "D", new MoveData(
            new int[] { 0, 1, 2, 3, 6, 4, 7, 5 },
            new int[] { 0, 1, 2, 3, 7, 6, 4, 5, 8, 9, 10, 11 }
        ) },
        { "D'", new MoveData(
            new int[] { 0, 1, 2, 3, 5, 7, 4, 6 },
            new int[] { 0, 1, 2, 3, 6, 7, 5, 4, 8, 9, 10, 11 }
        ) },
        { "D2", new MoveData(
            new int[] { 0, 1, 2, 3, 7, 6, 5, 4 },
            new int[] { 0, 1, 2, 3, 5, 4, 7, 6, 8, 9, 10, 11 }
        ) },
        { "F", new MoveData(
            new int[] { 4, 0, 2, 3, 5, 1, 6, 7 },
            new int[] { 0, 1, 2, 9, 4, 5, 6, 8, 3, 7, 10, 11 }
        ) },
        { "F'", new MoveData(
            new int[] { 1, 5, 2, 3, 0, 4, 6, 7 },
            new int[] { 0, 1, 2, 8, 4, 5, 6, 9, 7, 3, 10, 11 }
        ) },
        { "F2", new MoveData(
            new int[] { 5, 4, 2, 3, 1, 0, 6, 7 },
            new int[] { 0, 1, 2, 7, 4, 5, 6, 3, 9, 8, 10, 11 }
        ) },
        { "B'", new MoveData(
            new int[] { 0, 1, 6, 2, 4, 5, 7, 3 },
            new int[] { 0, 1, 10, 3, 4, 5, 11, 7, 8, 9, 6, 2 }
        ) },
        { "B", new MoveData(
            new int[] { 0, 1, 3, 7, 4, 5, 2, 6 },
            new int[] { 0, 1, 11, 3, 4, 5, 10, 7, 8, 9, 2, 6 }
        ) },
        { "B2", new MoveData(
            new int[] { 0, 1, 7, 6, 4, 5, 3, 2 },
            new int[] { 0, 1, 6, 3, 4, 5, 2, 7, 8, 9, 11, 10 }
        ) }

    };

    //Corner orientation deltas
    private static readonly Dictionary<string, int[]> CornerOrientationDeltas = new Dictionary<string, int[]>()
    {
        { "U",  new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "U'", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "U2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "D",  new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "D'", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "D2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "R",  new int[] { 2, 0, 1, 0, 1, 0, 2, 0 } },
        { "R'", new int[] { 2, 0, 1, 0, 1, 0, 2, 0 } },
        { "R2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "L",  new int[] { 0, 1, 0, 2, 0, 2, 0, 1 } },
        { "L'", new int[] { 0, 1, 0, 2, 0, 2, 0, 1 } },
        { "L2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "F",  new int[] { 1, 2, 0, 0, 2, 1, 0, 0 } },
        { "F'", new int[] { 1, 2, 0, 0, 2, 1, 0, 0 } },
        { "F2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "B",  new int[] { 0, 0, 2, 1, 0, 0, 1, 2 } },
        { "B'", new int[] { 0, 0, 2, 1, 0, 0, 1, 2 } },
        { "B2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0 } }
    };

    //Edge orientation deltas
    private static readonly Dictionary<string, int[]> EdgeOrientationDeltas = new Dictionary<string, int[]>()
    {
        { "U",  new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "U'", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "U2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "D",  new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "D'", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "D2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "R",  new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "R'", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "R2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

        { "L",  new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "L'", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
        { "L2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

        // F affects UF, DF, FR and FL.
        { "F",  new int[] { 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, 0 } },
        { "F'", new int[] { 0, 0, 0, 1, 0, 0, 0, 1, 1, 1, 0, 0 } },
        { "F2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

        // B affects UB, DB, BR and BL.
        { "B",  new int[] { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 1 } },
        { "B'", new int[] { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 1 } },
        { "B2", new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }
    };


    public static void ApplyMove(CubeStateData state, string move, bool recordMoveHistory = true)
    {
        //Updates the CORNER PERMUTATION indexing
        List<int> newCornerPerm = new List<int>(state.cornerPermutation);
        for (int oldPosition = 0; oldPosition < 8; oldPosition++)
        {
            int newPosition = MoveMaps[move].CornerPermutation[oldPosition];
            newCornerPerm[newPosition] = state.cornerPermutation[oldPosition];
        }

        //Updates the CORNER ORIENTATION
        List<int> newCornerOrient = new List<int>(state.cornerOrientation);
        newCornerOrient = GetNewCornerOrientation(state.cornerOrientation, move);

        //Updates the EDGE PERMUTATION
        List<int> newFullEdgePermutation = new List<int>(state.fullEdgePermutation);
        for (int i = 0; i < 12; i++)
        {
            newFullEdgePermutation[i] = state.fullEdgePermutation[MoveMaps[move].FullEdgePermutation[i]];
        }

        //Updates the EDGE ORIENTATION
        List<int> newFullEdgeOrientation = new List<int>(state.fullEdgeOrientation);
        newFullEdgeOrientation = GetNewEdgeOrientation(state.fullEdgeOrientation, move);

        state.cornerPermutation = newCornerPerm;
        state.cornerOrientation = newCornerOrient;
        state.fullEdgePermutation = newFullEdgePermutation;
        state.fullEdgeOrientation = newFullEdgeOrientation;
        UpdateDerivedEdgeState(state);

        state.depth += 1;
        if (!recordMoveHistory)
        {
            return;
        }

        if (state.moveHistory == null)
        {
            state.moveHistory = new List<string>();
        }
        state.moveHistory.Add(move);
    }

    private static void UpdateDerivedEdgeState(CubeStateData state)
    {
        state.firstEightEdgePermutation = new List<int>();
        state.lastFourEdgePermutation = new List<int>();
        state.firstEightEdgeOrientation = new List<int>();
        state.lastFourEdgeOrientation = new List<int>();

        for (int position = 0; position < state.fullEdgePermutation.Count; position++)
        {
            int edgePiece = state.fullEdgePermutation[position];

            if (edgePiece < 8)
            {
                state.firstEightEdgePermutation.Add(edgePiece);
                state.firstEightEdgeOrientation.Add(state.fullEdgeOrientation[position]);
            }
            else
            {
                state.lastFourEdgePermutation.Add(edgePiece);
                state.lastFourEdgeOrientation.Add(state.fullEdgeOrientation[position]);
            }
        }
    }

    private static List<int> GetNewEdgeOrientation(List<int> currentOrientation, string move)
    {
        List<int> newOrientation = new List<int>(currentOrientation);
        int[] permutationMap = MoveMaps[move].FullEdgePermutation;
        int[] orientationDelta = EdgeOrientationDeltas[move];

        for (int newPosition = 0; newPosition < 12; newPosition++)
        {
            int oldPosition = permutationMap[newPosition];
            newOrientation[newPosition] =
                (currentOrientation[oldPosition] + orientationDelta[newPosition]) % 2;
        }

        return newOrientation;
    }

    private static List<int> GetNewCornerOrientation(List<int> currentOrientation, string move)
    {
        List<int> newOrientation = new List<int>(currentOrientation);
        int[] permutationMap = MoveMaps[move].CornerPermutation;
        int[] orientationDelta = CornerOrientationDeltas[move];

        for (int oldPosition = 0; oldPosition < 8; oldPosition++)
        {
            int newPosition = permutationMap[oldPosition];
            newOrientation[newPosition] = (currentOrientation[oldPosition] + orientationDelta[newPosition]) % 3;
        }
        return newOrientation;
    }
}
