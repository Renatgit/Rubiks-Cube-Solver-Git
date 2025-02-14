using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    List<GameObject> BuildRays (Transform rayTransform, Vector3 direction) //function that builds the grids of rays for each side of the cube
    {
        int rayCount = 0; //used to name rays to make sure they are in right order

        List<GameObject> rays = new List<GameObject>();

        //Creates 9 rays in the shape of the side od the cube
        //Ray 0 at the top left and Ray 8 at the bottom right 

        for(int y = 1; y > -2; y--) 
        {
            for(int x = -1; x < 2; x++)
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

        //update the map with the found positions
        cubeMap.Set();
    }
}
