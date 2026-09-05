using Assets.Scripts.Solver.Coordinates;
using Assets.Scripts.Solver.PatternDatabases;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Assets.Scripts.Solver.Heuristics
{
    public static class FullCubeHeuristic
    {
        private static byte[] packedCornerPermutationEdgeOrientationPDB;

        public static int Estimate(SolverStateData state)
        {
            Prepare();
            int cornerEstimate = CornerPDBHeuristics.Estimate(state);
            int edgeEstimate = EdgeGroupPDBHeuristics.Estimate(state);
            int cornerEdgeEstimate = EstimateCornerPermutationEdgeOrientationPrepared(
                Phase2Coordinate.GetCornerPermutationIndex(state),
                Phase1Coordinate.GetEdgeOrientationIndex(state));

            return Math.Max(cornerEdgeEstimate, Math.Max(cornerEstimate, edgeEstimate));
        }

        public static void Prepare()
        {
            if (packedCornerPermutationEdgeOrientationPDB != null)
            {
                return;
            }

            if (!Phase1PDB.HasValidCornerPermutationEdgeOrientationSavedFile())
            {
                throw new FileNotFoundException(
                    "Corner-permutation/edge-orientation PDB file was not found or has an invalid size",
                    Phase1PDB.CornerPermutationEdgeOrientationFilePath);
            }

            packedCornerPermutationEdgeOrientationPDB =
                LoadPackedCornerPermutationEdgeOrientation();

            if (EstimateCornerPermutationEdgeOrientationPrepared(0, 0) != 0)
            {
                packedCornerPermutationEdgeOrientationPDB = null;
                throw new InvalidDataException(
                    "Corner-permutation/edge-orientation PDB has an invalid solved entry");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int EstimateCornerPermutationEdgeOrientationPrepared(
            int cornerPermutationIndex,
            int edgeOrientationIndex)
        {
            int index =
                cornerPermutationIndex * Phase1Coordinate.EdgeOrientationCount
                + edgeOrientationIndex;
            int shift = (index & 1) << 2;

            return (packedCornerPermutationEdgeOrientationPDB[index >> 1] >> shift) & 15;
        }

        private static byte[] LoadPackedCornerPermutationEdgeOrientation()
        {
            int entryCount = Phase1Coordinate.CornerPermutationEdgeOrientationCount;
            byte[] packedDatabase = new byte[(entryCount + 1) / 2];
            byte[] readBuffer = new byte[1024 * 1024];
            int entryIndex = 0;

            using (FileStream stream = new FileStream(
                Phase1PDB.CornerPermutationEdgeOrientationFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                readBuffer.Length,
                FileOptions.SequentialScan))
            {
                int bytesRead;
                while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        byte depth = readBuffer[i];
                        if (depth > 15)
                        {
                            throw new InvalidDataException(
                                "Corner-permutation/edge-orientation PDB contains a depth that cannot fit in four bits");
                        }

                        int packedIndex = entryIndex >> 1;
                        if ((entryIndex & 1) == 0)
                        {
                            packedDatabase[packedIndex] = depth;
                        }
                        else
                        {
                            packedDatabase[packedIndex] |= (byte)(depth << 4);
                        }

                        entryIndex++;
                    }
                }
            }

            if (entryIndex != entryCount)
            {
                throw new InvalidDataException(
                    "Corner-permutation/edge-orientation PDB contains an unexpected number of entries");
            }

            return packedDatabase;
        }
    }
}
