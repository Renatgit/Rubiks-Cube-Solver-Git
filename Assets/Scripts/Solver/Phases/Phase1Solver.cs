using Assets.Scripts.Core;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.Search;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Assets.Scripts.Solver.Phases
{
    public enum Phase1AxisHeuristicMode
    {
        SingleAxis,
        TripleAxis,
        TripleAxisWithEqualEstimateBonus
    }

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
        public long TripleAxisLookups;
        public long PrunedByTripleAxisLowerBound;
        public long PrunedByCornerLowerBound;
        public long CornerEdgeLookups;
        public long PrunedByCornerEdgeLowerBound;
        public long EdgeGroupALookups;
        public long PrunedByEdgeGroupALowerBound;
        public long EdgeGroupBLookups;
        public long PrunedByEdgeGroupBLowerBound;
        public long GoalsReached;
        public long RejectedByPhase2CornerSlice;
        public long CandidatesRebuilt;
        public bool StoppedEarly;
        public bool Cancelled;
    }

    public static class Phase1Solver
    {
        private const int Infinity = int.MaxValue;
        private const int MaxCornerEdgeLookupRemainingDepth = 7;
        private const int MaxEdgeGroupALookupRemainingDepth = 8;
        private const int MaxEdgeGroupBLookupRemainingDepth = 7;
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

        private class FixedCandidateSearchContext
        {
            public SolverStateData StartState;
            public int Bound;
            public int CurrentBestLength;
            public int[] PathMoveIds;
            public Func<Phase1Candidate, bool> OnCandidateFound;
            public Phase1CandidateSearchStats Stats;
            public CancellationToken CancellationToken;
            public Phase1AxisHeuristicMode AxisHeuristicMode;
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

        public static bool SearchCoordinateCandidatesAtBound(
            CubeStateData startState,
            int bound,
            int currentBestLength,
            Func<Phase1Candidate, bool> onCandidateFound)
        {
            if (onCandidateFound == null)
            {
                throw new ArgumentNullException(nameof(onCandidateFound));
            }

            SolverStateData start = SolverStateData.FromCubeStateData(startState);
            PrepareFixedCoordinateSearch();

            int cornerOrientationIndex = Phase1Coordinate.GetCornerOrientationIndex(start);
            int edgeOrientationIndex = Phase1Coordinate.GetEdgeOrientationIndex(start);
            int slicePositionIndex = Phase1Coordinate.GetSlicePositionIndex(start);
            Phase1AxisCoordinateView firstRotatedView = Phase1AxisCoordinate.CreateView(
                start,
                Phase1AxisCoordinate.FirstRotatedAxisView);
            Phase1AxisCoordinateView secondRotatedView = Phase1AxisCoordinate.CreateView(
                start,
                Phase1AxisCoordinate.SecondRotatedAxisView);
            int cornerPermutationIndex = Phase2Coordinate.GetCornerPermutationIndex(start);
            int sliceArrangementIndex = Phase1Coordinate.GetSliceArrangementIndex(start);
            int edgeGroupAIndex = EdgeGroupPDBHeuristics.GetGroupAIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                edgeGroupAIndex,
                out int edgeGroupAPositionIndex,
                out int edgeGroupAPermutationIndex,
                out int edgeGroupAOrientationIndex);
            int edgeGroupBIndex = EdgeGroupPDBHeuristics.GetGroupBIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                edgeGroupBIndex,
                out _,
                out int edgeGroupBPermutationIndex,
                out _);

            Phase1CandidateSearchStats stats = new Phase1CandidateSearchStats();
            FixedCandidateSearchContext context = new FixedCandidateSearchContext
            {
                StartState = start,
                Bound = bound,
                CurrentBestLength = currentBestLength,
                PathMoveIds = new int[bound],
                OnCandidateFound = onCandidateFound,
                Stats = stats,
                CancellationToken = CancellationToken.None,
                AxisHeuristicMode =
                    Phase1AxisHeuristicMode.TripleAxisWithEqualEstimateBonus
            };

            bool stoppedEarly = SearchCoordinateCandidateAtFixedBound(
                context,
                cornerOrientationIndex,
                edgeOrientationIndex,
                slicePositionIndex,
                firstRotatedView,
                secondRotatedView,
                cornerPermutationIndex,
                sliceArrangementIndex,
                edgeGroupAPositionIndex,
                edgeGroupAPermutationIndex,
                edgeGroupAOrientationIndex,
                edgeGroupBPermutationIndex,
                0,
                MoveGenerator.NoMoveId);
            LastCandidateSearchStats = stats;
            return stoppedEarly;
        }

        internal static void PrepareFixedCoordinateSearch()
        {
            Phase1MoveTables.BuildIfNeeded();
            Phase1Heuristic.Prepare();
            Phase2Heuristic.Prepare();
            CornerPDBHeuristics.Prepare();
            EdgeGroupPDBHeuristics.Prepare();
            FullCubeHeuristic.Prepare();
        }

        internal static bool SearchCoordinateRootBranchAtBoundPrepared(
            SolverStateData start,
            int bound,
            int currentBestLength,
            Phase1AxisHeuristicMode axisHeuristicMode,
            int firstMoveId,
            CancellationToken cancellationToken,
            Func<Phase1Candidate, bool> onCandidateFound,
            out Phase1CandidateSearchStats stats)
        {
            if (bound < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bound));
            }

            if (firstMoveId < 0 || firstMoveId >= MoveGenerator.AllMoves.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(firstMoveId));
            }

            if (onCandidateFound == null)
            {
                throw new ArgumentNullException(nameof(onCandidateFound));
            }

            stats = new Phase1CandidateSearchStats();
            if (cancellationToken.IsCancellationRequested)
            {
                stats.Cancelled = true;
                return true;
            }

            FixedCandidateSearchContext context = new FixedCandidateSearchContext
            {
                StartState = start,
                Bound = bound,
                CurrentBestLength = currentBestLength,
                PathMoveIds = new int[bound],
                OnCandidateFound = onCandidateFound,
                Stats = stats,
                CancellationToken = cancellationToken,
                AxisHeuristicMode = axisHeuristicMode
            };

            int cornerOrientationIndex = Phase1Coordinate.GetCornerOrientationIndex(start);
            int edgeOrientationIndex = Phase1Coordinate.GetEdgeOrientationIndex(start);
            int slicePositionIndex = Phase1Coordinate.GetSlicePositionIndex(start);
            Phase1AxisCoordinateView firstRotatedView = Phase1AxisCoordinate.CreateView(
                start,
                Phase1AxisCoordinate.FirstRotatedAxisView);
            Phase1AxisCoordinateView secondRotatedView = Phase1AxisCoordinate.CreateView(
                start,
                Phase1AxisCoordinate.SecondRotatedAxisView);
            int cornerPermutationIndex = Phase2Coordinate.GetCornerPermutationIndex(start);
            int sliceArrangementIndex = Phase1Coordinate.GetSliceArrangementIndex(start);
            int edgeGroupAIndex = EdgeGroupPDBHeuristics.GetGroupAIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                edgeGroupAIndex,
                out int edgeGroupAPositionIndex,
                out int edgeGroupAPermutationIndex,
                out int edgeGroupAOrientationIndex);
            int edgeGroupBIndex = EdgeGroupPDBHeuristics.GetGroupBIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                edgeGroupBIndex,
                out _,
                out int edgeGroupBPermutationIndex,
                out _);

            int nextCornerOrientationIndex =
                Phase1MoveTables.GetCornerOrientationAfterMovePrepared(
                    cornerOrientationIndex,
                    firstMoveId);
            int nextEdgeOrientationIndex =
                Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(
                    edgeOrientationIndex,
                    firstMoveId);
            int nextSlicePositionIndex =
                Phase1MoveTables.GetSlicePositionAfterMovePrepared(
                    slicePositionIndex,
                    firstMoveId);
            context.PathMoveIds[0] = firstMoveId;

            return SearchCoordinateCandidateAtFixedBound(
                context,
                nextCornerOrientationIndex,
                nextEdgeOrientationIndex,
                nextSlicePositionIndex,
                firstRotatedView,
                secondRotatedView,
                cornerPermutationIndex,
                sliceArrangementIndex,
                edgeGroupAPositionIndex,
                edgeGroupAPermutationIndex,
                edgeGroupAOrientationIndex,
                edgeGroupBPermutationIndex,
                1,
                firstMoveId);
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
            EdgeGroupPDBHeuristics.Prepare();
            FullCubeHeuristic.Prepare();

            int startCornerOrientationIndex = Phase1Coordinate.GetCornerOrientationIndex(start);
            int startEdgeOrientationIndex = Phase1Coordinate.GetEdgeOrientationIndex(start);
            int startSlicePositionIndex = Phase1Coordinate.GetSlicePositionIndex(start);
            int startCornerPermutationIndex = Phase2Coordinate.GetCornerPermutationIndex(start);
            int startSliceArrangementIndex = Phase1Coordinate.GetSliceArrangementIndex(start);
            int startEdgeGroupAIndex = EdgeGroupPDBHeuristics.GetGroupAIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                startEdgeGroupAIndex,
                out int startEdgeGroupAPositionIndex,
                out int startEdgeGroupAPermutationIndex,
                out int startEdgeGroupAOrientationIndex);
            int startEdgeGroupBIndex = EdgeGroupPDBHeuristics.GetGroupBIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                startEdgeGroupBIndex,
                out _,
                out int startEdgeGroupBPermutationIndex,
                out _);
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
                    startEdgeGroupAPositionIndex,
                    startEdgeGroupAPermutationIndex,
                    startEdgeGroupAOrientationIndex,
                    startEdgeGroupBPermutationIndex,
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
            int baseCornerPermutationIndex,
            int baseSliceArrangementIndex,
            int baseEdgeGroupAPositionIndex,
            int baseEdgeGroupAPermutationIndex,
            int baseEdgeGroupAOrientationIndex,
            int baseEdgeGroupBPermutationIndex,
            int depth,
            int bound,
            int incomingMoveId,
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

            // Advance secondary coordinates only after each cheaper lower bound passes
            int cornerPermutationIndex = baseCornerPermutationIndex;
            if (incomingMoveId != MoveGenerator.NoMoveId)
            {
                cornerPermutationIndex =
                    Phase1MoveTables.GetCornerPermutationAfterMovePrepared(
                        baseCornerPermutationIndex,
                        incomingMoveId);
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

                int remainingDepth = currentBestLength - 1 - depth;
                if (remainingDepth <= MaxCornerEdgeLookupRemainingDepth)
                {
                    int cornerEdgeLowerBound =
                        FullCubeHeuristic.EstimateCornerPermutationEdgeOrientationPrepared(
                            cornerPermutationIndex,
                            edgeOrientationIndex);
                    LastCandidateSearchStats.CornerEdgeLookups++;

                    if (depth + cornerEdgeLowerBound >= currentBestLength)
                    {
                        LastCandidateSearchStats.PrunedByCornerEdgeLowerBound++;
                        return Infinity;
                    }
                }

                int sourceEdgeGroupAPositionIndex = baseEdgeGroupAPositionIndex;
                int edgeGroupAPositionIndex = baseEdgeGroupAPositionIndex;
                int edgeGroupAPermutationIndex = baseEdgeGroupAPermutationIndex;
                int edgeGroupAOrientationIndex = baseEdgeGroupAOrientationIndex;

                if (incomingMoveId != MoveGenerator.NoMoveId)
                {
                    edgeGroupAPermutationIndex =
                        Phase1MoveTables.GetEdgeGroupPermutationAfterMovePrepared(
                            baseEdgeGroupAPositionIndex,
                            baseEdgeGroupAPermutationIndex,
                            incomingMoveId);
                    edgeGroupAOrientationIndex =
                        Phase1MoveTables.GetEdgeGroupOrientationAfterMovePrepared(
                            baseEdgeGroupAPositionIndex,
                            baseEdgeGroupAOrientationIndex,
                            incomingMoveId);
                    edgeGroupAPositionIndex =
                        Phase1MoveTables.GetEdgeGroupPositionAfterMovePrepared(
                            baseEdgeGroupAPositionIndex,
                            incomingMoveId);
                }

                if (remainingDepth <= MaxEdgeGroupALookupRemainingDepth)
                {
                    int edgeGroupALowerBound = EdgeGroupPDBHeuristics.EstimateGroupAPrepared(
                        edgeGroupAPositionIndex,
                        edgeGroupAPermutationIndex,
                        edgeGroupAOrientationIndex);
                    LastCandidateSearchStats.EdgeGroupALookups++;

                    if (depth + edgeGroupALowerBound >= currentBestLength)
                    {
                        LastCandidateSearchStats.PrunedByEdgeGroupALowerBound++;
                        return Infinity;
                    }
                }

                baseEdgeGroupAPositionIndex = edgeGroupAPositionIndex;
                baseEdgeGroupAPermutationIndex = edgeGroupAPermutationIndex;
                baseEdgeGroupAOrientationIndex = edgeGroupAOrientationIndex;

                int edgeGroupBPermutationIndex = baseEdgeGroupBPermutationIndex;

                if (incomingMoveId != MoveGenerator.NoMoveId)
                {
                    int sourceEdgeGroupBPositionIndex =
                        Phase1MoveTables.GetComplementaryEdgeGroupPositionPrepared(
                            sourceEdgeGroupAPositionIndex);
                    edgeGroupBPermutationIndex =
                        Phase1MoveTables.GetEdgeGroupPermutationAfterMovePrepared(
                            sourceEdgeGroupBPositionIndex,
                            baseEdgeGroupBPermutationIndex,
                            incomingMoveId);
                }

                if (remainingDepth <= MaxEdgeGroupBLookupRemainingDepth)
                {
                    int edgeGroupBPositionIndex =
                        Phase1MoveTables.GetComplementaryEdgeGroupPositionPrepared(
                            edgeGroupAPositionIndex);
                    int edgeGroupBOrientationIndex =
                        Phase1MoveTables.GetEdgeGroupOrientationFromFullOrientationPrepared(
                            edgeOrientationIndex,
                            edgeGroupBPositionIndex);
                    int edgeGroupBLowerBound = EdgeGroupPDBHeuristics.EstimateGroupBPrepared(
                        edgeGroupBPositionIndex,
                        edgeGroupBPermutationIndex,
                        edgeGroupBOrientationIndex);
                    LastCandidateSearchStats.EdgeGroupBLookups++;

                    if (depth + edgeGroupBLowerBound >= currentBestLength)
                    {
                        LastCandidateSearchStats.PrunedByEdgeGroupBLowerBound++;
                        return Infinity;
                    }
                }

                baseEdgeGroupBPermutationIndex = edgeGroupBPermutationIndex;
            }

            if (currentBestLength == Infinity
                && incomingMoveId != MoveGenerator.NoMoveId)
            {
                int sourcePositionIndex = baseEdgeGroupAPositionIndex;
                int sourceEdgeGroupBPositionIndex =
                    Phase1MoveTables.GetComplementaryEdgeGroupPositionPrepared(
                        sourcePositionIndex);
                baseEdgeGroupAPermutationIndex =
                    Phase1MoveTables.GetEdgeGroupPermutationAfterMovePrepared(
                        sourcePositionIndex,
                        baseEdgeGroupAPermutationIndex,
                        incomingMoveId);
                baseEdgeGroupAOrientationIndex =
                    Phase1MoveTables.GetEdgeGroupOrientationAfterMovePrepared(
                        sourcePositionIndex,
                        baseEdgeGroupAOrientationIndex,
                        incomingMoveId);
                baseEdgeGroupAPositionIndex =
                    Phase1MoveTables.GetEdgeGroupPositionAfterMovePrepared(
                        sourcePositionIndex,
                        incomingMoveId);

                baseEdgeGroupBPermutationIndex =
                    Phase1MoveTables.GetEdgeGroupPermutationAfterMovePrepared(
                        sourceEdgeGroupBPositionIndex,
                        baseEdgeGroupBPermutationIndex,
                        incomingMoveId);
            }

            bool isPhase1Goal = IsPhase1CoordinateGoal(
                cornerOrientationIndex,
                edgeOrientationIndex,
                slicePositionIndex);
            int sliceArrangementIndex = baseSliceArrangementIndex;

            if (isPhase1Goal && incomingMoveId != MoveGenerator.NoMoveId)
            {
                sliceArrangementIndex =
                    Phase1MoveTables.GetSliceArrangementAfterMovePrepared(
                        baseSliceArrangementIndex,
                        incomingMoveId);
            }

            if (isPhase1Goal)
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

            if (!isPhase1Goal && incomingMoveId != MoveGenerator.NoMoveId)
            {
                sliceArrangementIndex =
                    Phase1MoveTables.GetSliceArrangementAfterMovePrepared(
                        baseSliceArrangementIndex,
                        incomingMoveId);
            }

            int minNextBound = Infinity;
            int[] validMoveIds = MoveGenerator.GetValidMoveIds(incomingMoveId);

            for (int i = 0; i < validMoveIds.Length; i++)
            {
                int moveId = validMoveIds[i];
                int nextCornerOrientationIndex =
                    Phase1MoveTables.GetCornerOrientationAfterMovePrepared(cornerOrientationIndex, moveId);
                int nextEdgeOrientationIndex =
                    Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(edgeOrientationIndex, moveId);
                int nextSlicePositionIndex =
                    Phase1MoveTables.GetSlicePositionAfterMovePrepared(slicePositionIndex, moveId);

                pathMoveIds[depth] = moveId;

                int result = SearchCoordinateCandidates(
                    startState,
                    nextCornerOrientationIndex,
                    nextEdgeOrientationIndex,
                    nextSlicePositionIndex,
                    cornerPermutationIndex,
                    sliceArrangementIndex,
                    baseEdgeGroupAPositionIndex,
                    baseEdgeGroupAPermutationIndex,
                    baseEdgeGroupAOrientationIndex,
                    baseEdgeGroupBPermutationIndex,
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

        private static bool SearchCoordinateCandidateAtFixedBound(
            FixedCandidateSearchContext context,
            int cornerOrientationIndex,
            int edgeOrientationIndex,
            int slicePositionIndex,
            Phase1AxisCoordinateView baseFirstRotatedView,
            Phase1AxisCoordinateView baseSecondRotatedView,
            int baseCornerPermutationIndex,
            int baseSliceArrangementIndex,
            int baseEdgeGroupAPositionIndex,
            int baseEdgeGroupAPermutationIndex,
            int baseEdgeGroupAOrientationIndex,
            int baseEdgeGroupBPermutationIndex,
            int depth,
            int incomingMoveId)
        {
            context.Stats.NodesVisited++;

            if ((context.Stats.NodesVisited & 1023) == 0
                && context.CancellationToken.IsCancellationRequested)
            {
                context.Stats.Cancelled = true;
                return true;
            }

            int heuristic = Phase1Heuristic.EstimatePrepared(
                cornerOrientationIndex,
                edgeOrientationIndex,
                slicePositionIndex);
            int estimatedTotal = depth + heuristic;

            if (estimatedTotal >= context.CurrentBestLength)
            {
                context.Stats.PrunedByCurrentBest++;
                return false;
            }

            if (estimatedTotal > context.Bound)
            {
                return false;
            }

            Phase1AxisCoordinateView firstRotatedView = baseFirstRotatedView;
            Phase1AxisCoordinateView secondRotatedView = baseSecondRotatedView;
            if (context.AxisHeuristicMode != Phase1AxisHeuristicMode.SingleAxis)
            {
                if (incomingMoveId != MoveGenerator.NoMoveId)
                {
                    firstRotatedView = Phase1AxisCoordinate.MovePrepared(
                        baseFirstRotatedView,
                        Phase1AxisCoordinate.FirstRotatedAxisView,
                        incomingMoveId);
                    secondRotatedView = Phase1AxisCoordinate.MovePrepared(
                        baseSecondRotatedView,
                        Phase1AxisCoordinate.SecondRotatedAxisView,
                        incomingMoveId);
                }

                context.Stats.TripleAxisLookups++;
                int tripleAxisLowerBound = Phase1Heuristic.EstimateAcrossAxesPrepared(
                    heuristic,
                    firstRotatedView,
                    secondRotatedView,
                    context.AxisHeuristicMode
                        == Phase1AxisHeuristicMode.TripleAxisWithEqualEstimateBonus);

                if (depth + tripleAxisLowerBound >= context.CurrentBestLength)
                {
                    context.Stats.PrunedByTripleAxisLowerBound++;
                    return false;
                }
            }

            int cornerPermutationIndex = baseCornerPermutationIndex;
            if (incomingMoveId != MoveGenerator.NoMoveId)
            {
                cornerPermutationIndex =
                    Phase1MoveTables.GetCornerPermutationAfterMovePrepared(
                        baseCornerPermutationIndex,
                        incomingMoveId);
            }

            int cornerLowerBound = CornerPDBHeuristics.EstimatePrepared(
                cornerPermutationIndex,
                cornerOrientationIndex);

            if (depth + cornerLowerBound >= context.CurrentBestLength)
            {
                context.Stats.PrunedByCornerLowerBound++;
                return false;
            }

            int remainingDepth = context.CurrentBestLength - 1 - depth;
            if (remainingDepth <= MaxCornerEdgeLookupRemainingDepth)
            {
                int cornerEdgeLowerBound =
                    FullCubeHeuristic.EstimateCornerPermutationEdgeOrientationPrepared(
                        cornerPermutationIndex,
                        edgeOrientationIndex);
                context.Stats.CornerEdgeLookups++;

                if (depth + cornerEdgeLowerBound >= context.CurrentBestLength)
                {
                    context.Stats.PrunedByCornerEdgeLowerBound++;
                    return false;
                }
            }

            int sourceEdgeGroupAPositionIndex = baseEdgeGroupAPositionIndex;
            int edgeGroupAPositionIndex = baseEdgeGroupAPositionIndex;
            int edgeGroupAPermutationIndex = baseEdgeGroupAPermutationIndex;
            int edgeGroupAOrientationIndex = baseEdgeGroupAOrientationIndex;

            if (incomingMoveId != MoveGenerator.NoMoveId)
            {
                edgeGroupAPermutationIndex =
                    Phase1MoveTables.GetEdgeGroupPermutationAfterMovePrepared(
                        baseEdgeGroupAPositionIndex,
                        baseEdgeGroupAPermutationIndex,
                        incomingMoveId);
                edgeGroupAOrientationIndex =
                    Phase1MoveTables.GetEdgeGroupOrientationAfterMovePrepared(
                        baseEdgeGroupAPositionIndex,
                        baseEdgeGroupAOrientationIndex,
                        incomingMoveId);
                edgeGroupAPositionIndex =
                    Phase1MoveTables.GetEdgeGroupPositionAfterMovePrepared(
                        baseEdgeGroupAPositionIndex,
                        incomingMoveId);
            }

            if (remainingDepth <= MaxEdgeGroupALookupRemainingDepth)
            {
                int edgeGroupALowerBound = EdgeGroupPDBHeuristics.EstimateGroupAPrepared(
                    edgeGroupAPositionIndex,
                    edgeGroupAPermutationIndex,
                    edgeGroupAOrientationIndex);
                context.Stats.EdgeGroupALookups++;

                if (depth + edgeGroupALowerBound >= context.CurrentBestLength)
                {
                    context.Stats.PrunedByEdgeGroupALowerBound++;
                    return false;
                }
            }

            int edgeGroupBPermutationIndex = baseEdgeGroupBPermutationIndex;
            if (incomingMoveId != MoveGenerator.NoMoveId)
            {
                int sourceEdgeGroupBPositionIndex =
                    Phase1MoveTables.GetComplementaryEdgeGroupPositionPrepared(
                        sourceEdgeGroupAPositionIndex);
                edgeGroupBPermutationIndex =
                    Phase1MoveTables.GetEdgeGroupPermutationAfterMovePrepared(
                        sourceEdgeGroupBPositionIndex,
                        baseEdgeGroupBPermutationIndex,
                        incomingMoveId);
            }

            if (remainingDepth <= MaxEdgeGroupBLookupRemainingDepth)
            {
                int edgeGroupBPositionIndex =
                    Phase1MoveTables.GetComplementaryEdgeGroupPositionPrepared(
                        edgeGroupAPositionIndex);
                int edgeGroupBOrientationIndex =
                    Phase1MoveTables.GetEdgeGroupOrientationFromFullOrientationPrepared(
                        edgeOrientationIndex,
                        edgeGroupBPositionIndex);
                int edgeGroupBLowerBound = EdgeGroupPDBHeuristics.EstimateGroupBPrepared(
                    edgeGroupBPositionIndex,
                    edgeGroupBPermutationIndex,
                    edgeGroupBOrientationIndex);
                context.Stats.EdgeGroupBLookups++;

                if (depth + edgeGroupBLowerBound >= context.CurrentBestLength)
                {
                    context.Stats.PrunedByEdgeGroupBLowerBound++;
                    return false;
                }
            }

            bool isPhase1Goal = IsPhase1CoordinateGoal(
                cornerOrientationIndex,
                edgeOrientationIndex,
                slicePositionIndex);
            int sliceArrangementIndex = baseSliceArrangementIndex;

            if (isPhase1Goal && incomingMoveId != MoveGenerator.NoMoveId)
            {
                sliceArrangementIndex =
                    Phase1MoveTables.GetSliceArrangementAfterMovePrepared(
                        baseSliceArrangementIndex,
                        incomingMoveId);
            }

            if (isPhase1Goal)
            {
                context.Stats.GoalsReached++;
                int slicePermutationIndex =
                    Phase1Coordinate.GetSlicePermutationIndexFromArrangement(sliceArrangementIndex);
                int phase2CornerSliceLowerBound =
                    Phase2Heuristic.EstimateCornerSlicePermutationPrepared(
                        cornerPermutationIndex,
                        slicePermutationIndex);

                if (phase2CornerSliceLowerBound > remainingDepth)
                {
                    context.Stats.RejectedByPhase2CornerSlice++;
                }
                else
                {
                    context.Stats.CandidatesRebuilt++;
                    Phase1Candidate candidate = CreateCandidateFromPath(
                        context.StartState,
                        context.PathMoveIds,
                        depth);

                    if (context.OnCandidateFound(candidate))
                    {
                        context.Stats.StoppedEarly = true;
                        return true;
                    }
                }
            }

            if (depth == context.Bound)
            {
                return false;
            }

            if (!isPhase1Goal && incomingMoveId != MoveGenerator.NoMoveId)
            {
                sliceArrangementIndex =
                    Phase1MoveTables.GetSliceArrangementAfterMovePrepared(
                        baseSliceArrangementIndex,
                        incomingMoveId);
            }

            int[] validMoveIds = MoveGenerator.GetValidMoveIds(incomingMoveId);

            for (int i = 0; i < validMoveIds.Length; i++)
            {
                int moveId = validMoveIds[i];
                int nextCornerOrientationIndex =
                    Phase1MoveTables.GetCornerOrientationAfterMovePrepared(
                        cornerOrientationIndex,
                        moveId);
                int nextEdgeOrientationIndex =
                    Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(
                        edgeOrientationIndex,
                        moveId);
                int nextSlicePositionIndex =
                    Phase1MoveTables.GetSlicePositionAfterMovePrepared(
                        slicePositionIndex,
                        moveId);
                context.PathMoveIds[depth] = moveId;

                if (SearchCoordinateCandidateAtFixedBound(
                    context,
                    nextCornerOrientationIndex,
                    nextEdgeOrientationIndex,
                    nextSlicePositionIndex,
                    firstRotatedView,
                    secondRotatedView,
                    cornerPermutationIndex,
                    sliceArrangementIndex,
                    edgeGroupAPositionIndex,
                    edgeGroupAPermutationIndex,
                    edgeGroupAOrientationIndex,
                    edgeGroupBPermutationIndex,
                    depth + 1,
                    moveId))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool SearchCoordinateRootOnlyAtBoundPrepared(
            SolverStateData start,
            int currentBestLength,
            Phase1AxisHeuristicMode axisHeuristicMode,
            Func<Phase1Candidate, bool> onCandidateFound,
            out Phase1CandidateSearchStats stats)
        {
            if (onCandidateFound == null)
            {
                throw new ArgumentNullException(nameof(onCandidateFound));
            }

            stats = new Phase1CandidateSearchStats();
            FixedCandidateSearchContext context = new FixedCandidateSearchContext
            {
                StartState = start,
                Bound = 0,
                CurrentBestLength = currentBestLength,
                PathMoveIds = new int[0],
                OnCandidateFound = onCandidateFound,
                Stats = stats,
                CancellationToken = CancellationToken.None,
                AxisHeuristicMode = axisHeuristicMode
            };

            int cornerOrientationIndex = Phase1Coordinate.GetCornerOrientationIndex(start);
            int edgeOrientationIndex = Phase1Coordinate.GetEdgeOrientationIndex(start);
            int slicePositionIndex = Phase1Coordinate.GetSlicePositionIndex(start);
            Phase1AxisCoordinateView firstRotatedView = Phase1AxisCoordinate.CreateView(
                start,
                Phase1AxisCoordinate.FirstRotatedAxisView);
            Phase1AxisCoordinateView secondRotatedView = Phase1AxisCoordinate.CreateView(
                start,
                Phase1AxisCoordinate.SecondRotatedAxisView);
            int cornerPermutationIndex = Phase2Coordinate.GetCornerPermutationIndex(start);
            int sliceArrangementIndex = Phase1Coordinate.GetSliceArrangementIndex(start);
            int edgeGroupAIndex = EdgeGroupPDBHeuristics.GetGroupAIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                edgeGroupAIndex,
                out int edgeGroupAPositionIndex,
                out int edgeGroupAPermutationIndex,
                out int edgeGroupAOrientationIndex);
            int edgeGroupBIndex = EdgeGroupPDBHeuristics.GetGroupBIndex(start);
            EdgeGroupCoordinate.SplitIndex(
                edgeGroupBIndex,
                out _,
                out int edgeGroupBPermutationIndex,
                out _);

            return SearchCoordinateCandidateAtFixedBound(
                context,
                cornerOrientationIndex,
                edgeOrientationIndex,
                slicePositionIndex,
                firstRotatedView,
                secondRotatedView,
                cornerPermutationIndex,
                sliceArrangementIndex,
                edgeGroupAPositionIndex,
                edgeGroupAPermutationIndex,
                edgeGroupAOrientationIndex,
                edgeGroupBPermutationIndex,
                0,
                MoveGenerator.NoMoveId);
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
