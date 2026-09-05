using Assets.Scripts.Core;
using Assets.Scripts.Solver.Heuristics;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts.Solver.Phases
{
    public class TwoPhaseShortestSolverStats
    {
        public Phase1AxisHeuristicMode AxisHeuristicMode;
        public int ParallelWorkers;
        public int InitialLowerBound;
        public int FinalDepth;
        public int TotalDepthsTried;
        public long CandidatesFound;
        public long Phase2Attempts;
        public long SkippedByPhase2Heuristic;
        public long SkippedByRemainingDepth;
        public long Phase1NodesVisited;
        public long Phase1TripleAxisLookups;
        public long Phase1PrunedByTripleAxisLowerBound;
        public long Phase1PrunedByCornerLowerBound;
        public long Phase1CornerEdgeLookups;
        public long Phase1PrunedByCornerEdgeLowerBound;
        public long Phase1EdgeGroupALookups;
        public long Phase1PrunedByEdgeGroupALowerBound;
        public long Phase1EdgeGroupBLookups;
        public long Phase1PrunedByEdgeGroupBLowerBound;
        public long Phase1GoalsReached;
        public long Phase1CandidatesPrefiltered;
        public long Phase1CandidatesRebuilt;
        public int CancelledBranches;
        public long TotalElapsedMilliseconds;
        public long TotalPhase2Milliseconds;
    }

    public static class TwoPhaseShortestSolver
    {
        private const int DefaultMaxPhase2Depth = 18;
        private static readonly object Phase2SearchLock = new object();

        private sealed class RootBranchResult
        {
            public List<string> Solution;
            public TwoPhaseShortestSolverStats Stats = new TwoPhaseShortestSolverStats();
        }

        public static TwoPhaseShortestSolverStats LastStats { get; private set; }

        public static List<string> Solve(CubeStateData startState, int maxTotalDepth)
        {
            return Solve(
                startState,
                maxTotalDepth,
                maxTotalDepth,
                DefaultMaxPhase2Depth,
                Phase1AxisHeuristicMode.TripleAxisWithEqualEstimateBonus);
        }

        public static List<string> Solve(
            CubeStateData startState,
            int maxTotalDepth,
            Phase1AxisHeuristicMode axisHeuristicMode)
        {
            return Solve(
                startState,
                maxTotalDepth,
                maxTotalDepth,
                DefaultMaxPhase2Depth,
                axisHeuristicMode);
        }

        public static List<string> Solve(CubeStateData startState, int maxTotalDepth, int maxPhase1Depth, int maxPhase2Depth)
        {
            return Solve(
                startState,
                maxTotalDepth,
                maxPhase1Depth,
                maxPhase2Depth,
                Phase1AxisHeuristicMode.TripleAxisWithEqualEstimateBonus);
        }

        public static List<string> Solve(
            CubeStateData startState,
            int maxTotalDepth,
            int maxPhase1Depth,
            int maxPhase2Depth,
            Phase1AxisHeuristicMode axisHeuristicMode)
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            SolverStateData start = SolverStateData.FromCubeStateData(startState);
            int lowerBound = GetInitialLowerBound(start, axisHeuristicMode);

            LastStats = new TwoPhaseShortestSolverStats
            {
                AxisHeuristicMode = axisHeuristicMode,
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
                    maxPhase2Depth,
                    axisHeuristicMode);

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
            int maxPhase2Depth,
            Phase1AxisHeuristicMode axisHeuristicMode)
        {
            int phase1DepthLimit = totalDepth < maxPhase1Depth ? totalDepth : maxPhase1Depth;
            SolverStateData start = SolverStateData.FromCubeStateData(startState);
            Phase1Solver.PrepareFixedCoordinateSearch();

            RootBranchResult rootResult = new RootBranchResult();
            System.Func<Phase1Candidate, bool> rootCallback = candidate =>
                TryCandidate(candidate, totalDepth, maxPhase2Depth, rootResult);

            bool stopped = Phase1Solver.SearchCoordinateRootOnlyAtBoundPrepared(
                start,
                totalDepth + 1,
                axisHeuristicMode,
                rootCallback,
                out Phase1CandidateSearchStats rootStats);
            AddPhase1Stats(rootResult.Stats, rootStats);
            AddBranchStats(LastStats, rootResult.Stats);

            if (stopped)
            {
                return rootResult.Solution;
            }

            if (phase1DepthLimit >= 1)
            {
                int[] rootMoveIds = MoveGenerator.AllMoveIds;

                int workerCount = Math.Min(
                    rootMoveIds.Length,
                    Math.Max(1, Environment.ProcessorCount -1));
                LastStats.ParallelWorkers = workerCount;     
                
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = workerCount
                };

                RootBranchResult[] branchResults = new RootBranchResult[rootMoveIds.Length];

                using (CancellationTokenSource cancellationSource =
                    new CancellationTokenSource())
                {
                    Parallel.For(
                        0,
                        rootMoveIds.Length,
                        options,
                        index =>
                        {
                            RootBranchResult branchResult = new RootBranchResult();

                            Func<Phase1Candidate, bool> branchCallback = candidate =>
                            {
                                bool found = TryCandidate(
                                    candidate,
                                    totalDepth,
                                    maxPhase2Depth,
                                    branchResult);

                                if (found)
                                {
                                    cancellationSource.Cancel();
                                }

                                return found;
                            };

                            Phase1Solver.SearchCoordinateRootBranchAtBoundPrepared(
                                start,
                                phase1DepthLimit,
                                totalDepth + 1,
                                axisHeuristicMode,
                                rootMoveIds[index],
                                cancellationSource.Token,
                                branchCallback,
                                out Phase1CandidateSearchStats phase1Stats);

                            AddPhase1Stats(branchResult.Stats, phase1Stats);
                            if (phase1Stats.Cancelled)
                            {
                                branchResult.Stats.CancelledBranches++;
                            }
                            branchResults[index] = branchResult;
                        });
                }
                List<string> foundSolution = null;

                for (int i = 0; i < branchResults.Length; i++)
                {
                    RootBranchResult branchResult = branchResults[i];

                    AddBranchStats(LastStats, branchResult.Stats);

                    if (foundSolution == null && branchResult.Solution != null)
                    {
                        foundSolution = branchResult.Solution;
                    }
                }

                return foundSolution;
            }
            return null;
        }

        private static bool TryCandidate(
            Phase1Candidate candidate,
            int totalDepth,
            int maxPhase2Depth,
            RootBranchResult result)
        {
            result.Stats.CandidatesFound++;

            int phase1Length = candidate.Moves.Count;
            int remainingDepth = totalDepth - phase1Length;

            if (remainingDepth < 0 || remainingDepth > maxPhase2Depth)
            {
                result.Stats.SkippedByRemainingDepth++;
                return false;
            }

            int phase2LowerBound = Phase2Heuristic.Estimate(candidate.State);
            if (phase2LowerBound > remainingDepth)
            {
                result.Stats.SkippedByPhase2Heuristic++;
                return false;
            }

            result.Stats.Phase2Attempts++;
            string previousMove = phase1Length == 0
                ? null
                : candidate.Moves[phase1Length - 1];
            List<string> phase2Solution;
            long phase2ElapsedMilliseconds;

            lock (Phase2SearchLock)
            {
                Stopwatch phase2Stopwatch = Stopwatch.StartNew();
                phase2Solution =
                    Phase2Solver.Solve(candidate.State, remainingDepth, previousMove);
                phase2Stopwatch.Stop();
                phase2ElapsedMilliseconds = phase2Stopwatch.ElapsedMilliseconds;
            }

            result.Stats.TotalPhase2Milliseconds += phase2ElapsedMilliseconds;

            if (phase2Solution == null)
            {
                return false;
            }

            result.Solution = new List<string>();
            result.Solution.AddRange(candidate.Moves);
            result.Solution.AddRange(phase2Solution);
            return true;
        }

        private static void AddPhase1Stats(
            TwoPhaseShortestSolverStats destination,
            Phase1CandidateSearchStats source)
        {
            destination.Phase1NodesVisited += source.NodesVisited;
            destination.Phase1TripleAxisLookups += source.TripleAxisLookups;
            destination.Phase1PrunedByTripleAxisLowerBound +=
                source.PrunedByTripleAxisLowerBound;
            destination.Phase1PrunedByCornerLowerBound += source.PrunedByCornerLowerBound;
            destination.Phase1CornerEdgeLookups += source.CornerEdgeLookups;
            destination.Phase1PrunedByCornerEdgeLowerBound +=
                source.PrunedByCornerEdgeLowerBound;
            destination.Phase1EdgeGroupALookups += source.EdgeGroupALookups;
            destination.Phase1PrunedByEdgeGroupALowerBound +=
                source.PrunedByEdgeGroupALowerBound;
            destination.Phase1EdgeGroupBLookups += source.EdgeGroupBLookups;
            destination.Phase1PrunedByEdgeGroupBLowerBound +=
                source.PrunedByEdgeGroupBLowerBound;
            destination.Phase1GoalsReached += source.GoalsReached;
            destination.Phase1CandidatesPrefiltered += source.RejectedByPhase2CornerSlice;
            destination.Phase1CandidatesRebuilt += source.CandidatesRebuilt;
        }

        private static void AddBranchStats(
            TwoPhaseShortestSolverStats destination,
            TwoPhaseShortestSolverStats source)
        {
            destination.CandidatesFound += source.CandidatesFound;
            destination.Phase2Attempts += source.Phase2Attempts;
            destination.SkippedByPhase2Heuristic += source.SkippedByPhase2Heuristic;
            destination.SkippedByRemainingDepth += source.SkippedByRemainingDepth;
            destination.Phase1NodesVisited += source.Phase1NodesVisited;
            destination.Phase1TripleAxisLookups += source.Phase1TripleAxisLookups;
            destination.Phase1PrunedByTripleAxisLowerBound +=
                source.Phase1PrunedByTripleAxisLowerBound;
            destination.Phase1PrunedByCornerLowerBound +=
                source.Phase1PrunedByCornerLowerBound;
            destination.Phase1CornerEdgeLookups += source.Phase1CornerEdgeLookups;
            destination.Phase1PrunedByCornerEdgeLowerBound +=
                source.Phase1PrunedByCornerEdgeLowerBound;
            destination.Phase1EdgeGroupALookups += source.Phase1EdgeGroupALookups;
            destination.Phase1PrunedByEdgeGroupALowerBound +=
                source.Phase1PrunedByEdgeGroupALowerBound;
            destination.Phase1EdgeGroupBLookups += source.Phase1EdgeGroupBLookups;
            destination.Phase1PrunedByEdgeGroupBLowerBound +=
                source.Phase1PrunedByEdgeGroupBLowerBound;
            destination.Phase1GoalsReached += source.Phase1GoalsReached;
            destination.Phase1CandidatesPrefiltered += source.Phase1CandidatesPrefiltered;
            destination.Phase1CandidatesRebuilt += source.Phase1CandidatesRebuilt;
            destination.CancelledBranches += source.CancelledBranches;
            destination.TotalPhase2Milliseconds += source.TotalPhase2Milliseconds;
        }

        private static int GetInitialLowerBound(
            SolverStateData start,
            Phase1AxisHeuristicMode axisHeuristicMode)
        {
            int phase1LowerBound = axisHeuristicMode == Phase1AxisHeuristicMode.SingleAxis
                ? Phase1Heuristic.Estimate(start)
                : Phase1Heuristic.EstimateAcrossAxes(
                    start,
                    axisHeuristicMode
                        == Phase1AxisHeuristicMode.TripleAxisWithEqualEstimateBonus);
            int fullCubeLowerBound = FullCubeHeuristic.Estimate(start);
            return System.Math.Max(phase1LowerBound, fullCubeLowerBound);
        }
    }
}
