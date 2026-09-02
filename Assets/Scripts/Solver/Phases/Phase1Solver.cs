using Assets.Scripts.Core;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.Search;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Solver.Phases
{
    public class Phase1Candidate
    {
        public SolverStateData State;
        public List<string> Moves;

        public Phase1Candidate(SolverStateData state, List<string> moves)
        {
            State = state;
            Moves = moves;
        }
    }

    public class Phase1CandidateSearchStats
    {
        public long NodesVisited;
        public long PrunedByCurrentBest;
        public long PrunedByCornerLowerBound;
        public long GoalsReached;
        public long RejectedByPhase2CornerSlice;
        public long CandidatesRebuilt;
        public bool StoppedEarly;
    }

    public static class Phase1Solver
    {
        private const int Infinity = int.MaxValue;
        private static readonly int SolvedSlicePositionIndex =
            Phase1Coordinate.GetSlicePositionIndexFromPositions(new int[] { 8, 9, 10, 11 });

        private class OrderedMove
        {
            public string Move;
            public SolverStateData State;
            public int Heuristic;

            public OrderedMove(string move, SolverStateData state, int heuristic)
            {
                Move = move;
                State = state;
                Heuristic = heuristic;
            }
        }

        public static IDAStarSearchStats LastSearchStats
        {
            get { return IDAStarSearch.LastSearchStats; }
        }

        public static Phase1CandidateSearchStats LastCandidateSearchStats { get; private set; }

        public static List<string> Solve(CubeStateData startState, int maxDepth)
        {
            SolverStateData start = SolverStateData.FromCubeStateData(startState);

            return IDAStarSearch.Solve(
                start,
                maxDepth,
                Phase1Goal.IsReached,
                Phase1Heuristic.Estimate,
                MoveGenerator.GetValidMoves);
        }

        public static void SearchCandidates(
            CubeStateData startState,
            int maxDepth,
            Action<Phase1Candidate> onCandidateFound)
        {
            SearchCandidates(startState, maxDepth, null, onCandidateFound);
        }

        public static void SearchCandidates(
            CubeStateData startState,
            int maxDepth,
            Func<int> getCurrentBestLength,
            Action<Phase1Candidate> onCandidateFound)
        {
            SearchCandidates(startState, maxDepth, getCurrentBestLength, null, onCandidateFound);
        }

        public static void SearchCandidates(
            CubeStateData startState,
            int maxDepth,
            Func<int> getCurrentBestLength,
            Func<bool> shouldStop,
            Action<Phase1Candidate> onCandidateFound)
        {
            SolverStateData start = SolverStateData.FromCubeStateData(startState);
            int bound = Phase1Heuristic.Estimate(start);
            HashSet<SolverStateKey> reportedCandidates = new HashSet<SolverStateKey>();
            LastCandidateSearchStats = new Phase1CandidateSearchStats();

            while (bound <= maxDepth)
            {
                if (shouldStop != null && shouldStop())
                {
                    LastCandidateSearchStats.StoppedEarly = true;
                    return;
                }

                List<string> path = new List<string>();
                HashSet<SolverStateKey> visitedOnPath = new HashSet<SolverStateKey>();

                int result = SearchCandidates(
                    start,
                    0,
                    bound,
                    null,
                    path,
                    visitedOnPath,
                    reportedCandidates,
                    getCurrentBestLength,
                    shouldStop,
                    onCandidateFound);

                if (result == Infinity)
                {
                    return;
                }

                bound = result;
            }
        }

        public static void SearchCoordinateCandidates(
            CubeStateData startState,
            int maxDepth,
            Func<int> getCurrentBestLength,
            Func<bool> shouldStop,
            Action<Phase1Candidate> onCandidateFound)
        {
            SearchCoordinateCandidatesCore(
                startState,
                maxDepth,
                true,
                getCurrentBestLength,
                shouldStop,
                onCandidateFound);
        }

        public static void SearchCoordinateCandidatesAtBound(
            CubeStateData startState,
            int bound,
            Func<int> getCurrentBestLength,
            Func<bool> shouldStop,
            Action<Phase1Candidate> onCandidateFound)
        {
            SearchCoordinateCandidatesCore(
                startState,
                bound,
                false,
                getCurrentBestLength,
                shouldStop,
                onCandidateFound);
        }

        private static void SearchCoordinateCandidatesCore(
            CubeStateData startState,
            int maxDepth,
            bool iterateBounds,
            Func<int> getCurrentBestLength,
            Func<bool> shouldStop,
            Action<Phase1Candidate> onCandidateFound)
        {
            SolverStateData start = SolverStateData.FromCubeStateData(startState);
            Phase1MoveTables.BuildIfNeeded();
            Phase1Heuristic.Prepare();
            Phase2Heuristic.Prepare();
            CornerPDBHeuristics.Prepare();

            int startCornerOrientationIndex = Phase1Coordinate.GetCornerOrientationIndex(start);
            int startEdgeOrientationIndex = Phase1Coordinate.GetEdgeOrientationIndex(start);
            int startSlicePositionIndex = Phase1Coordinate.GetSlicePositionIndex(start);
            int startCornerPermutationIndex = Phase2Coordinate.GetCornerPermutationIndex(start);
            int startSliceArrangementIndex = Phase1Coordinate.GetSliceArrangementIndex(start);
            int bound = iterateBounds
                ? Phase1Heuristic.EstimatePrepared(
                    startCornerOrientationIndex,
                    startEdgeOrientationIndex,
                    startSlicePositionIndex)
                : maxDepth;

            int[] pathMoveIds = new int[maxDepth];
            LastCandidateSearchStats = new Phase1CandidateSearchStats();

            while (bound <= maxDepth)
            {
                if (shouldStop != null && shouldStop())
                {
                    LastCandidateSearchStats.StoppedEarly = true;
                    return;
                }

                int result = SearchCoordinateCandidates(
                    start,
                    startCornerOrientationIndex,
                    startEdgeOrientationIndex,
                    startSlicePositionIndex,
                    startCornerPermutationIndex,
                    startSliceArrangementIndex,
                    0,
                    bound,
                    MoveGenerator.NoMoveId,
                    pathMoveIds,
                    getCurrentBestLength,
                    shouldStop,
                    onCandidateFound);

                if (result == Infinity)
                {
                    return;
                }

                if (!iterateBounds)
                {
                    return;
                }

                bound = result;
            }
        }

        private static int SearchCandidates(
            SolverStateData state,
            int depth,
            int bound,
            string previousMove,
            List<string> path,
            HashSet<SolverStateKey> visitedOnPath,
            HashSet<SolverStateKey> reportedCandidates,
            Func<int> getCurrentBestLength,
            Func<bool> shouldStop,
            Action<Phase1Candidate> onCandidateFound)
        {
            LastCandidateSearchStats.NodesVisited++;

            if (shouldStop != null && shouldStop())
            {
                LastCandidateSearchStats.StoppedEarly = true;
                return Infinity;
            }

            int currentBestLength = getCurrentBestLength == null ? Infinity : getCurrentBestLength();
            int estimatedTotal = depth + Phase1Heuristic.Estimate(state);

            if (estimatedTotal >= currentBestLength)
            {
                LastCandidateSearchStats.PrunedByCurrentBest++;
                return Infinity;
            }

            if (estimatedTotal > bound)
            {
                return estimatedTotal;
            }

            if (Phase1Goal.IsReached(state))
            {
                SolverStateKey candidateKey = SolverStateKey.FromState(state);

                if (!reportedCandidates.Contains(candidateKey))
                {
                    reportedCandidates.Add(candidateKey);
                    onCandidateFound(new Phase1Candidate(state.Clone(), new List<string>(path)));

                    if (shouldStop != null && shouldStop())
                    {
                        LastCandidateSearchStats.StoppedEarly = true;
                        return Infinity;
                    }
                }
            }

            if (depth == bound)
            {
                return Infinity;
            }

            int minNextBound = Infinity;
            SolverStateKey stateKey = SolverStateKey.FromState(state);
            visitedOnPath.Add(stateKey);

            List<OrderedMove> orderedMoves = GetOrderedMoves(state, previousMove, visitedOnPath);

            foreach (OrderedMove orderedMove in orderedMoves)
            {
                path.Add(orderedMove.Move);
                int result = SearchCandidates(
                    orderedMove.State,
                    depth + 1,
                    bound,
                    orderedMove.Move,
                    path,
                    visitedOnPath,
                    reportedCandidates,
                    getCurrentBestLength,
                    shouldStop,
                    onCandidateFound);

                if (result < minNextBound)
                {
                    minNextBound = result;
                }

                path.RemoveAt(path.Count - 1);
            }

            visitedOnPath.Remove(stateKey);

            return minNextBound;
        }

        private static List<OrderedMove> GetOrderedMoves(
            SolverStateData state,
            string previousMove,
            HashSet<SolverStateKey> visitedOnPath)
        {
            List<OrderedMove> orderedMoves = new List<OrderedMove>();
            int previousMoveId = previousMove == null ? MoveGenerator.NoMoveId : MoveGenerator.GetMoveId(previousMove);
            int cornerOrientationIndex = Phase1Coordinate.GetCornerOrientationIndex(state);
            int edgeOrientationIndex = Phase1Coordinate.GetEdgeOrientationIndex(state);
            int slicePositionIndex = Phase1Coordinate.GetSlicePositionIndex(state);

            foreach (int moveId in MoveGenerator.GetValidMoveIds(previousMoveId))
            {
                string move = MoveGenerator.GetMoveName(moveId);
                SolverStateData child = state.Clone();
                MoveProcessor.ApplyMove(child, moveId);

                SolverStateKey childKey = SolverStateKey.FromState(child);
                if (visitedOnPath.Contains(childKey))
                {
                    continue;
                }

                orderedMoves.Add(new OrderedMove(
                    move,
                    child,
                    Phase1Heuristic.Estimate(
                        Phase1MoveTables.GetCornerOrientationAfterMove(cornerOrientationIndex, moveId),
                        Phase1MoveTables.GetEdgeOrientationAfterMove(edgeOrientationIndex, moveId),
                        Phase1MoveTables.GetSlicePositionAfterMove(slicePositionIndex, moveId))));
            }

            orderedMoves.Sort((a, b) => a.Heuristic.CompareTo(b.Heuristic));
            return orderedMoves;
        }

        private static int SearchCoordinateCandidates(
            SolverStateData startState,
            int cornerOrientationIndex,
            int edgeOrientationIndex,
            int slicePositionIndex,
            int cornerPermutationIndex,
            int sliceArrangementIndex,
            int depth,
            int bound,
            int previousMoveId,
            int[] pathMoveIds,
            Func<int> getCurrentBestLength,
            Func<bool> shouldStop,
            Action<Phase1Candidate> onCandidateFound)
        {
            LastCandidateSearchStats.NodesVisited++;

            if (shouldStop != null && shouldStop())
            {
                LastCandidateSearchStats.StoppedEarly = true;
                return Infinity;
            }

            int currentBestLength = getCurrentBestLength == null ? Infinity : getCurrentBestLength();
            int heuristic = Phase1Heuristic.EstimatePrepared(
                cornerOrientationIndex,
                edgeOrientationIndex,
                slicePositionIndex);
            int estimatedTotal = depth + heuristic;

            if (estimatedTotal >= currentBestLength)
            {
                LastCandidateSearchStats.PrunedByCurrentBest++;
                return Infinity;
            }

            if (estimatedTotal > bound)
            {
                return estimatedTotal;
            }

            if (currentBestLength != Infinity)
            {
                int cornerLowerBound = CornerPDBHeuristics.EstimatePrepared(
                    cornerPermutationIndex,
                    cornerOrientationIndex);

                if (depth + cornerLowerBound >= currentBestLength)
                {
                    LastCandidateSearchStats.PrunedByCornerLowerBound++;
                    return Infinity;
                }
            }

            if (IsPhase1CoordinateGoal(cornerOrientationIndex, edgeOrientationIndex, slicePositionIndex))
            {
                LastCandidateSearchStats.GoalsReached++;
                int remainingDepth = currentBestLength == Infinity
                    ? Infinity
                    : currentBestLength - 1 - depth;
                int slicePermutationIndex =
                    Phase1Coordinate.GetSlicePermutationIndexFromArrangement(sliceArrangementIndex);
                int phase2CornerSliceLowerBound =
                    Phase2Heuristic.EstimateCornerSlicePermutationPrepared(
                        cornerPermutationIndex,
                        slicePermutationIndex);

                if (phase2CornerSliceLowerBound > remainingDepth)
                {
                    LastCandidateSearchStats.RejectedByPhase2CornerSlice++;
                }
                else
                {
                    LastCandidateSearchStats.CandidatesRebuilt++;
                    onCandidateFound(CreateCandidateFromPath(startState, pathMoveIds, depth));

                    if (shouldStop != null && shouldStop())
                    {
                        LastCandidateSearchStats.StoppedEarly = true;
                        return Infinity;
                    }
                }
            }

            if (depth == bound)
            {
                return Infinity;
            }

            int minNextBound = Infinity;
            int[] validMoveIds = MoveGenerator.GetValidMoveIds(previousMoveId);

            for (int i = 0; i < validMoveIds.Length; i++)
            {
                int moveId = validMoveIds[i];
                int nextCornerOrientationIndex =
                    Phase1MoveTables.GetCornerOrientationAfterMovePrepared(cornerOrientationIndex, moveId);
                int nextEdgeOrientationIndex =
                    Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(edgeOrientationIndex, moveId);
                int nextSlicePositionIndex =
                    Phase1MoveTables.GetSlicePositionAfterMovePrepared(slicePositionIndex, moveId);
                int nextCornerPermutationIndex =
                    Phase1MoveTables.GetCornerPermutationAfterMovePrepared(cornerPermutationIndex, moveId);
                int nextSliceArrangementIndex =
                    Phase1MoveTables.GetSliceArrangementAfterMovePrepared(sliceArrangementIndex, moveId);

                pathMoveIds[depth] = moveId;

                int result = SearchCoordinateCandidates(
                    startState,
                    nextCornerOrientationIndex,
                    nextEdgeOrientationIndex,
                    nextSlicePositionIndex,
                    nextCornerPermutationIndex,
                    nextSliceArrangementIndex,
                    depth + 1,
                    bound,
                    moveId,
                    pathMoveIds,
                    getCurrentBestLength,
                    shouldStop,
                    onCandidateFound);

                if (result < minNextBound)
                {
                    minNextBound = result;
                }
            }

            return minNextBound;
        }

        private static bool IsPhase1CoordinateGoal(
            int cornerOrientationIndex,
            int edgeOrientationIndex,
            int slicePositionIndex)
        {
            return cornerOrientationIndex == 0
                && edgeOrientationIndex == 0
                && slicePositionIndex == SolvedSlicePositionIndex;
        }

        private static Phase1Candidate CreateCandidateFromPath(
            SolverStateData startState,
            int[] pathMoveIds,
            int pathLength)
        {
            SolverStateData candidateState = startState.Clone();
            List<string> moves = new List<string>(pathLength);

            for (int i = 0; i < pathLength; i++)
            {
                int moveId = pathMoveIds[i];
                MoveProcessor.ApplyMove(candidateState, moveId);
                moves.Add(MoveGenerator.GetMoveName(moveId));
            }

            return new Phase1Candidate(candidateState, moves);
        }
    }
}
