using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;
using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Solver.Heuristics
{
    public static class Phase2Heuristic
    {
        private static byte[] cornerSlicePermutationPDB;
        private static byte[] nonSliceEdgePermutationPDB;

        public static int Estimate(SolverStateData state)
        {
            LoadIfNeeded();

            int cornerSliceEstimate = EstimateCornerSlicePermutation(
                Phase2Coordinate.GetCornerPermutationIndex(state),
                Phase2Coordinate.GetSlicePermutationIndex(state));
            int nonSliceEdgeEstimate = nonSliceEdgePermutationPDB[Phase2Coordinate.GetNonSliceEdgePermutationIndex(state)];

            if (cornerSliceEstimate == Phase2PDB.Unvisited || nonSliceEdgeEstimate == Phase2PDB.Unvisited)
            {
                return 0;
            }

            return Math.Max(cornerSliceEstimate, nonSliceEdgeEstimate);
        }

        public static int EstimateCornerSlicePermutation(int cornerPermutationIndex, int slicePermutationIndex)
        {
            LoadIfNeeded();
            return EstimateCornerSlicePermutationPrepared(cornerPermutationIndex, slicePermutationIndex);
        }

        public static void Prepare()
        {
            LoadIfNeeded();
        }

        internal static int EstimateCornerSlicePermutationPrepared(
            int cornerPermutationIndex,
            int slicePermutationIndex)
        {
            int estimate = cornerSlicePermutationPDB[
                cornerPermutationIndex * Phase2Coordinate.SlicePermutationCount + slicePermutationIndex];

            return estimate == Phase2PDB.Unvisited ? 0 : estimate;
        }

        public static void ClearDatabases()
        {
            cornerSlicePermutationPDB = null;
            nonSliceEdgePermutationPDB = null;
        }

        private static void LoadIfNeeded()
        {
            if (cornerSlicePermutationPDB != null && nonSliceEdgePermutationPDB != null)
            {
                return;
            }

            string cornerSlicePath = Application.dataPath + "/PatternDatabase/phase2_corner_slice_permutation.pdb";
            string nonSliceEdgePath = Application.dataPath + "/PatternDatabase/phase2_non_slice_edge_permutation.pdb";

            if (!File.Exists(cornerSlicePath))
            {
                throw new FileNotFoundException("Phase 2 corner-slice permutation PDB file was not found", cornerSlicePath);
            }

            if (!File.Exists(nonSliceEdgePath))
            {
                throw new FileNotFoundException("Phase 2 non-slice edge permutation PDB file was not found", nonSliceEdgePath);
            }

            cornerSlicePermutationPDB = Phase2PDB.LoadCornerSlicePermutation(cornerSlicePath);
            nonSliceEdgePermutationPDB = Phase2PDB.LoadNonSliceEdgePermutation(nonSliceEdgePath);
        }
    }
}
