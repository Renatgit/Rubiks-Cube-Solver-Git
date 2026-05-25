using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable] //make the class JSON-compatible
public class PatternDatabaseManager : MonoBehaviour
{
    private static string folderPath;
    private PDBLoader pdbLoader = new PDBLoader();
    // Start is called before the first frame update
    void Start()
    {
        InitializePatternDatabaseFolder(); //initialises the folder for a pattern database in current directory
        CreateEmptyPatternFiles(20);
        pdbLoader.LoadPDB();
    }

    // Update is called once per frame
    void Update()
    {

    }

    //Creates the "PatternDatabase" folder inside Assets/ if it doesn't exist
    private void InitializePatternDatabaseFolder()
    {
        string currentDirectory = Application.dataPath; // "Assets/" folder in Unity
        folderPath = currentDirectory + "/PatternDatabase";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            // Debug.Log("PatternDatabase folder created at: " + folderPath);
        }
    }

    //Get the folder path (Used when saving/loading files)
    public static string GetDatabasePath()
    {
        return folderPath;
    }

    //Save the given CubeStateData to a JSON file (depth_X.json)
    public static void SaveStateToPatternDatabase(CubeStateData newState)
    {
        string folderPath = GetDatabasePath();
        string filePath = folderPath + "/depth_" + newState.depth + ".json";
        
        // Load existing states
        List<CubeStateData> database = LoadPatternDatabase(filePath);

        // Prevent from saving duplicates
        foreach (var state in database)
        {
            if (AreStatesEqual(state, newState)) return;
        }

        // Add new state and save it
        database.Add(newState);
        string json = JsonConvert.SerializeObject(database, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    //Load the pattern database from a JSON file
    public static List<CubeStateData> LoadPatternDatabase(string filePath)
    {
        if (!File.Exists(filePath)) return new List<CubeStateData>();

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<List<CubeStateData>>(json);
    }

    //Compare two cube states to avoid saving duplicates
    private static bool AreStatesEqual(CubeStateData state1, CubeStateData state2)
    {
        return JsonConvert.SerializeObject(state1) == JsonConvert.SerializeObject(state2);
    }

    //Create json files for saving states with depth 0-20
    public static void CreateEmptyPatternFiles(int maxDepth)
    {
        string folderPath = GetDatabasePath();

        for (int depth = 0; depth <= maxDepth; depth++)
        {
            string filePath = folderPath + "/depth_" + depth + ".json";

            // If file doesn't exist, create an empty JSON array
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]"); // Empty list in JSON format
            }
        }
    }
}
