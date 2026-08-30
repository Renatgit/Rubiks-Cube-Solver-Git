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

    public static class Phase1Solver
    {
        private const int Infinity = int.MaxValue;

        public static IDAStarSearchStats LastSearchStats
        {
            get { return IDAStarSearch.LastSearchStats; }
        }

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
            SolverStateData start = SolverStateData.FromCubeStateData(startState);
            int bound = Phase1Heuristic.Estimate(start);
            HashSet<SolverStateKey> reportedCandidates = new HashSet<SolverStateKey>();

            while (bound <= maxDepth)
            {
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
            Action<Phase1Candidate> onCandidateFound)
        {
            int estimatedTotal = depth + Phase1Heuristic.Estimate(state);

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
                }
            }

            if (depth == bound)
            {
                return Infinity;
            }

            int minNextBound = Infinity;
            SolverStateKey stateKey = SolverStateKey.FromState(state);
            visitedOnPath.Add(stateKey);

            foreach (string move in MoveGenerator.GetValidMoves(previousMove))
            {
                SolverStateData child = state.Clone();
                MoveProcessor.ApplyMove(child, move);

                SolverStateKey childKey = SolverStateKey.FromState(child);
                if (visitedOnPath.Contains(childKey))
                {
                    continue;
                }

                path.Add(move);
                int result = SearchCandidates(
                    child,
                    depth + 1,
                    bound,
                    move,
                    path,
                    visitedOnPath,
                    reportedCandidates,
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
    }
}
