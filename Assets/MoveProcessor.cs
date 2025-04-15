using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RubiksCubeSim;
using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;
public class MoveProcessor : MonoBehaviour
{
    //MoveMaps maps Rubik's Cube moves to their corresponding MoveData
    //Each MoveData contains:
    //- CornerPermutation: Defines how the corners are permuted for the move
    //- CornerOrientationToIndex (Optional): Specifies the indices of corners affected by orientation changes
    //- CornerOrientationFromIndex (Optional): Maps the original orientation indices to the new ones
    private static Dictionary<string, MoveData> MoveMaps = new Dictionary<string, MoveData>()
    {
        { "U", new MoveData(
            new int[] {1, 3, 0, 2, 4, 5, 6, 7}, //Corner Permutation Map
            new int[] {}, //CornerOrientationToIndex
            new int[] {}, //CornerOrientationFromIndex
            new int[] { 2, 3, 1, 0, 4, 5, 6, 7, 8, 9, 10, 11 }, //FullEdgePermutation map
            new int[] {0, 1, 2, 3}, //EdgeOrientationToIndex
            new int[] {2, 3, 1, 0} //EdgeOrientationFromIndex
        ) },
        { "U'", new MoveData( 
            new int[] {2, 0, 3, 1, 4, 5, 6, 7}, 
            new int[] {}, 
            new int[] {},
            new int[] { 3, 2, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11 }, 
            new int[] {0, 1, 2, 3}, 
            new int[] { 3, 2, 0, 1 } 
        ) },
        { "U2", new MoveData( 
            new int[] {3, 2, 1, 0, 4, 5, 6, 7}, 
            new int[] {},
            new int[] {},
            new int[] { 1, 0, 3, 2, 4, 5, 6, 7, 8, 9, 10, 11 },
            new int[] {0, 1, 2, 3},
            new int[] { 1, 0, 3, 2 }
        ) },
        { "L", new MoveData( 
            new int[] { 0, 5, 2, 1, 4, 7, 6, 3 },
            new int[] {1, 3, 5, 7},
            new int[] {3, 7, 1, 5},
            new int[] { 0, 11, 2, 3, 4, 9, 6, 7, 8, 1, 10, 5 }
        ) },
        { "L'", new MoveData(
            new int[] { 0, 3, 2, 7, 4, 1, 6, 5 },
            new int[] {1, 3, 5, 7},
            new int[] {5, 1, 7, 3},
            new int[] { 0, 9, 2, 3, 4, 11, 6, 7, 8, 5, 10, 1 }
        ) },
        { "L2", new MoveData(
            new int[] { 0, 7, 2, 5, 4, 3, 6, 1 },
            new int[] {1, 3, 5, 7},
            new int[] { 3, 7, 1, 5 },
            new int[] { 0, 5, 2, 3, 4, 1, 6, 7, 8, 11, 10, 9 }
        ) },
        { "R", new MoveData(
            new int[] { 2, 1, 6, 3, 0, 5, 4, 7 },
            new int[]{ 0, 2, 4, 6 },
            new int[]{ 4, 0, 6, 2 },
            new int[] { 8, 1, 2, 3, 10, 5, 6, 7, 4, 9, 0, 11 },
            new int[] {0, 4, 8, 10},
            new int[] {8, 10, 4, 0}
        ) },
        { "R'", new MoveData(
            new int[] { 4, 1, 0, 3, 6, 5, 2, 7 },
            new int[]{ 0, 2, 4, 6 },
            new int[]{ 2, 6, 0, 4 },
            new int[] { 10, 1, 2, 3, 8, 5, 6, 7, 0, 9, 4, 11 }
        ) },
        { "R2", new MoveData(
            new int[] { 6, 1, 4, 3, 2, 5, 0, 7 },
            new int[]{ 0, 2, 4, 6 },
            new int[]{ 4, 0, 6, 2 },
            new int[] { 4, 1, 2, 3, 0, 5, 6, 7, 10, 9, 8, 11 }
        ) },
        { "D", new MoveData(
            new int[] { 0, 1, 2, 3, 6, 4, 7, 5 },
            new int[] {},
            new int[] {},
            new int[] { 0, 1, 2, 3, 7, 6, 4, 5, 8, 9, 10, 11 },
            new int[] {4, 5, 6, 7},
            new int[] {7, 6, 4, 5}
        ) },
        { "D'", new MoveData(
            new int[] { 0, 1, 2, 3, 5, 7, 4, 6 },
            new int[] {},
            new int[] {},
            new int[] { 0, 1, 2, 3, 6, 7, 5, 4, 8, 9, 10, 11 },
            new int[] {4, 5, 6, 7},
            new int[] {6, 7, 5, 4}
        ) },
        { "D2", new MoveData(
            new int[] { 0, 1, 2, 3, 7, 6, 5, 4 },
            new int[] {},
            new int[] {},
            new int[] { 0, 1, 2, 3, 5, 4, 7, 6, 8, 9, 10, 11 },
            new int[] {4, 5, 6, 7},
            new int[] {5, 4, 7, 6}
        ) },
        { "F", new MoveData(
            new int[] { 4, 0, 2, 3, 5, 1, 6, 7 },
            new int[] {0, 1, 4, 5},
            new int[] {1, 5, 0, 4},
            new int[] { 0, 1, 2, 9, 4, 5, 6, 8, 3, 7, 10, 11 }
        ) },
        { "F'", new MoveData(
            new int[] { 1, 5, 2, 3, 0, 4, 6, 7 },
            new int[] {0, 1, 4, 5},
            new int[] {4, 0, 5, 1},
            new int[] { 0, 1, 2, 8, 4, 5, 6, 9, 7, 3, 10, 11 }
        ) },
        { "F2", new MoveData(
            new int[] { 5, 4, 2, 3, 1, 0, 6, 7 },
            new int[] {0, 1, 4, 5},
            new int[] {1, 5, 0, 4},
            new int[] { 0, 1, 2, 7, 4, 5, 6, 3, 9, 8, 10, 11 }
        ) },
        { "B", new MoveData(
            new int[] { 0, 1, 3, 7, 4, 5, 2, 6 },
            new int[] {2, 3, 6, 7},
            new int[] {6, 2, 7, 3},
            new int[] { 0, 1, 10, 3, 4, 5, 11, 7, 8, 9, 6, 2 }
        ) },
        { "B'", new MoveData(
            new int[] { 0, 1, 3, 7, 4, 5, 2, 6 },
            new int[] {2, 3, 6, 7},
            new int[] {3, 7, 2, 6},
            new int[] { 0, 1, 11, 3, 4, 5, 10, 7, 8, 9, 2, 6 }
        ) },
        { "B2", new MoveData(
            new int[] { 0, 1, 7, 6, 4, 5, 3, 2 },
            new int[] {2, 3, 6, 7},
            new int[] { 6, 2, 7, 3 },
            new int[] { 0, 1, 6, 3, 4, 5, 2, 7, 8, 9, 11, 10 }
        ) }

    };


    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    
    public static void ApplyMove(CubeStateData state, string move)
    {
        //Updates the CORNER PERMUTATION indexing
        List<int> newCornerPerm = new List<int>(state.cornerPermutation);
        for (int i = 0; i < 8; i++)
        {
            int currentNumIndex = state.cornerPermutation.IndexOf(i);
            newCornerPerm[currentNumIndex] = MoveMaps[move].CornerPermutation[i];
        }

        //Updates the CORNER ORIENTATION
        List<int> newCornerOrient = new List<int>(state.cornerOrientation);
        newCornerOrient = GetNewCornerOrientation(state.cornerOrientation, move, MoveMaps[move].CornerOrientationToIndex, MoveMaps[move].CornerOrientationFromIndex);

        //Updates the EDGE PERMUTATION
        List<int> newFullEdgePermutation = new List<int>(state.fullEdgePermutation);
        for (int i = 0; i < 12; i++)
        {
            newFullEdgePermutation[i] = state.fullEdgePermutation[MoveMaps[move].FullEdgePermutation[i]];
        }

        //Updates the EDGE ORIENTATION
        List<int> newFullEdgeOrientation = new List<int>(state.fullEdgeOrientation);
        newFullEdgeOrientation = GetNewEdgeOrientation(state.fullEdgePermutation, state.fullEdgeOrientation, state.lastFourEdgeAlingment, move, MoveMaps[move].EdgeOrientationToIndex, MoveMaps[move].EdgeOrientationFromIndex);




        //Debug.Log("Corner Permutation NEXT MOVE: [" + string.Join(", ", newCornerPerm) + "]\n--------------------------------------------------------------------------");
        //Debug.Log("Corner Orientation NEXT MOVE: [" + string.Join(", ", newCornerOrient) + "]\n--------------------------------------------------------------------------");
        //Debug.Log("Edge Permutation NEXT MOVE: [" + string.Join(", ", newFullEdgePermutation) + "]\n--------------------------------------------------------------------------");
        Debug.Log("Edge Orientation NEXT MOVE: [" + string.Join(", ", newFullEdgeOrientation) + "]\n--------------------------------------------------------------------------");

        state.cornerPermutation = newCornerPerm;
        state.cornerOrientation = newCornerOrient;
        state.fullEdgePermutation = newFullEdgePermutation;
    }

