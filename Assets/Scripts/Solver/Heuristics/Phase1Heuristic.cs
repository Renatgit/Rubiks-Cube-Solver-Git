using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Scripts.Solver.Heuristics
{
    public static class Phase1Heuristic
    {
        private static byte[] cornerSlicePDB;
        private static byte[] edgeSlicePDB;
        private static byte[] exactSymmetryPDB;

        public static bool IsUsingExactSymmetryDatabase
        {
            get { return exactSymmetryPDB != null; }
        }

        public static int Estimate(SolverStateData state)
        {
            LoadIfNeeded();

            return EstimatePrepared(
                Phase1Coordinate.GetCornerOrientationIndex(state),
                Phase1Coordinate.GetEdgeOrientationIndex(state),
                Phase1Coordinate.GetSlicePositionIndex(state));
        }

        public static int Estimate(int cornerOrientationIndex, int edgeOrientationIndex, int slicePositionIndex)
        {
            LoadIfNeeded();

            return EstimatePrepared(cornerOrientationIndex, edgeOrientationIndex, slicePositionIndex);
        }

        public static void Prepare()
        {
            LoadIfNeeded();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int EstimatePrepared(
            int cornerOrientationIndex,
            int edgeOrientationIndex,
            int slicePositionIndex)
        {
            if (exactSymmetryPDB != null)
            {
                int exactIndex = Phase1SymmetryCoordinate.GetExactIndexPrepared(
                    cornerOrientationIndex,
                    edgeOrientationIndex,
                    slicePositionIndex);
                return exactSymmetryPDB[exactIndex];
            }

            int cornerSliceEstimate = cornerSlicePDB[
                cornerOrientationIndex * Phase1Coordinate.SlicePositionCount + slicePositionIndex];
            int edgeSliceEstimate = edgeSlicePDB[
                edgeOrientationIndex * Phase1Coordinate.SlicePositionCount + slicePositionIndex];

            if (cornerSliceEstimate == Phase1PDB.Unvisited || edgeSliceEstimate == Phase1PDB.Unvisited)
            {
                return 0;
            }

            return Math.Max(cornerSliceEstimate, edgeSliceEstimate);
        }

        public static void UseDatabases(byte[] cornerSliceDatabase, byte[] edgeSliceDatabase)
        {
            if (cornerSliceDatabase.Length != Phase1Coordinate.CornerSliceCount)
            {
                throw new Exception("Invalid corner-slice Phase 1 PDB size");
            }

            if (edgeSliceDatabase.Length != Phase1Coordinate.EdgeSliceCount)
            {
                throw new Exception("Invalid edge-slice Phase 1 PDB size");
            }

            cornerSlicePDB = cornerSliceDatabase;
            edgeSlicePDB = edgeSliceDatabase;
            exactSymmetryPDB = null;
        }

        public static void UseExactSymmetryDatabase(byte[] database)
        {
            if (database.Length != Phase1SymmetryCoordinate.ExactPhase1EntryCount)
            {
                throw new Exception("Invalid exact symmetry Phase 1 PDB size");
            }

            Phase1SymmetryCoordinate.BuildIfNeeded();
            exactSymmetryPDB = database;
            cornerSlicePDB = null;
            edgeSlicePDB = null;
        }

        public static void ClearDatabases()
        {
            cornerSlicePDB = null;
            edgeSlicePDB = null;
            exactSymmetryPDB = null;
        }

        private static void LoadIfNeeded()
        {
            if (exactSymmetryPDB != null
                || (cornerSlicePDB != null && edgeSlicePDB != null))
            {
                return;
            }

            string exactSymmetryPath = Phase1SymmetryPDB.FilePath;
            if (!File.Exists(exactSymmetryPath))
            {
                Debug.Log("Exact Phase 1 PDB is missing and will be generated once");
                exactSymmetryPDB = Phase1SymmetryPDB.GenerateFullAndSave(
                    message => Debug.Log(message));
                return;
            }

            Phase1SymmetryCoordinate.BuildIfNeeded();
            exactSymmetryPDB = Phase1SymmetryPDB.Load(exactSymmetryPath);
        }
    }
}
