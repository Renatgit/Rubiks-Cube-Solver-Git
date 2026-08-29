using Assets.Scripts.Solver;
using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;
using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Solver.Heuristics
{
    public static class EdgeGroupPDBHeuristics
    {
        private static readonly int[] GroupA = { 0, 1, 2, 3, 4, 5 };
        private static readonly int[] GroupB = { 6, 7, 8, 9, 10, 11 };

        private static byte[] edgeGroupAPDB;
        private static byte[] edgeGroupBPDB;

        public static int Estimate(CubeStateData state)
        {
            int groupAEstimate = EstimateGroupA(state);
            int groupBEstimate = EstimateGroupB(state);

            return Math.Max(groupAEstimate, groupBEstimate);
        }

        public static int Estimate(SolverStateData state)
        {
            int groupAEstimate = EstimateGroupA(state);
            int groupBEstimate = EstimateGroupB(state);

            return Math.Max(groupAEstimate, groupBEstimate);
        }

        public static int EstimateGroupA(CubeStateData state)
        {
            LoadGroupAIfNeeded();
            return EstimateFromDatabase(state, GroupA, edgeGroupAPDB);
        }

        public static int EstimateGroupB(CubeStateData state)
        {
            LoadGroupBIfNeeded();
            return EstimateFromDatabase(state, GroupB, edgeGroupBPDB);
        }

        public static int EstimateGroupA(SolverStateData state)
        {
            LoadGroupAIfNeeded();
            return EstimateFromDatabase(state, GroupA, edgeGroupAPDB);
        }

        public static int EstimateGroupB(SolverStateData state)
        {
            LoadGroupBIfNeeded();
            return EstimateFromDatabase(state, GroupB, edgeGroupBPDB);
        }

        private static int EstimateFromDatabase(CubeStateData state, int[] trackedEdges, byte[] database)
        {
            int edgeIndex = EdgeGroupCoordinate.GetIndex(
                state.fullEdgePermutation.ToArray(),
                state.fullEdgeOrientation.ToArray(),
                trackedEdges);

            return database[edgeIndex];
        }

        private static int EstimateFromDatabase(SolverStateData state, int[] trackedEdges, byte[] database)
        {
            int edgeIndex = EdgeGroupCoordinate.GetIndex(
                state.FullEdgePermutation,
                state.FullEdgeOrientation,
                trackedEdges);

            return database[edgeIndex];
        }

        private static void LoadGroupAIfNeeded()
        {
            if (edgeGroupAPDB != null)
            {
                return;
            }

            string filePath = Application.dataPath + "/PatternDatabase/edge_group_a.pdb";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Edge group A PDB file was not found", filePath);
            }

            edgeGroupAPDB = EdgeGroupPDB.Load(filePath);
        }

        private static void LoadGroupBIfNeeded()
        {
            if (edgeGroupBPDB != null)
            {
                return;
            }

            string filePath = Application.dataPath + "/PatternDatabase/edge_group_b.pdb";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Edge group B PDB file was not found", filePath);
            }

            edgeGroupBPDB = EdgeGroupPDB.Load(filePath);
        }
    }
}
