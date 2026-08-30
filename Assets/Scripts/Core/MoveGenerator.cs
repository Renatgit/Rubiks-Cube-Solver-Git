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

        public static readonly string[] Phase2Moves =
        {
            "U", "U'", "U2",
            "D", "D'", "D2",
            "R2", "L2", "F2", "B2"
        };

        public static List<CubeStateData> GenerateChildren(CubeStateData state, string previousMove = null, bool recordMoveHistory = true)
        {
            List<CubeStateData> children = new List<CubeStateData>();

            foreach (string move in GetValidMoves(previousMove))
            {
                CubeStateData child = CubeState.CloneState(state);
                MoveProcessor.ApplyMove(child, move, recordMoveHistory);
                children.Add(child);
            }

            return children;
        }

        public static List<string> GetValidMoves(string previousMove)
        {
            List<string> validMoves = new List<string>(AllMoves);

            if (string.IsNullOrEmpty(previousMove))
            {
                return validMoves;
            }

            string previousFace = previousMove[0].ToString();

            validMoves.RemoveAll(move => move.StartsWith(previousFace));

            if (previousFace == "D")
            {
                validMoves.RemoveAll(move => move.StartsWith("U"));
            }
            else if (previousFace == "L")
            {
                validMoves.RemoveAll(move => move.StartsWith("R"));
            }
            else if (previousFace == "B")
            {
                validMoves.RemoveAll(move => move.StartsWith("F"));
            }

            return validMoves;
        }

        public static List<string> GetValidPhase2Moves(string previousMove)
        {
            List<string> validMoves = new List<string>(Phase2Moves);

            if (string.IsNullOrEmpty(previousMove))
            {
                return validMoves;
            }

            string previousFace = previousMove[0].ToString();

            validMoves.RemoveAll(move => move.StartsWith(previousFace));

            if (previousFace == "D")
            {
                validMoves.RemoveAll(move => move.StartsWith("U"));
            }
            else if (previousFace == "L")
            {
                validMoves.RemoveAll(move => move.StartsWith("R"));
            }
            else if (previousFace == "B")
            {
                validMoves.RemoveAll(move => move.StartsWith("F"));
            }

            return validMoves;
        }
    }
}