    private static List<int> GetNewEdgeOrientation(List<int> permutation, List<int> orientation, Dictionary<int, string> alignment, string move, int[] ToIndex, int[] FromIndex)
    {
        List<int> newOrientation = new List<int>(orientation);
        //Moves "U" and "D" do not affect edge orientation values
        //Moves "U" and "D" only swap the orientation values of the edges
        if (move[0] == 'U' || move[0] == 'D')
        {
            newOrientation = GetUorDEdgeOrientaionChange(permutation, orientation, alignment, move, ToIndex, FromIndex);
            return newOrientation;
        }
        //Moves "R", "L", "F", and "B" affect edge orientation values
        else if (move != null)
        {
            //new int[] { 0, 4, 8, 10 }, To
            //new int[] { 8, 10, 4, 0 }  From
            for(int i = 0; i < 4; i++)
            {
                if (permutation[FromIndex[i]] < 4 && orientation[FromIndex[i]] == 0) //if the affected edge is one of the yellow edges
                {
                    newOrientation[ToIndex[i]] = 1; 
                }
            }
            return null;
        }
        else
        {
            Debug.LogError("Move is null or empty.");
            return null;
        }
            
    }

    private static List<int> GetUorDEdgeOrientaionChange(List<int> permutation, List<int> orientation, Dictionary<int, string> alignment, string move, int[] ToIndex, int[] FromIndex)
    {
        List<int> newOrientation = new List<int>(orientation);
        for (int i = 0; i < 4; i++)
        {
            if (permutation[FromIndex[i]] < 8) //if piece, that is coming to the new position, is one of the white or yellow edges
            {
                //set the orientation of the piece in the new position to the orientation of the piece in the old position
                newOrientation[ToIndex[i]] = orientation[FromIndex[i]];
                continue;
            }
            else
            {
                //if piece with no yellow/white sides is aligned with the other side in old position
                //then rotation 'U' or 'D' will break this aligning and the new orientation = flipped
                if (orientation[FromIndex[i]] == 0)
                {
                    newOrientation[ToIndex[i]] = 1;
                }

                else
                {
                    if (ToIndex[i] == 0 || ToIndex[i] == 4) //if affected edge is on the right side
                    {
                        int edgeIndex = permutation[FromIndex[i]];
                        if (edgeIndex == 8 || edgeIndex == 10)
                        {
                            if (alignment[edgeIndex] == "R")
                            {
                                newOrientation[ToIndex[i]] = 0;
                            }
                            else
                            {
                                newOrientation[ToIndex[i]] = 1;
                            }
                        }
                        else
                        {
                            newOrientation[ToIndex[i]] = 1;
                        }
                    }
                    else if (ToIndex[i] == 1 || ToIndex[i] == 5) //if affected edge is on the left side
                    {
                        int edgeIndex = permutation[FromIndex[i]];
                        if (edgeIndex == 9 || edgeIndex == 11)
                        {
                            if (alignment[edgeIndex] == "L")
                            {
                                newOrientation[ToIndex[i]] = 0;
                            }
                            else
                            {
                                newOrientation[ToIndex[i]] = 1;
                            }
                        }
                        else
                        {
                            newOrientation[ToIndex[i]] = 1;
                        }
                    }
                    else if (ToIndex[i] == 2 || ToIndex[i] == 6) //if affected edge is on the back side
                    {
                        int edgeIndex = permutation[FromIndex[i]];
                        if (edgeIndex == 10 || edgeIndex == 11)
                        {
                            if (alignment[edgeIndex] == "B")
                            {
                                newOrientation[ToIndex[i]] = 0;
                            }
                            else
                            {
                                newOrientation[ToIndex[i]] = 1;
                            }
                        }
                        else
                        {
                            newOrientation[ToIndex[i]] = 1;
                        }
                    }
                    else if (ToIndex[i] == 3 || ToIndex[i] == 7) //if affected edge is on the front side
                    {
                        int edgeIndex = permutation[FromIndex[i]];
                        if (edgeIndex == 8 || edgeIndex == 9)
                        {
                            if (alignment[edgeIndex] == "F")
                            {
                                newOrientation[ToIndex[i]] = 0;
                            }
                            else
                            {
                                newOrientation[ToIndex[i]] = 1;
                            }
                        }
                        else
                        {
                            newOrientation[ToIndex[i]] = 1;
                        }
                    }
                }
            }
        }
        return newOrientation;
    }

