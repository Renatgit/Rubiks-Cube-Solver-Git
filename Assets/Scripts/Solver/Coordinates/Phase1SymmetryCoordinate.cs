using Assets.Scripts.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Scripts.Solver.Coordinates
{
    public class Phase1SymmetryBuildStats
    {
        public int ClassCount;
        public long ElapsedMilliseconds;
        public bool LoadedFromFile;
    }

    public static class Phase1SymmetryCoordinate
    {
        public const int FlipSliceRawCount =
            Phase1Coordinate.EdgeOrientationCount * Phase1Coordinate.SlicePositionCount;
        public const int FlipSliceClassCount = 64430;
        public const int ExactPhase1EntryCount =
            FlipSliceClassCount * Phase1Coordinate.CornerOrientationCount;

        private const ushort UnassignedClass = ushort.MaxValue;
        private const int FileMagic = 0x50315359;
        private const int FileVersion = 1;
        private const string FileName = "phase1_symmetry.dat";

        private static ushort[] flipSliceClassIndices;
        private static byte[] flipSliceSymmetries;
        private static int[] flipSliceRepresentatives;
        private static ushort[] cornerOrientationConjugates;
        private static ushort[] representativeStabilizers;
        private static ushort[] movedClassIndices;
        private static byte[] movedSymmetries;

        public static Phase1SymmetryBuildStats LastBuildStats { get; private set; }

        public static string FilePath
        {
            get { return Application.dataPath + "/PatternDatabase/" + FileName; }
        }

        public static void BuildIfNeeded()
        {
            if (flipSliceClassIndices != null)
            {
                return;
            }

            if (File.Exists(FilePath))
            {
                Load(FilePath);
                return;
            }

            Build();
            Save(FilePath);
        }

        public static int GetRawFlipSliceIndex(int edgeOrientationIndex, int slicePositionIndex)
        {
            return slicePositionIndex * Phase1Coordinate.EdgeOrientationCount + edgeOrientationIndex;
        }

        public static int GetClassIndex(int rawFlipSliceIndex)
        {
            BuildIfNeeded();
            return flipSliceClassIndices[rawFlipSliceIndex];
        }

        public static int GetSymmetryIndex(int rawFlipSliceIndex)
        {
            BuildIfNeeded();
            return flipSliceSymmetries[rawFlipSliceIndex];
        }

        public static int GetRepresentativeRawIndex(int classIndex)
        {
            BuildIfNeeded();
            return flipSliceRepresentatives[classIndex];
        }

        public static int GetExactIndex(
            int cornerOrientationIndex,
            int edgeOrientationIndex,
            int slicePositionIndex)
        {
            BuildIfNeeded();
            return GetExactIndexPrepared(
                cornerOrientationIndex,
                edgeOrientationIndex,
                slicePositionIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetExactIndexPrepared(
            int cornerOrientationIndex,
            int edgeOrientationIndex,
            int slicePositionIndex)
        {
            int rawFlipSliceIndex =
                slicePositionIndex * Phase1Coordinate.EdgeOrientationCount + edgeOrientationIndex;
            int classIndex = flipSliceClassIndices[rawFlipSliceIndex];
            int symmetryIndex = flipSliceSymmetries[rawFlipSliceIndex];
            int transformedCornerOrientation =
                cornerOrientationConjugates[cornerOrientationIndex * Phase1Symmetry.Count + symmetryIndex];

            return classIndex * Phase1Coordinate.CornerOrientationCount + transformedCornerOrientation;
        }

        public static int GetExactIndex(SolverStateData state)
        {
            return GetExactIndex(
                Phase1Coordinate.GetCornerOrientationIndex(state),
                Phase1Coordinate.GetEdgeOrientationIndex(state),
                Phase1Coordinate.GetSlicePositionIndex(state));
        }

        public static int TransformRawFlipSliceIndex(
            int rawFlipSliceIndex,
            int symmetryIndex,
            bool inverseFirst)
        {
            int edgeOrientationIndex = rawFlipSliceIndex % Phase1Coordinate.EdgeOrientationCount;
            int slicePositionIndex = rawFlipSliceIndex / Phase1Coordinate.EdgeOrientationCount;
            int[] edgePermutation = Phase1Coordinate.GetEdgePermutationFromSliceIndex(slicePositionIndex);
            int[] edgeOrientation = Phase1Coordinate.GetEdgeOrientationFromIndex(edgeOrientationIndex);
            int[] transformedPermutation = new int[12];
            int[] transformedOrientation = new int[12];

            if (inverseFirst)
            {
                Phase1Symmetry.TransformEdgesInverseFirst(
                    edgePermutation,
                    edgeOrientation,
                    symmetryIndex,
                    transformedPermutation,
                    transformedOrientation);
            }
            else
            {
                Phase1Symmetry.TransformEdges(
                    edgePermutation,
                    edgeOrientation,
                    symmetryIndex,
                    transformedPermutation,
                    transformedOrientation);
            }

            return GetRawFlipSliceIndex(
                Phase1Coordinate.GetEdgeOrientationIndex(transformedOrientation),
                Phase1Coordinate.GetSlicePositionIndex(transformedPermutation));
        }

        internal static int GetCornerOrientationConjugatePrepared(
            int cornerOrientationIndex,
            int symmetryIndex)
        {
            return cornerOrientationConjugates[
                cornerOrientationIndex * Phase1Symmetry.Count + symmetryIndex];
        }

        internal static int GetRepresentativeStabilizersPrepared(int classIndex)
        {
            return representativeStabilizers[classIndex];
        }

        internal static void PrepareMoveTables()
        {
            BuildIfNeeded();

            if (movedClassIndices != null)
            {
                return;
            }

            Phase1MoveTables.BuildIfNeeded();
            int moveCount = MoveGenerator.AllMoves.Length;
            movedClassIndices = new ushort[FlipSliceClassCount * moveCount];
            movedSymmetries = new byte[FlipSliceClassCount * moveCount];

            for (int classIndex = 0; classIndex < FlipSliceClassCount; classIndex++)
            {
                int representative = flipSliceRepresentatives[classIndex];
                int edgeOrientationIndex = representative % Phase1Coordinate.EdgeOrientationCount;
                int slicePositionIndex = representative / Phase1Coordinate.EdgeOrientationCount;

                for (int moveId = 0; moveId < moveCount; moveId++)
                {
                    int movedEdgeOrientation =
                        Phase1MoveTables.GetEdgeOrientationAfterMovePrepared(edgeOrientationIndex, moveId);
                    int movedSlicePosition =
                        Phase1MoveTables.GetSlicePositionAfterMovePrepared(slicePositionIndex, moveId);
                    int movedRawIndex = GetRawFlipSliceIndex(movedEdgeOrientation, movedSlicePosition);
                    int tableIndex = classIndex * moveCount + moveId;

                    movedClassIndices[tableIndex] = flipSliceClassIndices[movedRawIndex];
                    movedSymmetries[tableIndex] = flipSliceSymmetries[movedRawIndex];
                }
            }
        }

        internal static int GetMovedClassPrepared(int classIndex, int moveId)
        {
            return movedClassIndices[classIndex * MoveGenerator.AllMoves.Length + moveId];
        }

        internal static int GetMovedSymmetryPrepared(int classIndex, int moveId)
        {
            return movedSymmetries[classIndex * MoveGenerator.AllMoves.Length + moveId];
        }

        private static void Build()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            BuildCornerOrientationConjugates();
            BuildFlipSliceClasses();
            BuildRepresentativeStabilizers();
            stopwatch.Stop();

            LastBuildStats = new Phase1SymmetryBuildStats
            {
                ClassCount = flipSliceRepresentatives.Length,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                LoadedFromFile = false
            };
        }

        private static void BuildCornerOrientationConjugates()
        {
            cornerOrientationConjugates = new ushort[
                Phase1Coordinate.CornerOrientationCount * Phase1Symmetry.Count];

            for (int cornerOrientationIndex = 0;
                cornerOrientationIndex < Phase1Coordinate.CornerOrientationCount;
                cornerOrientationIndex++)
            {
                for (int symmetryIndex = 0; symmetryIndex < Phase1Symmetry.Count; symmetryIndex++)
                {
                    cornerOrientationConjugates[
                        cornerOrientationIndex * Phase1Symmetry.Count + symmetryIndex] =
                        (ushort)Phase1Symmetry.TransformCornerOrientationIndex(
                            cornerOrientationIndex,
                            symmetryIndex);
                }
            }
        }

        private static void BuildFlipSliceClasses()
        {
            flipSliceClassIndices = new ushort[FlipSliceRawCount];
            flipSliceSymmetries = new byte[FlipSliceRawCount];

            for (int i = 0; i < flipSliceClassIndices.Length; i++)
            {
                flipSliceClassIndices[i] = UnassignedClass;
            }

            List<int> representatives = new List<int>(FlipSliceClassCount);
            int[] transformedPermutation = new int[12];
            int[] transformedOrientation = new int[12];

            for (int rawIndex = 0; rawIndex < FlipSliceRawCount; rawIndex++)
            {
                if (flipSliceClassIndices[rawIndex] != UnassignedClass)
                {
                    continue;
                }

                int classIndex = representatives.Count;
                if (classIndex >= UnassignedClass)
                {
                    throw new InvalidOperationException("Too many FlipSlice symmetry classes");
                }

                flipSliceClassIndices[rawIndex] = (ushort)classIndex;
                flipSliceSymmetries[rawIndex] = 0;
                representatives.Add(rawIndex);

                int edgeOrientationIndex = rawIndex % Phase1Coordinate.EdgeOrientationCount;
                int slicePositionIndex = rawIndex / Phase1Coordinate.EdgeOrientationCount;
                int[] edgePermutation = Phase1Coordinate.GetEdgePermutationFromSliceIndex(slicePositionIndex);
                int[] edgeOrientation = Phase1Coordinate.GetEdgeOrientationFromIndex(edgeOrientationIndex);

                for (int symmetryIndex = 0; symmetryIndex < Phase1Symmetry.Count; symmetryIndex++)
                {
                    Phase1Symmetry.TransformEdgesInverseFirst(
                        edgePermutation,
                        edgeOrientation,
                        symmetryIndex,
                        transformedPermutation,
                        transformedOrientation);

                    int transformedRawIndex = GetRawFlipSliceIndex(
                        Phase1Coordinate.GetEdgeOrientationIndex(transformedOrientation),
                        Phase1Coordinate.GetSlicePositionIndex(transformedPermutation));

                    if (flipSliceClassIndices[transformedRawIndex] == UnassignedClass)
                    {
                        flipSliceClassIndices[transformedRawIndex] = (ushort)classIndex;
                        flipSliceSymmetries[transformedRawIndex] = (byte)symmetryIndex;
                    }
                }
            }

            if (representatives.Count != FlipSliceClassCount)
            {
                throw new InvalidOperationException(
                    "Expected " + FlipSliceClassCount + " FlipSlice classes but built " + representatives.Count);
            }

            flipSliceRepresentatives = representatives.ToArray();
        }

        private static void BuildRepresentativeStabilizers()
        {
            representativeStabilizers = new ushort[FlipSliceClassCount];
            int[] transformedPermutation = new int[12];
            int[] transformedOrientation = new int[12];

            for (int classIndex = 0; classIndex < FlipSliceClassCount; classIndex++)
            {
                int representative = flipSliceRepresentatives[classIndex];
                int edgeOrientationIndex = representative % Phase1Coordinate.EdgeOrientationCount;
                int slicePositionIndex = representative / Phase1Coordinate.EdgeOrientationCount;
                int[] edgePermutation = Phase1Coordinate.GetEdgePermutationFromSliceIndex(slicePositionIndex);
                int[] edgeOrientation = Phase1Coordinate.GetEdgeOrientationFromIndex(edgeOrientationIndex);
                int stabilizers = 0;

                for (int symmetryIndex = 0; symmetryIndex < Phase1Symmetry.Count; symmetryIndex++)
                {
                    Phase1Symmetry.TransformEdges(
                        edgePermutation,
                        edgeOrientation,
                        symmetryIndex,
                        transformedPermutation,
                        transformedOrientation);

                    int transformedRawIndex = GetRawFlipSliceIndex(
                        Phase1Coordinate.GetEdgeOrientationIndex(transformedOrientation),
                        Phase1Coordinate.GetSlicePositionIndex(transformedPermutation));

                    if (transformedRawIndex == representative)
                    {
                        stabilizers |= 1 << symmetryIndex;
                    }
                }

                representativeStabilizers[classIndex] = (ushort)stabilizers;
            }
        }

        private static void Save(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using (BinaryWriter writer = new BinaryWriter(File.Create(filePath)))
            {
                writer.Write(FileMagic);
                writer.Write(FileVersion);
                writer.Write(FlipSliceRawCount);
                writer.Write(FlipSliceClassCount);
                writer.Write(Phase1Symmetry.Count);
                writer.Write(Phase1Coordinate.CornerOrientationCount);
                WriteUInt16Array(writer, flipSliceClassIndices);
                writer.Write(flipSliceSymmetries);
                WriteInt32Array(writer, flipSliceRepresentatives);
                WriteUInt16Array(writer, cornerOrientationConjugates);
                WriteUInt16Array(writer, representativeStabilizers);
            }
        }

        private static void Load(string filePath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
            {
                if (reader.ReadInt32() != FileMagic
                    || reader.ReadInt32() != FileVersion
                    || reader.ReadInt32() != FlipSliceRawCount
                    || reader.ReadInt32() != FlipSliceClassCount
                    || reader.ReadInt32() != Phase1Symmetry.Count
                    || reader.ReadInt32() != Phase1Coordinate.CornerOrientationCount)
                {
                    throw new InvalidDataException("Invalid Phase 1 symmetry table header");
                }

                flipSliceClassIndices = ReadUInt16Array(reader, FlipSliceRawCount);
                flipSliceSymmetries = reader.ReadBytes(FlipSliceRawCount);
                flipSliceRepresentatives = ReadInt32Array(reader, FlipSliceClassCount);
                cornerOrientationConjugates = ReadUInt16Array(
                    reader,
                    Phase1Coordinate.CornerOrientationCount * Phase1Symmetry.Count);
                representativeStabilizers = ReadUInt16Array(reader, FlipSliceClassCount);

                if (flipSliceSymmetries.Length != FlipSliceRawCount
                    || reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    throw new InvalidDataException("Invalid Phase 1 symmetry table length");
                }
            }

            stopwatch.Stop();
            LastBuildStats = new Phase1SymmetryBuildStats
            {
                ClassCount = flipSliceRepresentatives.Length,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                LoadedFromFile = true
            };
        }

        private static void WriteUInt16Array(BinaryWriter writer, ushort[] values)
        {
            byte[] bytes = new byte[values.Length * sizeof(ushort)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            writer.Write(bytes);
        }

        private static ushort[] ReadUInt16Array(BinaryReader reader, int length)
        {
            byte[] bytes = reader.ReadBytes(length * sizeof(ushort));
            if (bytes.Length != length * sizeof(ushort))
            {
                throw new EndOfStreamException();
            }

            ushort[] values = new ushort[length];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return values;
        }

        private static void WriteInt32Array(BinaryWriter writer, int[] values)
        {
            byte[] bytes = new byte[values.Length * sizeof(int)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            writer.Write(bytes);
        }

        private static int[] ReadInt32Array(BinaryReader reader, int length)
        {
            byte[] bytes = reader.ReadBytes(length * sizeof(int));
            if (bytes.Length != length * sizeof(int))
            {
                throw new EndOfStreamException();
            }

            int[] values = new int[length];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return values;
        }
    }
}
