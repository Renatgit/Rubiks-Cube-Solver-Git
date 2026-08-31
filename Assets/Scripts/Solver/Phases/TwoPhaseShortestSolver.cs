using Assets.Scripts.Solver.Heuristics;
using System.Collections.Generic;
using System.Diagnostics;

namespace Assets.Scripts.Solver.Phases
{
    public class TwoPhaseShortestSolverStats
    {
        public int InitialLowerBound;
        public int FinalDepth;
        public int TotalDepthsTried;
        public int CandidatesFound;
        public int Phase2Attempts;
        public int SkippedByPhase2Heuristic;
        public int SkippedByRemainingDepth;
        public int Phase1NodesVisited;
        public long TotalElapsedMilliseconds;
        public long TotalPhase2Milliseconds;
    }

    public static class TwoPhaseShortestSolver
    {
        private const int DefaultMaxPhase1Depth = 12;
        private const int DefaultMaxPhase2Depth = 18;

        public static TwoPhaseShortestSolverStats LastStats { get; private set; }

        public static List<string> Solve(CubeStateData startState, int maxTotalDepth)
        {
            return Solve(startState, maxTotalDepth, DefaultMaxPhase1Depth, DefaultMaxPhase2Depth);
        }

        public static List<string> Solve(CubeStateData startState, int maxTotalDepth, int maxPhase1Depth, int maxPhase2Depth)
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            SolverStateData start = SolverStateData.FromCubeStateData(startState);
            int lowerBound = GetInitialLowerBound(start);

            LastStats = new TwoPhaseShortestSolverStats
            {
                InitialLowerBound = lowerBound,
                FinalDepth = -1
            };

            for (int totalDepth = lowerBound; totalDepth <= maxTotalDepth; totalDepth++)
            {
                LastStats.TotalDepthsTried++;

                List<string> solution = TrySolveAtTotalDepth(
                    startState,
                    totalDepth,
                    maxPhase1Depth,
                    maxPhase2Depth);

                if (solution != null)
                {
                    totalStopwatch.Stop();
                    LastStats.FinalDepth = solution.Count;
                    LastStats.TotalElapsedMilliseconds = totalStopwatch.ElapsedMilliseconds;
                    return solution;
                }
            }

            totalStopwatch.Stop();
            LastStats.TotalElapsedMilliseconds = totalStopwatch.ElapsedMilliseconds;
            return null;
        }

        private static List<string> TrySolveAtTotalDepth(
            CubeStateData startState,
            int totalDepth,
            int maxPhase1Depth,
            int maxPhase2Depth)
        {
            List<string> foundSolution = null;
            int phase1DepthLimit = totalDepth < maxPhase1Depth ? totalDepth : maxPhase1Depth;

            Phase1Solver.SearchCandidates(
                startState,
                phase1DepthLimit,
                () => totalDepth + 1,
                () => foundSolution != null,
                candidate =>
                {
                    LastStats.CandidatesFound++;

                    int phase1Length = candidate.Moves.Count;
                    int remainingDepth = totalDepth - phase1Length;

                    if (remainingDepth < 0 || remainingDepth > maxPhase2Depth)
                    {
                        LastStats.SkippedByRemainingDepth++;
                        return;
                    }

                    int phase2LowerBound = Phase2Heuristic.Estimate(candidate.State);
                    if (phase2LowerBound > remainingDepth)
                    {
                        LastStats.SkippedByPhase2Heuristic++;
                        return;
                    }

                    LastStats.Phase2Attempts++;
                    string previousMove = phase1Length == 0 ? null : candidate.Moves[phase1Length - 1];

                    Stopwatch phase2Stopwatch = Stopwatch.StartNew();
                    List<string> phase2Solution = Phase2Solver.Solve(candidate.State, remainingDepth, previousMove);
                    phase2Stopwatch.Stop();
                    LastStats.TotalPhase2Milliseconds += phase2Stopwatch.ElapsedMilliseconds;

                    if (phase2Solution == null)
                    {
                        return;
                    }

                    foundSolution = new List<string>();
                    foundSolution.AddRange(candidate.Moves);
                    foundSolution.AddRange(phase2Solution);
                });

            LastStats.Phase1NodesVisited += Phase1Solver.LastCandidateSearchStats.NodesVisited;
            return foundSolution;
        }

        private static int GetInitialLowerBound(SolverStateData start)
        {
            return Phase1Heuristic.Estimate(start);
        }
    }
}