    private static List<int> GetNewCornerOrientation(List<int> orientation, string move, int[] ToIndex, int[] FromIndex)
    {
        //calls the method to return a corner orientation based on the move
        if (move[0] == 'R') 
        {
            if (move.Contains("2"))
            {
                var intermediateOrientation = GetRMoveOrientationChange(orientation, ToIndex, FromIndex);
                return GetRMoveOrientationChange(intermediateOrientation, ToIndex, FromIndex);
            }
            else
            {
                return GetRMoveOrientationChange(orientation, ToIndex, FromIndex);
            }
        }
        else if (move.StartsWith("L"))
        {
            if (move.Contains("2"))
            {
                var intermediateOrientation = GetLMoveOrientationChange(orientation, ToIndex, FromIndex);
                return GetLMoveOrientationChange(intermediateOrientation, ToIndex, FromIndex);
            }
            else
            {
                return GetLMoveOrientationChange(orientation, ToIndex, FromIndex);
            }
        }
        else if (move.StartsWith("F"))
        {
            if (move.Contains("2"))
            {
                var intermediateOrientation = GetFMoveOrientationChange(orientation, ToIndex, FromIndex);
                return GetFMoveOrientationChange(intermediateOrientation, ToIndex, FromIndex);
            }
            else
            {
                return GetFMoveOrientationChange(orientation, ToIndex, FromIndex);
            }
        }
        else if (move.StartsWith("B"))
        {
            if (move.Contains("2"))
            {
                var intermediateOrientation = GetBMoveOrientationChange(orientation, ToIndex, FromIndex);
                return GetBMoveOrientationChange(intermediateOrientation, ToIndex, FromIndex);
            }
            else
            {
                return GetBMoveOrientationChange(orientation, ToIndex, FromIndex);
            }
        }
        else if (move.StartsWith("U"))
        {
            return orientation;
        }
        else if (move.StartsWith("D"))
        {
            return orientation;
        }
        else
        {
            Debug.LogError("Move is null or empty.");
            return null;
        }
    }

