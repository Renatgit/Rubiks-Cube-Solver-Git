using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;
using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Solver.Heuristics
{
    public static class Phase1Heuristic
    {
        private static byte[] cornerSlicePDB;
        private static byte[] edgeSlicePDB;

        public static int Estimate(SolverStateData state)
        {
            LoadIfNeeded();

            int cornerSliceEstimate = cornerSlicePDB[Phase1Coordinate.GetCornerSliceIndex(state)];
            int edgeSliceEstimate = edgeSlicePDB[Phase1Coordinate.GetEdgeSliceIndex(state)];

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
        }

        public static void ClearDatabases()
        {
            cornerSlicePDB = null;
            edgeSlicePDB = null;
        }

        private static void LoadIfNeeded()
        {
            if (cornerSlicePDB != null && edgeSlicePDB != null)
            {
                return;
            }

            string cornerSlicePath = Application.dataPath + "/PatternDatabase/phase1_corner_slice.pdb";
            string edgeSlicePath = Application.dataPath + "/PatternDatabase/phase1_edge_slice.pdb";

            if (!File.Exists(cornerSlicePath))
            {
                throw new FileNotFoundException("Phase 1 corner-slice PDB file was not found", cornerSlicePath);
            }

            if (!File.Exists(edgeSlicePath))
            {
                throw new FileNotFoundException("Phase 1 edge-slice PDB file was not found", edgeSlicePath);
            }

            cornerSlicePDB = Phase1PDB.LoadCornerSlice(cornerSlicePath);
            edgeSlicePDB = Phase1PDB.LoadEdgeSlice(edgeSlicePath);
        }
    }
}
