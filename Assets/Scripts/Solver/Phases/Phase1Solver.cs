using Assets.Scripts.Core;
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
        public int NodesVisited;
        public int PrunedByCurrentBest;
        public bool StoppedEarly;
    }

    public static class Phase1Solver
    {
        private const int Infinity = int.MaxValue;

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

            foreach (string move in MoveGenerator.GetValidMoves(previousMove))
            {
                SolverStateData child = state.Clone();
                MoveProcessor.ApplyMove(child, move);

                SolverStateKey childKey = SolverStateKey.FromState(child);
                if (visitedOnPath.Contains(childKey))
                {
                    continue;
                }

                orderedMoves.Add(new OrderedMove(
                    move,
                    child,
                    Phase1Heuristic.Estimate(child)));
            }

            orderedMoves.Sort((a, b) => a.Heuristic.CompareTo(b.Heuristic));
            return orderedMoves;
        }
    }
}
