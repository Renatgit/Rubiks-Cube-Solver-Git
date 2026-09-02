using System.Collections.Generic;

namespace Assets.Scripts.Core
{
	public static class MoveGenerator 
	{
        public const int NoMoveId = -1;

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

        public static readonly int[] AllMoveIds = BuildMoveIds(AllMoves);
        public static readonly int[] Phase2MoveIds = BuildMoveIds(Phase2Moves);

        private static readonly Dictionary<string, int> MoveIdsByName = BuildMoveIdsByName();
        private static readonly int[][] ValidMoveIdsByPreviousMove = BuildValidMoveIdsByPreviousMove(AllMoveIds);
        private static readonly int[][] ValidPhase2MoveIdsByPreviousMove = BuildValidMoveIdsByPreviousMove(Phase2MoveIds);

        public static int GetMoveId(string move)
        {
            return MoveIdsByName[move];
        }

        public static string GetMoveName(int moveId)
        {
            return AllMoves[moveId];
        }

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
            int previousMoveId = string.IsNullOrEmpty(previousMove) ? NoMoveId : GetMoveId(previousMove);
            int[] validMoveIds = GetValidMoveIds(previousMoveId);
            List<string> validMoves = new List<string>(AllMoves);

            validMoves.Clear();
            for (int i = 0; i < validMoveIds.Length; i++)
            {
                validMoves.Add(GetMoveName(validMoveIds[i]));
            }

            return validMoves;
        }

        public static List<string> GetValidPhase2Moves(string previousMove)
        {
            int previousMoveId = string.IsNullOrEmpty(previousMove) ? NoMoveId : GetMoveId(previousMove);
            int[] validMoveIds = GetValidPhase2MoveIds(previousMoveId);
            List<string> validMoves = new List<string>(Phase2Moves);

            validMoves.Clear();
            for (int i = 0; i < validMoveIds.Length; i++)
            {
                validMoves.Add(GetMoveName(validMoveIds[i]));
            }

            return validMoves;
        }

        public static int[] GetValidMoveIds(int previousMoveId)
        {
            if (previousMoveId == NoMoveId)
            {
                return AllMoveIds;
            }

            return ValidMoveIdsByPreviousMove[previousMoveId];
        }

        public static int[] GetValidPhase2MoveIds(int previousMoveId)
        {
            if (previousMoveId == NoMoveId)
            {
                return Phase2MoveIds;
            }

            return ValidPhase2MoveIdsByPreviousMove[previousMoveId];
        }

        private static int[] GetValidMoveIdsFrom(int[] sourceMoveIds, int previousMoveId)
        {
            if (previousMoveId == NoMoveId)
            {
                return sourceMoveIds;
            }

            List<int> validMoveIds = new List<int>(sourceMoveIds.Length);
            char previousFace = AllMoves[previousMoveId][0];

            for (int i = 0; i < sourceMoveIds.Length; i++)
            {
                int moveId = sourceMoveIds[i];
                char face = AllMoves[moveId][0];

                if (face == previousFace)
                {
                    continue;
                }

                if ((previousFace == 'D' && face == 'U')
                    || (previousFace == 'L' && face == 'R')
                    || (previousFace == 'B' && face == 'F'))
                {
                    continue;
                }

                validMoveIds.Add(moveId);
            }

            return validMoveIds.ToArray();
        }

        private static int[] BuildMoveIds(string[] moves)
        {
            int[] moveIds = new int[moves.Length];

            for (int i = 0; i < moves.Length; i++)
            {
                for (int moveId = 0; moveId < AllMoves.Length; moveId++)
                {
                    if (moves[i] == AllMoves[moveId])
                    {
                        moveIds[i] = moveId;
                        break;
                    }
                }
            }

            return moveIds;
        }

        private static Dictionary<string, int> BuildMoveIdsByName()
        {
            Dictionary<string, int> moveIdsByName = new Dictionary<string, int>();

            for (int i = 0; i < AllMoves.Length; i++)
            {
                moveIdsByName[AllMoves[i]] = i;
            }

            return moveIdsByName;
        }

        private static int[][] BuildValidMoveIdsByPreviousMove(int[] sourceMoveIds)
        {
            int[][] validMoveIdsByPreviousMove = new int[AllMoves.Length][];

            for (int previousMoveId = 0; previousMoveId < AllMoves.Length; previousMoveId++)
            {
                validMoveIdsByPreviousMove[previousMoveId] = GetValidMoveIdsFrom(sourceMoveIds, previousMoveId);
            }

            return validMoveIdsByPreviousMove;
        }
    }
}
