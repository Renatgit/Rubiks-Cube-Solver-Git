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
        public long CandidatesFound;
        public long Phase2Attempts;
        public long SkippedByPhase2Heuristic;
        public long SkippedByRemainingDepth;
        public long Phase1NodesVisited;
        public long Phase1PrunedByCornerLowerBound;
        public long Phase1EdgeGroupALookups;
        public long Phase1PrunedByEdgeGroupALowerBound;
        public long Phase1EdgeGroupBLookups;
        public long Phase1PrunedByEdgeGroupBLowerBound;
        public long Phase1GoalsReached;
        public long Phase1CandidatesPrefiltered;
        public long Phase1CandidatesRebuilt;
        public long TotalElapsedMilliseconds;
        public long TotalPhase2Milliseconds;
    }

    public static class TwoPhaseShortestSolver
    {
        private const int DefaultMaxPhase2Depth = 18;

        public static TwoPhaseShortestSolverStats LastStats { get; private set; }

        public static List<string> Solve(CubeStateData startState, int maxTotalDepth)
        {
            return Solve(startState, maxTotalDepth, maxTotalDepth, DefaultMaxPhase2Depth);
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

            Phase1Solver.SearchCoordinateCandidatesAtBound(
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
            LastStats.Phase1PrunedByCornerLowerBound +=
                Phase1Solver.LastCandidateSearchStats.PrunedByCornerLowerBound;
            LastStats.Phase1EdgeGroupALookups +=
                Phase1Solver.LastCandidateSearchStats.EdgeGroupALookups;
            LastStats.Phase1PrunedByEdgeGroupALowerBound +=
                Phase1Solver.LastCandidateSearchStats.PrunedByEdgeGroupALowerBound;
            LastStats.Phase1EdgeGroupBLookups +=
                Phase1Solver.LastCandidateSearchStats.EdgeGroupBLookups;
            LastStats.Phase1PrunedByEdgeGroupBLowerBound +=
                Phase1Solver.LastCandidateSearchStats.PrunedByEdgeGroupBLowerBound;
            LastStats.Phase1GoalsReached += Phase1Solver.LastCandidateSearchStats.GoalsReached;
            LastStats.Phase1CandidatesPrefiltered +=
                Phase1Solver.LastCandidateSearchStats.RejectedByPhase2CornerSlice;
            LastStats.Phase1CandidatesRebuilt +=
                Phase1Solver.LastCandidateSearchStats.CandidatesRebuilt;
            return foundSolution;
        }

        private static int GetInitialLowerBound(SolverStateData start)
        {
            int phase1LowerBound = Phase1Heuristic.Estimate(start);
            int cornerLowerBound = CornerPDBHeuristics.Estimate(start);
            int edgeLowerBound = EdgeGroupPDBHeuristics.Estimate(start);
            return System.Math.Max(
                phase1LowerBound,
                System.Math.Max(cornerLowerBound, edgeLowerBound));
        }
    }
}
