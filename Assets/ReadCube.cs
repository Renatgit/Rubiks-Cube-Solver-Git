using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RubiksCubeSim;
using System.Text;

public class ReadCube : MonoBehaviour
{
    // Transform variable for each ray
    public Transform tUp;
    public Transform tDown;
    public Transform tRight;
    public Transform tLeft;
    public Transform tFront;
    public Transform tBack;

    //Lists to store the 9 rays created for each side and transforms arranged in grif pattern 
    private List<GameObject> frontRays = new List<GameObject>();
    private List<GameObject> backRays = new List<GameObject>();
    private List<GameObject> leftRays = new List<GameObject>();
    private List<GameObject> rightRays = new List<GameObject>();
    private List<GameObject> upRays = new List<GameObject>();
    private List<GameObject> downRays = new List<GameObject>();


    //layer mask for the faces of the cube
    private int layerMask = 1 << 6;

    CubeState cubeState;
    CubeMap cubeMap;
    public GameObject emptyGO;


    // Start is called before the first frame update
    void Start()
    {
        SetRayTransforms();

        cubeState = FindObjectOfType<CubeState>();
        cubeMap = FindObjectOfType<CubeMap>();

        ReadState();
        CubeState.start = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    //populate the ray lists with raycasts eminating from transform, angled owards the cube
    void SetRayTransforms() //Set the transform for rays for each side
    {
        upRays = BuildRays(tUp, new Vector3(90, 90, 0));
        downRays = BuildRays(tDown, new Vector3(270, 90, 0));
        leftRays = BuildRays(tLeft, new Vector3(0, 180, 0));
        rightRays = BuildRays(tRight, new Vector3(0, 0, 0));
        frontRays = BuildRays(tFront, new Vector3(0, 90, 0));
        backRays = BuildRays(tBack, new Vector3(0, 270, 0));
    }

    List<GameObject> BuildRays(Transform rayTransform, Vector3 direction) //function that builds the grids of rays for each side of the cube
    {
        int rayCount = 0; //used to name rays to make sure they are in right order

        List<GameObject> rays = new List<GameObject>();

        //Creates 9 rays in the shape of the side od the cube
        //Ray 0 at the top left and Ray 8 at the bottom right 

        for (int y = 1; y > -2; y--)
        {
            for (int x = -1; x < 2; x++)
            {
                Vector3 startPos = new Vector3(rayTransform.localPosition.x + x, rayTransform.localPosition.y + y, rayTransform.localPosition.z);

                //This line creates a new GameObject by duplicating emptyGO at the position startPos, with no rotation (Quaternion.identity),
                //and attaches it to rayTransform as its parent.
                GameObject rayStart = Instantiate(emptyGO, startPos, Quaternion.identity, rayTransform);

                rayStart.name = rayCount.ToString();
                rays.Add(rayStart);
                rayCount++;
            }
        }
        rayTransform.localRotation = Quaternion.Euler(direction); //rotate the ray relative to its own axes
        return rays;
    }

    public List<GameObject> ReadFace(List<GameObject> rayStarts, Transform rayTransform)
    {
        List<GameObject> facesHit = new List<GameObject>(); //List of the faces hit

        foreach (GameObject raySt in rayStarts)
        {
            Vector3 ray = raySt.transform.position;
            RaycastHit hit;

            //if the ray intersect with any object in the layerMask(layer 6)
            if (Physics.Raycast(ray, rayTransform.forward, out hit, Mathf.Infinity, layerMask))
            {
                facesHit.Add(hit.collider.gameObject);
            }
        }
        return facesHit;
    }

    public void ReadState()
    {
        cubeState = FindObjectOfType<CubeState>();
        cubeMap = FindObjectOfType<CubeMap>();

        //set the state of each position in the list of sides
        //so we know color is in what position

        cubeState.up = ReadFace(upRays, tUp);
        cubeState.down = ReadFace(downRays, tDown);
        cubeState.left = ReadFace(leftRays, tLeft);
        cubeState.right = ReadFace(rightRays, tRight);
        cubeState.front = ReadFace(frontRays, tFront);
        cubeState.back = ReadFace(backRays, tBack);



        //---------------CORNERS------------------------

        //Get the corner names directly from fixed positions
        string[] scannedCorners = new string[8];

        scannedCorners[0] = cubeState.up[8].transform.parent.name; // UFR
        scannedCorners[1] = cubeState.up[6].transform.parent.name;  // UFL
        scannedCorners[2] = cubeState.up[2].transform.parent.name;  // URB
        scannedCorners[3] = cubeState.up[0].transform.parent.name;  // ULB
        scannedCorners[4] = cubeState.down[2].transform.parent.name; // DRF
        scannedCorners[5] = cubeState.down[0].transform.parent.name; // DLF
        scannedCorners[6] = cubeState.down[8].transform.parent.name; // DRB
        scannedCorners[7] = cubeState.down[6].transform.parent.name; // DLB

        //pass corners current permutation in terms of names 
        //to convert them in digit format in CubeState
        cubeState.cornersNamesPermutation = scannedCorners;

        //Get the corners' sticker name
        int[] scannedStickerOrientation = new int[8];

        string stickerRightOrientation = cubeState.up[8].name;
        string stickerClockwise = cubeState.right[0].name;
        string stickerAntiClockwise = cubeState.front[2].name;
        scannedStickerOrientation[0] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 0

        stickerRightOrientation = cubeState.up[6].name;
        stickerClockwise = cubeState.front[0].name;
        stickerAntiClockwise = cubeState.left[2].name;
        scannedStickerOrientation[1] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 1

        stickerRightOrientation = cubeState.up[2].name;
        stickerClockwise = cubeState.back[0].name;
        stickerAntiClockwise = cubeState.right[2].name;
        scannedStickerOrientation[2] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 2

        stickerRightOrientation = cubeState.up[0].name;
        stickerClockwise = cubeState.left[0].name;
        stickerAntiClockwise = cubeState.back[2].name;
        scannedStickerOrientation[3] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 3

        stickerRightOrientation = cubeState.down[2].name;
        stickerClockwise = cubeState.front[8].name;
        stickerAntiClockwise = cubeState.right[6].name;
        scannedStickerOrientation[4] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 4

        stickerRightOrientation = cubeState.down[0].name;
        stickerClockwise = cubeState.left[8].name;
        stickerAntiClockwise = cubeState.front[6].name;
        scannedStickerOrientation[5] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 5

        stickerRightOrientation = cubeState.down[8].name;
        stickerClockwise = cubeState.right[8].name;
        stickerAntiClockwise = cubeState.back[6].name;
        scannedStickerOrientation[6] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 6

        stickerRightOrientation = cubeState.down[6].name;
        stickerClockwise = cubeState.back[8].name;
        stickerAntiClockwise = cubeState.left[6].name;
        scannedStickerOrientation[7] = cubeState.CalculateCornerOrientation(stickerRightOrientation, stickerClockwise, stickerAntiClockwise); //orientation of the corner on the position 7

        //pass corners current orientation
        cubeState.cornerCurrentOrientation = scannedStickerOrientation;




        //---------------EDGES------------------------

        // Edge mappings based on solved state (adjust these based on your naming system)
        Dictionary<string, int> edgeIndices = new Dictionary<string, int>
        {
            {"UR", 0}, {"UL", 1}, {"UB", 2}, {"UF", 3},
            {"DR", 4}, {"DL", 5}, {"DB", 6}, {"DF", 7},
            {"FR", 8}, {"FL", 9}, {"BR", 10}, {"BL", 11}
        };
        //Get 12 edges of the cube and divide into two 8 and 4-item arrays
        //in location order
        List<string> firstEightEdges = new List<string>();
        List<string> lastFourEdges = new List<string>();
        List<int> firstEightEdgesOrientation = new List<int>();
        List<int> lastFourEdgesOrientation = new List<int>();

        // Get all edge pieces' current names and locations
        string[] edgeNames = new string[12];
        // Get all edge pieces' current orientation
        int[] edgeGroupedOrientation = new int [12];

        //First 8 edges (UR, UL, UB, UF, DR, DL, DB, DF)
        edgeNames[0] = cubeState.up[5].transform.parent.name;  // UR(or the piece in its position and same for all the following)

        string stickerHit = cubeState.up[5].name;
        string secondStickerHit = cubeState.right[1].name; 
        string sideAligned = "Up";
        string secondSideAligned = "R";
        edgeGroupedOrientation[0] = cubeState.CalculateEdgeOrientation(edgeNames[0], stickerHit, sideAligned, secondSideAligned, secondStickerHit);


        edgeNames[1] = cubeState.up[3].transform.parent.name;  // UL
        stickerHit = cubeState.up[3].name;
        secondStickerHit = cubeState.left[1].name;
        secondSideAligned = "L";
        edgeGroupedOrientation[1] = cubeState.CalculateEdgeOrientation(edgeNames[1], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        edgeNames[2] = cubeState.up[1].transform.parent.name;  // UB
        stickerHit = cubeState.up[1].name;
        secondStickerHit = cubeState.back[1].name;
        secondSideAligned = "B";
        edgeGroupedOrientation[2] = cubeState.CalculateEdgeOrientation(edgeNames[2], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        edgeNames[3] = cubeState.up[7].transform.parent.name;  // UF
        stickerHit = cubeState.up[7].name;
        secondStickerHit = cubeState.front[1].name;
        secondSideAligned = "F";
        edgeGroupedOrientation[3] = cubeState.CalculateEdgeOrientation(edgeNames[3], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        //------------------------------------//
        sideAligned = "Down";
        edgeNames[4] = cubeState.down[5].transform.parent.name; // DR
        stickerHit = cubeState.down[5].name;
        secondStickerHit = cubeState.right[7].name;
        secondSideAligned = "R";
        edgeGroupedOrientation[4] = cubeState.CalculateEdgeOrientation(edgeNames[4], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        edgeNames[5] = cubeState.down[3].transform.parent.name; // DL
        stickerHit = cubeState.down[3].name;
        secondStickerHit = cubeState.left[7].name;
        secondSideAligned = "L";
        edgeGroupedOrientation[5] = cubeState.CalculateEdgeOrientation(edgeNames[5], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        edgeNames[6] = cubeState.down[7].transform.parent.name; // DB
        stickerHit = cubeState.down[7].name;
        secondStickerHit = cubeState.back[7].name;
        secondSideAligned = "B";
        edgeGroupedOrientation[6] = cubeState.CalculateEdgeOrientation(edgeNames[6], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        edgeNames[7] = cubeState.down[1].transform.parent.name; // DF
        stickerHit = cubeState.down[1].name;
        secondStickerHit = cubeState.front[7].name;
        secondSideAligned = "F";
        edgeGroupedOrientation[7] = cubeState.CalculateEdgeOrientation(edgeNames[7], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        //Last 4 edges (FR, FL, BR, BL)
        sideAligned = "Front";
        edgeNames[8] = cubeState.front[5].transform.parent.name;  // FR
        stickerHit = cubeState.front[5].name;
        secondStickerHit = cubeState.right[3].name;
        secondSideAligned = "R";
        edgeGroupedOrientation[8] = cubeState.CalculateEdgeOrientation(edgeNames[8], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        edgeNames[9] = cubeState.front[3].transform.parent.name;  // FL
        stickerHit = cubeState.front[3].name;
        secondStickerHit = cubeState.left[5].name;
        secondSideAligned = "L";
        edgeGroupedOrientation[9] = cubeState.CalculateEdgeOrientation(edgeNames[9], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        sideAligned = "Back";
        edgeNames[10] = cubeState.back[3].transform.parent.name;  // BR
        stickerHit = cubeState.back[3].name;
        secondStickerHit = cubeState.right[5].name;
        secondSideAligned = "R";
        edgeGroupedOrientation[10] = cubeState.CalculateEdgeOrientation(edgeNames[10], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        edgeNames[11] = cubeState.back[5].transform.parent.name;  // BL
        stickerHit = cubeState.back[5].name;
        secondStickerHit = cubeState.left[3].name;
        secondSideAligned = "L";
        edgeGroupedOrientation[11] = cubeState.CalculateEdgeOrientation(edgeNames[11], stickerHit, sideAligned, secondSideAligned, secondStickerHit);

        //Saves the full edge premutation list
        List<int> fullEdgePermutation = new List<int>();

        for (int i = 0; i < edgeNames.Length; i++)
        {
            fullEdgePermutation.Add(edgeIndices[edgeNames[i]]); //Add the index of the edge to the list
        }

        //Pass edge names to CubeState
        cubeState.fullEdgesPermutation = fullEdgePermutation.ToArray();

        //Pass the orientation of the edges to CubeState
        cubeState.fullEdgesOrientation = edgeGroupedOrientation;
        


        //update the map with the found positions
        cubeMap.Set();


        //Log the current state of the cube
        Debug.Log("Corner Permutation: [" + string.Join(", ", cubeState.GetCubeStateData().cornerPermutation) + "]");
        Debug.Log("Corner Orientation: [" + string.Join(", ", cubeState.GetCubeStateData().cornerOrientation) + "]");
        Debug.Log("Edge 12 Permutation: [" + string.Join(", ", cubeState.GetCubeStateData().fullEdgePermutation) + "]");
        Debug.Log("Edge 12 Orientation: [" + string.Join(", ", cubeState.GetCubeStateData().fullEdgeOrientation) + "]");

        Debug.Log("Edge 8 Permutation: [" + string.Join(", ", cubeState.GetCubeStateData().firstEightEdgePermutation) + "]");
        Debug.Log("Edge 4 Permutation: [" + string.Join(", ", cubeState.GetCubeStateData().lastFourEdgePermutation) + "]");
        Debug.Log("Edge 8 Orientation: [" + string.Join(", ", cubeState.GetCubeStateData().firstEightEdgeOrientation) + "]");
        Debug.Log("Edge 4 Orientation: [" + string.Join(", ", cubeState.GetCubeStateData().lastFourEdgeOrientation) + "]");

        MoveProcessor.ApplyMove(cubeState.GetCubeStateData(), "R");

    }
}
