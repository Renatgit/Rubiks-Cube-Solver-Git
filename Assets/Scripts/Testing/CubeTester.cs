using Assets.Scripts.Core;
using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.Heuristics;
using Assets.Scripts.Solver.PatternDatabases;
using Assets.Scripts.Solver.Phases;
using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public class CubeTester : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RunAutomaticTests()
    {
        TestPhase1CandidateMoveTables();
        Phase1Heuristic.Prepare();
        Phase2Heuristic.Prepare();
        CornerPDBHeuristics.Prepare();

        TestShortestSolverScramble(
            "12-move scramble",
            "R", "U", "F'", "L2", "D", "B'", "R2", "U'", "F", "D2", "L", "B2");
        TestShortestSolverScramble(
            "13-move scramble",
            "R", "U", "F'", "L2", "D", "B'", "R2", "U'", "F", "D2", "L", "B2", "U");
        TestShortestSolverScramble(
            "14-move scramble",
            "R", "U", "F'", "L2", "D", "B'", "R2", "U'", "F", "D2", "L", "B2", "U", "R");
    }

    private static void TestPhase1CandidateMoveTables()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Phase1MoveTables.BuildIfNeeded();
        stopwatch.Stop();

        SolverStateData state = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        ApplyMoves(state, "R", "U", "F'", "L2", "D", "B'");
        int cornerPermutationIndex = Phase2Coordinate.GetCornerPermutationIndex(state);
        int sliceArrangementIndex = Phase1Coordinate.GetSliceArrangementIndex(state);
        bool cornerMovesMatch = true;
        bool sliceMovesMatch = true;

        foreach (int moveId in MoveGenerator.AllMoveIds)
        {
            SolverStateData child = state.Clone();
            MoveProcessor.ApplyMove(child, moveId);

            cornerMovesMatch = cornerMovesMatch
                && Phase1MoveTables.GetCornerPermutationAfterMovePrepared(
                    cornerPermutationIndex,
                    moveId) == Phase2Coordinate.GetCornerPermutationIndex(child);
            sliceMovesMatch = sliceMovesMatch
                && Phase1MoveTables.GetSliceArrangementAfterMovePrepared(
                    sliceArrangementIndex,
                    moveId) == Phase1Coordinate.GetSliceArrangementIndex(child);
        }

        SolverStateData phase1State = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        ApplyMoves(phase1State, "U", "R2", "D'", "F2");
        int phase1SliceArrangement = Phase1Coordinate.GetSliceArrangementIndex(phase1State);
        bool phase2SliceMatches =
            Phase1Coordinate.GetSlicePermutationIndexFromArrangement(phase1SliceArrangement)
            == Phase2Coordinate.GetSlicePermutationIndex(phase1State);
        bool passed = cornerMovesMatch && sliceMovesMatch && phase2SliceMatches;

        Debug.Log("CUBE TESTS - Phase 1 candidate prefilter tables"
            + " | corner moves: " + cornerMovesMatch
            + " | slice moves: " + sliceMovesMatch
            + " | phase2 slice: " + phase2SliceMatches
            + " | build time: " + stopwatch.ElapsedMilliseconds + "ms"
            + " | passed: " + passed);
    }

    private static void GenerateAndTestFullPhase1SymmetryPDB()
    {
        bool alreadyExisted = Phase1SymmetryPDB.HasValidSavedFile();
        Debug.Log("CUBE TESTS - Full exact Phase 1 PDB generation started"
            + " | entries: " + Phase1SymmetryCoordinate.ExactPhase1EntryCount
            + " | existing valid file: " + alreadyExisted
            + " | progress: " + Phase1SymmetryPDB.ProgressFilePath);

        Stopwatch stopwatch = Stopwatch.StartNew();
        byte[] database = Phase1SymmetryPDB.GenerateFullAndSave(
            message => Debug.Log("CUBE TESTS - " + message));
        stopwatch.Stop();

        SolverStateData solved = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        SolverStateData rState = solved.Clone();
        MoveProcessor.ApplyMove(rState, "R");
        SolverStateData uState = solved.Clone();
        MoveProcessor.ApplyMove(uState, "U");

        int solvedDepth = database[Phase1SymmetryCoordinate.GetExactIndex(solved)];
        int rDepth = database[Phase1SymmetryCoordinate.GetExactIndex(rState)];
        int uDepth = database[Phase1SymmetryCoordinate.GetExactIndex(uState)];
        Phase1SymmetryPdbGenerationStats stats = Phase1SymmetryPDB.LastGenerationStats;

        bool fileIsValid = Phase1SymmetryPDB.HasValidSavedFile();
        bool valuesAreValid = solvedDepth == 0 && rDepth == 1 && uDepth == 0;
        bool generationIsComplete = alreadyExisted
            || (stats != null
                && stats.VisitedStates == Phase1SymmetryCoordinate.ExactPhase1EntryCount);
        bool passed = fileIsValid && valuesAreValid && generationIsComplete;

        Debug.Log("CUBE TESTS - Full exact Phase 1 PDB"
            + " | passed: " + passed
            + " | generated now: " + !alreadyExisted
            + " | states: " + (stats == null ? database.LongLength : stats.VisitedStates)
            + " | max depth: " + (stats == null ? -1 : stats.MaxDepth)
            + " | solved: " + solvedDepth
            + " | R: " + rDepth
            + " | U: " + uDepth
            + " | file valid: " + fileIsValid
            + " | time: " + stopwatch.ElapsedMilliseconds + "ms"
            + " | file: " + Phase1SymmetryPDB.FilePath);
    }

    private static void TestPhase1Symmetry()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        SolverStateData state = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        ApplyMoves(state, "R", "U", "F'", "L2", "D", "B'");

        bool inverseRoundTrips = true;
        bool moveConjugationWorks = true;
        bool phase1GoalIsPreserved = true;
        HashSet<SolverStateKey> transformedStates = new HashSet<SolverStateKey>();

        SolverStateData phase1State = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        ApplyMoves(phase1State, "U", "R2", "D'", "F2");

        for (int symmetryIndex = 0; symmetryIndex < Phase1Symmetry.Count; symmetryIndex++)
        {
            SolverStateData transformed = Phase1Symmetry.Transform(state, symmetryIndex);
            transformedStates.Add(SolverStateKey.FromState(transformed));

            SolverStateData restored = Phase1Symmetry.Transform(
                transformed,
                Phase1Symmetry.GetInverseIndex(symmetryIndex));
            inverseRoundTrips = inverseRoundTrips && StatesMatch(state, restored);

            SolverStateData transformedPhase1State =
                Phase1Symmetry.Transform(phase1State, symmetryIndex);
            phase1GoalIsPreserved = phase1GoalIsPreserved
                && Phase1Goal.IsReached(transformedPhase1State);

            for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
            {
                SolverStateData movedThenTransformed = state.Clone();
                MoveProcessor.ApplyMove(movedThenTransformed, moveId);
                movedThenTransformed = Phase1Symmetry.Transform(movedThenTransformed, symmetryIndex);

                SolverStateData transformedThenMoved = transformed.Clone();
                int conjugatedMoveId = Phase1Symmetry.GetConjugatedMoveId(symmetryIndex, moveId);
                MoveProcessor.ApplyMove(transformedThenMoved, conjugatedMoveId);

                moveConjugationWorks = moveConjugationWorks
                    && StatesMatch(movedThenTransformed, transformedThenMoved);
            }
        }

        Phase1SymmetryCoordinate.BuildIfNeeded();
        bool classCountIsCorrect = Phase1SymmetryCoordinate.LastBuildStats.ClassCount
            == Phase1SymmetryCoordinate.FlipSliceClassCount;
        bool representativesAreCorrect = TestSymmetryRepresentatives();

        Dictionary<int, byte> tinyDatabase = Phase1SymmetryPDB.GenerateTiny(3);
        SolverStateData solved = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        SolverStateData rState = solved.Clone();
        MoveProcessor.ApplyMove(rState, "R");
        SolverStateData rufState = solved.Clone();
        ApplyMoves(rufState, "R", "U", "F");

        int solvedIndex = Phase1SymmetryCoordinate.GetExactIndex(solved);
        int rIndex = Phase1SymmetryCoordinate.GetExactIndex(rState);
        int rufIndex = Phase1SymmetryCoordinate.GetExactIndex(rufState);
        bool tinyDepthsAreCorrect = tinyDatabase.TryGetValue(solvedIndex, out byte solvedDepth)
            && solvedDepth == 0
            && tinyDatabase.TryGetValue(rIndex, out byte rDepth)
            && rDepth == 1
            && tinyDatabase.TryGetValue(rufIndex, out byte rufDepth)
            && rufDepth <= 3;

        bool passed = inverseRoundTrips
            && transformedStates.Count == Phase1Symmetry.Count
            && moveConjugationWorks
            && phase1GoalIsPreserved
            && classCountIsCorrect
            && representativesAreCorrect
            && tinyDepthsAreCorrect;

        stopwatch.Stop();
        Debug.Log("CUBE TESTS - Phase 1 symmetry"
            + " | 16 unique: " + (transformedStates.Count == Phase1Symmetry.Count)
            + " | inverse: " + inverseRoundTrips
            + " | moves: " + moveConjugationWorks
            + " | goal: " + phase1GoalIsPreserved
            + " | classes: " + Phase1SymmetryCoordinate.LastBuildStats.ClassCount
            + " | representatives: " + representativesAreCorrect
            + " | tiny PDB: " + tinyDepthsAreCorrect
            + " | support loaded: " + Phase1SymmetryCoordinate.LastBuildStats.LoadedFromFile
            + " | time: " + stopwatch.ElapsedMilliseconds + "ms"
            + " | passed: " + passed);
    }

    private static bool TestSymmetryRepresentatives()
    {
        int[] rawIndexes =
        {
            0,
            1,
            2047,
            2048,
            7919,
            104729,
            500000,
            Phase1SymmetryCoordinate.FlipSliceRawCount - 1
        };

        for (int i = 0; i < rawIndexes.Length; i++)
        {
            int rawIndex = rawIndexes[i];
            int classIndex = Phase1SymmetryCoordinate.GetClassIndex(rawIndex);
            int symmetryIndex = Phase1SymmetryCoordinate.GetSymmetryIndex(rawIndex);
            int representative = Phase1SymmetryCoordinate.GetRepresentativeRawIndex(classIndex);
            int transformed = Phase1SymmetryCoordinate.TransformRawFlipSliceIndex(
                rawIndex,
                symmetryIndex,
                false);

            if (transformed != representative)
            {
                return false;
            }
        }

        return true;
    }

    private static bool StatesMatch(SolverStateData first, SolverStateData second)
    {
        return ArraysMatch(first.CornerPermutation, second.CornerPermutation)
            && ArraysMatch(first.CornerOrientation, second.CornerOrientation)
            && ArraysMatch(first.FullEdgePermutation, second.FullEdgePermutation)
            && ArraysMatch(first.FullEdgeOrientation, second.FullEdgeOrientation);
    }

    private static bool ArraysMatch(int[] first, int[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        for (int i = 0; i < first.Length; i++)
        {
            if (first[i] != second[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void TestMoveIdsAndPhase1Tables()
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Phase1MoveTables.BuildIfNeeded();
        stopwatch.Stop();

        bool moveIdsMatchNames = true;
        for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
        {
            string move = MoveGenerator.GetMoveName(moveId);
            moveIdsMatchNames = moveIdsMatchNames && MoveGenerator.GetMoveId(move) == moveId;
        }

        bool validMoveIdsMatchNames = ValidMoveIdsMatchNames(null)
            && ValidMoveIdsMatchNames("R")
            && ValidMoveIdsMatchNames("D")
            && ValidMoveIdsMatchNames("B");

        SolverStateData state = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
        ApplyMoves(state, "R", "U", "F'", "L2");

        bool phase1TablesMatchFullMoves = true;
        int cornerOrientationIndex = Phase1Coordinate.GetCornerOrientationIndex(state);
        int edgeOrientationIndex = Phase1Coordinate.GetEdgeOrientationIndex(state);
        int slicePositionIndex = Phase1Coordinate.GetSlicePositionIndex(state);

        foreach (int moveId in MoveGenerator.AllMoveIds)
        {
            SolverStateData child = state.Clone();
            MoveProcessor.ApplyMove(child, moveId);

            bool cornerMatches = Phase1MoveTables.GetCornerOrientationAfterMove(cornerOrientationIndex, moveId)
                == Phase1Coordinate.GetCornerOrientationIndex(child);
            bool edgeMatches = Phase1MoveTables.GetEdgeOrientationAfterMove(edgeOrientationIndex, moveId)
                == Phase1Coordinate.GetEdgeOrientationIndex(child);
            bool sliceMatches = Phase1MoveTables.GetSlicePositionAfterMove(slicePositionIndex, moveId)
                == Phase1Coordinate.GetSlicePositionIndex(child);

            phase1TablesMatchFullMoves = phase1TablesMatchFullMoves
                && cornerMatches
                && edgeMatches
                && sliceMatches;
        }

        bool passed = moveIdsMatchNames && validMoveIdsMatchNames && phase1TablesMatchFullMoves;
        Debug.Log("CUBE TESTS - Move IDs + Phase1 tables"
            + " | move ids: " + moveIdsMatchNames
            + " | valid ids: " + validMoveIdsMatchNames
            + " | tables: " + phase1TablesMatchFullMoves
            + " | build time: " + stopwatch.ElapsedMilliseconds + "ms"
            + " | passed: " + passed);
    }

    private static void TestShortestSolverScramble(string testName, params string[] scramble)
    {
        CubeStateData state = CubeState.CreateSolvedState();
        ApplyMoves(state, scramble);
        int maxTotalDepth = scramble.Length;

        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        System.Collections.Generic.List<string> solution = TwoPhaseShortestSolver.Solve(
            state,
            maxTotalDepth);
        stopwatch.Stop();

        bool solved = false;
        if (solution != null)
        {
            ApplyMoves(state, solution.ToArray());
            solved = SolverStateUtility.IsSolved(SolverStateData.FromCubeStateData(state));
        }

        TwoPhaseShortestSolverStats stats = TwoPhaseShortestSolver.LastStats;
        string solutionText = solution == null ? "null" : string.Join(", ", solution);
        int solutionLength = solution == null ? -1 : solution.Count;
        long phase1NodesPerSecond = stats.TotalElapsedMilliseconds == 0
            ? stats.Phase1NodesVisited
            : stats.Phase1NodesVisited * 1000 / stats.TotalElapsedMilliseconds;

        Debug.Log("CUBE TESTS - TwoPhaseShortestSolver " + testName
            + " | solved: " + solved
            + " | exact PDB: " + Phase1Heuristic.IsUsingExactSymmetryDatabase
            + " | length: " + solutionLength
            + " | time: " + stopwatch.ElapsedMilliseconds + "ms"
            + " | internal time: " + stats.TotalElapsedMilliseconds + "ms"
            + " | max depth: " + maxTotalDepth
            + " | lower bound: " + stats.InitialLowerBound
            + " | final depth: " + stats.FinalDepth
            + " | depths tried: " + stats.TotalDepthsTried
            + " | candidates: " + stats.CandidatesFound
            + " | phase1 goals: " + stats.Phase1GoalsReached
            + " | prefiltered: " + stats.Phase1CandidatesPrefiltered
            + " | rebuilt: " + stats.Phase1CandidatesRebuilt
            + " | phase1 nodes: " + stats.Phase1NodesVisited
            + " | corner pruned: " + stats.Phase1PrunedByCornerLowerBound
            + " | phase1 nodes/s: " + phase1NodesPerSecond
            + " | phase2 attempts: " + stats.Phase2Attempts
            + " | skipped h: " + stats.SkippedByPhase2Heuristic
            + " | phase2 total: " + stats.TotalPhase2Milliseconds + "ms"
            + " | solution: " + solutionText);
    }

    private static void ApplyMoves(CubeStateData state, params string[] moves)
    {
        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(state, move, false);
        }
    }

    private static void ApplyMoves(SolverStateData state, params string[] moves)
    {
        foreach (string move in moves)
        {
            MoveProcessor.ApplyMove(state, move);
        }
    }

    private static bool ValidMoveIdsMatchNames(string previousMove)
    {
        System.Collections.Generic.List<string> validMoves = MoveGenerator.GetValidMoves(previousMove);
        int previousMoveId = previousMove == null ? MoveGenerator.NoMoveId : MoveGenerator.GetMoveId(previousMove);
        int[] validMoveIds = MoveGenerator.GetValidMoveIds(previousMoveId);

        if (validMoves.Count != validMoveIds.Length)
        {
            return false;
        }

        for (int i = 0; i < validMoves.Count; i++)
        {
            if (validMoves[i] != MoveGenerator.GetMoveName(validMoveIds[i]))
            {
                return false;
            }
        }

        return true;
    }
}
