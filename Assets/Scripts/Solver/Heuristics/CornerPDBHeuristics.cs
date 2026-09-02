using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Scripts.Solver.Heuristics
{
    public static class CornerPDBHeuristics
    {
        private static byte[] cornerPDB;

        public static int Estimate(CubeStateData state)
        {
            LoadIfNeeded();

            int cornerIndex = CornerCoordinate.GetIndex(
                state.cornerPermutation.ToArray(),
                state.cornerOrientation.ToArray());

            return cornerPDB[cornerIndex];
        }

        public static int Estimate(SolverStateData state)
        {
            LoadIfNeeded();

            return EstimatePrepared(
                CornerCoordinate.GetPermutationIndex(state.CornerPermutation),
                CornerCoordinate.GetOrientationIndex(state.CornerOrientation));
        }

        public static void Prepare()
        {
            LoadIfNeeded();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int EstimatePrepared(
            int cornerPermutationIndex,
            int cornerOrientationIndex)
        {
            return cornerPDB[
                cornerPermutationIndex * CornerCoordinate.CornerOrientationCount
                + cornerOrientationIndex];
        }

        private static void LoadIfNeeded()
        {
            if (cornerPDB != null)
            {
                return;
            }

            string filePath = Application.dataPath + "/PatternDatabase/corner.pdb";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Corner PDB file was not found", filePath);
            }

            cornerPDB = CornerPDB.Load(filePath);
        }
    }
}