    private static List<int> GetRMoveOrientationChange(List<int> currentOrientation, int[] ToIndex, int[] FromIndex)
    {
        List<int> newCornerOrientation = new List<int>(currentOrientation);
        for (int i = 0; i < 8; i++)
        {   
            //if the element("i") is in the array of affected corners(ToIndex -> CornerOrientationToIndex)
            if (Array.Exists(ToIndex, element => element == i)) 
            {
                if(i == 0 || i == 6)
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 2;
                            break;
                        case 1:
                            newCornerOrientation[i] = 0;
                            break;
                        case 2:
                            newCornerOrientation[i] = 1;
                            break;
                        default:
                            return null;
                    }
                }
                else
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 1;
                            break;
                        case 1:
                            newCornerOrientation[i] = 2;
                            break;
                        case 2:
                            newCornerOrientation[i] = 0;
                            break;
                        default:
                            return null;
                    }
                }
            }
            else
            {
                newCornerOrientation[i] = currentOrientation[i];
            }
        }
        return newCornerOrientation;
    }

    private static List<int> GetLMoveOrientationChange(List<int> currentOrientation, int[] ToIndex, int[] FromIndex)
    {
        List<int> newCornerOrientation = new List<int>(currentOrientation);
        for (int i = 0; i < 8; i++)
        {
            if (Array.Exists(ToIndex, element => element == i))
            {
                if (i == 3 || i == 5)
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 2;
                            break;
                        case 1:
                            newCornerOrientation[i] = 0;
                            break;
                        case 2:
                            newCornerOrientation[i] = 1;
                            break;
                        default:
                            return null;
                    }
                }
                else
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 1;
                            break;
                        case 1:
                            newCornerOrientation[i] = 2;
                            break;
                        case 2:
                            newCornerOrientation[i] = 0;
                            break;
                        default:
                            return null;
                    }
                }
            }
            else
            {
                newCornerOrientation[i] = currentOrientation[i];
            }
        }
        return newCornerOrientation;
    }
    private static List<int> GetFMoveOrientationChange(List<int> currentOrientation, int[] ToIndex, int[] FromIndex)
    {
        List<int> newCornerOrientation = new List<int>(currentOrientation);
        for (int i = 0; i < 8; i++)
        {
            if (Array.Exists(ToIndex, element => element == i))
            {
                if (i == 1 || i == 4)
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 2;
                            break;
                        case 1:
                            newCornerOrientation[i] = 0;
                            break;
                        case 2:
                            newCornerOrientation[i] = 1;
                            break;
                        default:
                            return null;
                    }
                }
                else
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 1;
                            break;
                        case 1:
                            newCornerOrientation[i] = 2;
                            break;
                        case 2:
                            newCornerOrientation[i] = 0;
                            break;
                        default:
                            return null;
                    }
                }
            }
            else
            {
                newCornerOrientation[i] = currentOrientation[i];
            }
        }
        return newCornerOrientation;
    }
    private static List<int> GetBMoveOrientationChange(List<int> currentOrientation, int[] ToIndex, int[] FromIndex)
    {
        List<int> newCornerOrientation = new List<int>(currentOrientation);
        for (int i = 0; i < 8; i++)
        {
            if (Array.Exists(ToIndex, element => element == i))
            {
                if (i == 2 || i == 7)
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 2;
                            break;
                        case 1:
                            newCornerOrientation[i] = 0;
                            break;
                        case 2:
                            newCornerOrientation[i] = 1;
                            break;
                        default:
                            return null;
                    }
                }
                else
                {
                    switch (currentOrientation[FromIndex[Array.IndexOf(ToIndex, i)]])
                    {
                        case 0:
                            newCornerOrientation[i] = 1;
                            break;
                        case 1:
                            newCornerOrientation[i] = 2;
                            break;
                        case 2:
                            newCornerOrientation[i] = 0;
                            break;
                        default:
                            return null;
                    }
                }
            }
            else
            {
                newCornerOrientation[i] = currentOrientation[i];
            }
        }
        return newCornerOrientation;
    }
}
