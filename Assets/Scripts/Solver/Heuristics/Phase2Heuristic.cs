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

            int cornerSliceEstimate = cornerSlicePermutationPDB[Phase2Coordinate.GetCornerSlicePermutationIndex(state)];
            int nonSliceEdgeEstimate = nonSliceEdgePermutationPDB[Phase2Coordinate.GetNonSliceEdgePermutationIndex(state)];

            if (cornerSliceEstimate == Phase2PDB.Unvisited || nonSliceEdgeEstimate == Phase2PDB.Unvisited)
            {
                return 0;
            }

            return Math.Max(cornerSliceEstimate, nonSliceEdgeEstimate);
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
