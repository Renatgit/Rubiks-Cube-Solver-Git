using Assets.Scripts.Solver.Heuristics;
using System.Collections.Generic;
using System.Diagnostics;

namespace Assets.Scripts.Solver.Phases
{
    public class TwoPhaseSolverStats
    {
        public long CandidatesFound;
        public long Phase2Attempts;
        public long SkippedByPhase2Heuristic;
        public long SkippedByBestLength;
        public long BestUpdateCount;
        public int BestLength;
        public int AcceptableLength;
        public bool StoppedAfterAcceptableLength;
        public long Phase1NodesVisited;
        public long Phase1PrunedByCurrentBest;
        public long TotalElapsedMilliseconds;
        public long TotalPhase2Milliseconds;
        public long LongestPhase2Milliseconds;
    }

    public static class TwoPhaseSolver
    {
        public static TwoPhaseSolverStats LastStats { get; private set; }

        public static List<string> Solve(CubeStateData startState, int maxPhase1Depth, int maxPhase2Depth)
        {
            return Solve(startState, maxPhase1Depth, maxPhase2Depth, 20);
        }

        public static List<string> Solve(CubeStateData startState, int maxPhase1Depth, int maxPhase2Depth, int acceptableLength)
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            List<string> bestSolution = null;
            int bestLength = maxPhase1Depth + maxPhase2Depth + 1;

            LastStats = new TwoPhaseSolverStats
            {
                BestLength = -1,
                AcceptableLength = acceptableLength
            };

            Phase1Solver.SearchCandidates(
                startState,
                maxPhase1Depth,
                () => bestLength,
                () => bestSolution != null && bestLength <= acceptableLength,
                candidate =>
                {
                    LastStats.CandidatesFound++;

                    int phase1Length = candidate.Moves.Count;

                    if (phase1Length >= bestLength)
                    {
                        LastStats.SkippedByBestLength++;
                        return;
                    }

                    int phase2LowerBound = Phase2Heuristic.Estimate(candidate.State);
                    if (phase1Length + phase2LowerBound >= bestLength)
                    {
                        LastStats.SkippedByPhase2Heuristic++;
                        return;
                    }

                    int allowedPhase2Depth = bestLength - phase1Length - 1;
                    if (allowedPhase2Depth > maxPhase2Depth)
                    {
                        allowedPhase2Depth = maxPhase2Depth;
                    }

                    LastStats.Phase2Attempts++;
                    Stopwatch phase2Stopwatch = Stopwatch.StartNew();
                    List<string> phase2Solution = Phase2Solver.Solve(candidate.State, allowedPhase2Depth);
                    phase2Stopwatch.Stop();

                    LastStats.TotalPhase2Milliseconds += phase2Stopwatch.ElapsedMilliseconds;
                    if (phase2Stopwatch.ElapsedMilliseconds > LastStats.LongestPhase2Milliseconds)
                    {
                        LastStats.LongestPhase2Milliseconds = phase2Stopwatch.ElapsedMilliseconds;
                    }

                    if (phase2Solution == null)
                    {
                        return;
                    }

                    int totalLength = phase1Length + phase2Solution.Count;

                    if (totalLength < bestLength)
                    {
                        bestLength = totalLength;
                        LastStats.BestLength = bestLength;
                        LastStats.BestUpdateCount++;

                        bestSolution = new List<string>();
                        bestSolution.AddRange(candidate.Moves);
                        bestSolution.AddRange(phase2Solution);

                        if (bestLength <= acceptableLength)
                        {
                            LastStats.StoppedAfterAcceptableLength = true;
                        }
                    }
                });

            totalStopwatch.Stop();
            LastStats.TotalElapsedMilliseconds = totalStopwatch.ElapsedMilliseconds;
            LastStats.Phase1NodesVisited = Phase1Solver.LastCandidateSearchStats.NodesVisited;
            LastStats.Phase1PrunedByCurrentBest = Phase1Solver.LastCandidateSearchStats.PrunedByCurrentBest;

            return bestSolution;
        }
    }
}
