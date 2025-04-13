using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RubiksCubeSim;
using System.IO;
using Newtonsoft.Json;
[System.Serializable] //make the class JSON-compatible

public class PDBLoader : MonoBehaviour
{
    private Dictionary<string, int> cornerPDB = new Dictionary<string, int>();
    private Dictionary<string, int> firstEightEdgesPDB = new Dictionary<string, int>();
    private Dictionary<string, int> lastFourEdgesPDB = new Dictionary<string, int>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadPDB()
    {
        string folderPath = PatternDatabaseManager.GetDatabasePath();
        for (int depth = 0; depth <= 20; depth++)
        {
            //Debug.Log(folderPath);
            string filePath = folderPath + "/depth_" + depth + ".json";
            Debug.Log(filePath);
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(jsonContent))
                {
                    List<CubeStateData> jsonStates = PatternDatabaseManager.LoadPatternDatabase(filePath);
                    for (int stateIndex = 0; stateIndex < jsonStates.Count; stateIndex++)
                    {
                        CubeStateData state = jsonStates[stateIndex];
                        List<int> stateCornerPermutation = state.cornerPermutation;
                        List<int> stateCornerOrientation = state.cornerOrientation;
                        List<int> stateFirstEdgesPermutation = state.firstEightEdgePermutation;
                        List<int> stateFirstEdgesOrientation = state.firstEightEdgeOrientation;
                        List<int> stateLastEdgesPermutation = state.lastFourEdgePermutation;
                        List<int> stateLastEdgesOrientation = state.lastFourEdgeOrientation;
                        int depthHeuristic = state.depth;

                        string cornerKey = GetPDBKey(stateCornerPermutation, stateCornerOrientation);
                        string firstEdges = GetPDBKey(stateFirstEdgesPermutation, stateFirstEdgesOrientation);
                        string lastEdges = GetPDBKey(stateLastEdgesPermutation, stateLastEdgesOrientation);

                        UpdatePDB(cornerKey, depthHeuristic, cornerPDB);
                        UpdatePDB(firstEdges, depthHeuristic, firstEightEdgesPDB);
                        UpdatePDB(lastEdges, depthHeuristic, lastFourEdgesPDB);
                    }
                }
            }
        }


        //TESTING PDBs
        //foreach (KeyValuePair<string, int> kvp in cornerPDB)
        //{
        //    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
        //    Debug.Log("Key = " + kvp.Key + " Value = " + kvp.Value);
        //}
        //foreach (KeyValuePair<string, int> kvp in firstEightEdgesPDB)
        //{
        //    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
        //    Debug.Log("Key = " + kvp.Key + " Value = " + kvp.Value);
        //}
        //foreach (KeyValuePair<string, int> kvp in lastFourEdgesPDB)
        //{
        //    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
        //    Debug.Log("Key = " + kvp.Key + " Value = " + kvp.Value);
        //}
    }
    private string GetPDBKey(List<int> permutation, List<int> orientation)
    {
        string key = null;
        string permutationString = null;
        string orientationString = null;

        for(int i = 0; i < permutation.Count; i++)
        {
            permutationString += permutation[i].ToString();
            orientationString += orientation[i].ToString();
        }
        key = permutationString + "-" + orientationString;
        return key;
    }

    private void UpdatePDB(string key, int depth, Dictionary<string, int> pdb)
    {
        if (pdb.ContainsKey(key))
        {
            if(pdb[key] > depth)
            {
                pdb[key] = depth;
            }
        }
        else
        {
            pdb.Add(key, depth);
        }
    }
}
