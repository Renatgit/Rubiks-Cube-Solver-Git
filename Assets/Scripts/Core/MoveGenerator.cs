using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts.Core
{
	public static class MoveGenerator 
	{
        public static readonly string[] AllMoves =
        {
            "U", "U'", "U2",
            "D", "D'", "D2",
            "R", "R'", "R2",
            "L", "L'", "L2",
            "F", "F'", "F2",
            "B", "B'", "B2"
        };

        public static List<CubeStateData> GenerateChildren(CubeStateData state)
        {
            List<CubeStateData> children = new List<CubeStateData>();

            foreach (string move in AllMoves)
            {
                CubeStateData child = CubeState.CloneState(state);
                MoveProcessor.ApplyMove(child, move);
                children.Add(child);
            }

            return children;
        }
    }
}