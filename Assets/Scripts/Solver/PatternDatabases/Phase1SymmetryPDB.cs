using Assets.Scripts.Core;
using Assets.Scripts.Solver.Coordinates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Solver.PatternDatabases
{
    public class Phase1SymmetryPdbGenerationStats
    {
        public int MaxDepth;
        public long VisitedStates;
        public long ElapsedMilliseconds;
        public long[] DepthCounts;
    }

    public static class Phase1SymmetryPDB
    {
        public const byte Unvisited = byte.MaxValue;
        public const string FileName = "phase1_exact_symmetry.pdb";
        public const string TemporaryFileName = FileName + ".tmp";
        public const string ProgressFileName = FileName + ".inprogress";

        private const int BackwardSearchStartDepth = 9;

        public static Phase1SymmetryPdbGenerationStats LastGenerationStats { get; private set; }

        public static string FilePath
        {
            get { return Application.dataPath + "/PatternDatabase/" + FileName; }
        }

        public static string TemporaryFilePath
        {
            get { return Application.dataPath + "/PatternDatabase/" + TemporaryFileName; }
        }

        public static string ProgressFilePath
        {
            get { return Application.dataPath + "/PatternDatabase/" + ProgressFileName; }
        }

        public static Dictionary<int, byte> GenerateTiny(int maxDepth)
        {
            Prepare();

            Dictionary<int, byte> database = new Dictionary<int, byte>();
            Queue<int> queue = new Queue<int>();
            int solvedIndex = GetSolvedIndex();

            database[solvedIndex] = 0;
            queue.Enqueue(solvedIndex);

            while (queue.Count > 0)
            {
                int currentIndex = queue.Dequeue();
                int currentDepth = database[currentIndex];

                if (currentDepth >= maxDepth)
                {
                    continue;
                }

                for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                {
                    int childIndex = GetMovedIndexPrepared(currentIndex, moveId);
                    if (database.ContainsKey(childIndex))
                    {
                        continue;
                    }

                    database[childIndex] = (byte)(currentDepth + 1);
                    queue.Enqueue(childIndex);
                }
            }

            return database;
        }

        public static byte[] GenerateFull(Action<string> reportProgress = null)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Prepare();

            byte[] database = new byte[Phase1SymmetryCoordinate.ExactPhase1EntryCount];
            Array.Fill(database, Unvisited);

            List<long> depthCounts = new List<long>();
            int solvedIndex = GetSolvedIndex();
            database[solvedIndex] = 0;
            depthCounts.Add(1);

            long visitedStates = 1;
            int depth = 0;
            reportProgress?.Invoke(
                "Exact Phase 1 PDB depth 0 states: 1 / " + database.Length);

            while (visitedStates < database.Length)
            {
                int nextDepth = depth + 1;
                long newStates = depth < BackwardSearchStartDepth
                    ? ExpandForward(database, depth, nextDepth)
                    : ExpandBackward(database, depth, nextDepth);

                if (newStates == 0)
                {
                    throw new InvalidOperationException(
                        "Exact Phase 1 PDB generation stopped before every entry was reached");
                }

                visitedStates += newStates;
                depthCounts.Add(newStates);
                depth = nextDepth;

                reportProgress?.Invoke(
                    "Exact Phase 1 PDB depth " + depth
                    + " new states: " + newStates
                    + " | total: " + visitedStates + " / " + database.Length);
            }

            stopwatch.Stop();
            LastGenerationStats = new Phase1SymmetryPdbGenerationStats
            {
                MaxDepth = depth,
                VisitedStates = visitedStates,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DepthCounts = depthCounts.ToArray()
            };

            return database;
        }

        public static byte[] GenerateFullAndSave(Action<string> reportProgress = null)
        {
            string folderPath = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (HasValidSavedFile())
            {
                File.Delete(TemporaryFilePath);
                File.Delete(ProgressFilePath);
                reportProgress?.Invoke("Exact Phase 1 PDB already exists and has the correct size");
                return Load(FilePath);
            }

            File.Delete(TemporaryFilePath);
            WriteProgress("Exact Phase 1 PDB generation started");

            try
            {
                byte[] database = GenerateFull(message =>
                {
                    WriteProgress(message);
                    reportProgress?.Invoke(message);
                });

                WriteProgress("Writing exact Phase 1 PDB temporary file");
                Save(database, TemporaryFilePath);
                ValidateSavedFile(TemporaryFilePath);

                if (File.Exists(FilePath))
                {
                    File.Replace(TemporaryFilePath, FilePath, null);
                }
                else
                {
                    File.Move(TemporaryFilePath, FilePath);
                }

                ValidateSavedFile(FilePath);
                File.Delete(ProgressFilePath);
                reportProgress?.Invoke("Exact Phase 1 PDB saved and validated");
                return database;
            }
            catch (Exception exception)
            {
                try
                {
                    WriteProgress("Generation failed: " + exception);
                }
                catch
                {
                }

                throw;
            }
        }

        public static bool HasValidSavedFile()
        {
            return File.Exists(FilePath)
                && new FileInfo(FilePath).Length
                    == Phase1SymmetryCoordinate.ExactPhase1EntryCount;
        }

        public static void Save(byte[] database, string filePath)
        {
            if (database == null
                || database.Length != Phase1SymmetryCoordinate.ExactPhase1EntryCount)
            {
                throw new ArgumentException("Invalid exact Phase 1 PDB size", nameof(database));
            }

            string folderPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                1024 * 1024,
                FileOptions.WriteThrough))
            {
                stream.Write(database, 0, database.Length);
                stream.Flush(true);
            }
        }

        public static byte[] Load(string filePath)
        {
            byte[] database = File.ReadAllBytes(filePath);

            if (database.Length != Phase1SymmetryCoordinate.ExactPhase1EntryCount)
            {
                throw new InvalidDataException(
                    "Invalid exact Phase 1 PDB size: " + database.Length + " bytes");
            }

            return database;
        }

        private static void ValidateSavedFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Exact Phase 1 PDB was not written", filePath);
            }

            long fileLength = new FileInfo(filePath).Length;
            if (fileLength != Phase1SymmetryCoordinate.ExactPhase1EntryCount)
            {
                throw new InvalidDataException(
                    "Invalid exact Phase 1 PDB file size: " + fileLength + " bytes");
            }
        }

        private static void WriteProgress(string message)
        {
            File.WriteAllText(
                ProgressFilePath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + message);
        }

        public static int GetMovedIndex(int index, int moveId)
        {
            Prepare();
            return GetMovedIndexPrepared(index, moveId);
        }

        private static long ExpandForward(byte[] database, int depth, int nextDepth)
        {
            long newStates = 0;
            int cornerOrientationCount = Phase1Coordinate.CornerOrientationCount;

            for (int classIndex = 0;
                classIndex < Phase1SymmetryCoordinate.FlipSliceClassCount;
                classIndex++)
            {
                int classBaseIndex = classIndex * cornerOrientationCount;

                for (int cornerOrientationIndex = 0;
                    cornerOrientationIndex < cornerOrientationCount;
                    cornerOrientationIndex++)
                {
                    int index = classBaseIndex + cornerOrientationIndex;
                    if (database[index] != depth)
                    {
                        continue;
                    }

                    for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                    {
                        int childIndex = GetMovedIndexPrepared(index, moveId);
                        if (database[childIndex] != Unvisited)
                        {
                            continue;
                        }

                        newStates += SetWithStabilizers(
                            database,
                            childIndex,
                            (byte)nextDepth);
                    }
                }
            }

            return newStates;
        }

        private static long ExpandBackward(byte[] database, int depth, int nextDepth)
        {
            long newStates = 0;

            for (int index = 0; index < database.Length; index++)
            {
                if (database[index] != Unvisited)
                {
                    continue;
                }

                for (int moveId = 0; moveId < MoveGenerator.AllMoves.Length; moveId++)
                {
                    int neighborIndex = GetMovedIndexPrepared(index, moveId);
                    if (database[neighborIndex] == depth)
                    {
                        database[index] = (byte)nextDepth;
                        newStates++;
                        break;
                    }
                }
            }

            return newStates;
        }

        private static long SetWithStabilizers(byte[] database, int index, byte depth)
        {
            int cornerOrientationCount = Phase1Coordinate.CornerOrientationCount;
            int classIndex = index / cornerOrientationCount;
            int cornerOrientationIndex = index % cornerOrientationCount;
            int stabilizers = Phase1SymmetryCoordinate.GetRepresentativeStabilizersPrepared(classIndex);
            long added = 0;

            for (int symmetryIndex = 0;
                symmetryIndex < Phase1Symmetry.Count;
                symmetryIndex++)
            {
                if ((stabilizers & (1 << symmetryIndex)) == 0)
                {
                    continue;
                }

                int equivalentCornerOrientation =
                    Phase1SymmetryCoordinate.GetCornerOrientationConjugatePrepared(
                        cornerOrientationIndex,
                        symmetryIndex);
                int equivalentIndex = classIndex * cornerOrientationCount + equivalentCornerOrientation;

                if (database[equivalentIndex] == Unvisited)
                {
                    database[equivalentIndex] = depth;
                    added++;
                }
            }

            return added;
        }

        private static int GetMovedIndexPrepared(int index, int moveId)
        {
            int cornerOrientationCount = Phase1Coordinate.CornerOrientationCount;
            int classIndex = index / cornerOrientationCount;
            int cornerOrientationIndex = index % cornerOrientationCount;
            int movedCornerOrientation =
                Phase1MoveTables.GetCornerOrientationAfterMovePrepared(cornerOrientationIndex, moveId);
            int movedClassIndex =
                Phase1SymmetryCoordinate.GetMovedClassPrepared(classIndex, moveId);
            int movedSymmetryIndex =
                Phase1SymmetryCoordinate.GetMovedSymmetryPrepared(classIndex, moveId);
            int canonicalCornerOrientation =
                Phase1SymmetryCoordinate.GetCornerOrientationConjugatePrepared(
                    movedCornerOrientation,
                    movedSymmetryIndex);

            return movedClassIndex * cornerOrientationCount + canonicalCornerOrientation;
        }

        private static int GetSolvedIndex()
        {
            SolverStateData solved = SolverStateData.FromCubeStateData(CubeState.CreateSolvedState());
            return Phase1SymmetryCoordinate.GetExactIndex(solved);
        }

        private static void Prepare()
        {
            Phase1MoveTables.BuildIfNeeded();
            Phase1SymmetryCoordinate.PrepareMoveTables();
        }
    }
}
