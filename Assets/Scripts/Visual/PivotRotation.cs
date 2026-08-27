using System.Collections.Generic;
using UnityEngine;

public class PivotRotation : MonoBehaviour
{
    private List<GameObject> activeSide;
    private Vector3 mouseRef;
    private bool dragIsActive = false;
    
    private CubeState cubeState;
    private ReadCube readCube;

    private float sensitivity = 0.4f;
    private Vector3 rotation;

    private bool autoRotation = false;
    private Quaternion targetQuaternion;
    private float speed = 200f; //speed of autorotation

    private int indexOfPiece;

    private bool currentMoveIsManual = false;
    private float manualTurnAmount = 0f;

    private CubeGameController cubeGameController;

    // Start is called before the first frame update
    void Start()
    {
        // Use non-order-dependent API to avoid deprecated FindObject methods
        cubeState = UnityEngine.Object.FindAnyObjectByType<CubeState>();
        readCube = UnityEngine.Object.FindAnyObjectByType<ReadCube>();
        cubeGameController = UnityEngine.Object.FindAnyObjectByType<CubeGameController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (dragIsActive)
        {
            SpinSide(activeSide);
            if (Input.GetMouseButtonUp(0))
            {
                dragIsActive = false;
                AutoRotationAngles();
            }
        }
        if (autoRotation)
        {
            AutoRotate();
        }
    }

    private void SpinSide(List<GameObject> side)
    {
        //reset rotation 
        rotation = Vector3.zero;

        //current mouse position minus the initial mouse position
        Vector3 mouseOffset = Input.mousePosition - mouseRef;

        if(side == cubeState.front) //if front side - rotate around the x-axis 
        {
            RotateFrontSideMouse(indexOfPiece, mouseOffset);
        }
        if(side == cubeState.back) //if back side - rotate around the x-axis 
        {
            RotateBackSideMouse(indexOfPiece, mouseOffset);
        }
        if(side == cubeState.right) //if right side - rotate around the z-axis 
        {
            RotateRightSideMouse(indexOfPiece, mouseOffset);
        }
        if (side == cubeState.left) //if left side - rotate around the z-axis 
        {
            RotateLeftSideMouse(indexOfPiece, mouseOffset);
        }
        if (side == cubeState.up) //if up side - rotate around the y-axis 
        {
            RotateUpSideMouse(indexOfPiece, mouseOffset);
        }
        if (side == cubeState.down) //if bottom side - rotate around the y-axis 
        {
            RotateDownSideMouse(indexOfPiece, mouseOffset);
        }

        //rotate local transform of the object this script is attached to
        transform.Rotate(rotation, Space.Self);

        if (currentMoveIsManual)
        {
            manualTurnAmount += GetRotationAmountForSide(side);
        }

        //store mouse 
        mouseRef = Input.mousePosition;
    }

    public void Rotate(List<GameObject> side, int index)
    {
        //passing the variables 
        activeSide = side;
        mouseRef = Input.mousePosition;
        dragIsActive = true;
        indexOfPiece = index;

        currentMoveIsManual = true;
        manualTurnAmount = 0f;
    }


    public void AutoRotationAngles()
    {
        Vector3 localVectorAngle = transform.localEulerAngles;

        //round localVectorAngle to nearest 90 deg
        localVectorAngle.x = Mathf.Round(localVectorAngle.x / 90) * 90;
        localVectorAngle.y = Mathf.Round(localVectorAngle.y / 90) * 90;
        localVectorAngle.z = Mathf.Round(localVectorAngle.z / 90) * 90;

        targetQuaternion.eulerAngles = localVectorAngle;
        autoRotation = true;
    }

    private void AutoRotate()
    {
        dragIsActive = false;

        float step = speed * Time.deltaTime;
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetQuaternion, step);

        //If within one degree, set angle to target angle and end the rotation
        if(Quaternion.Angle(transform.localRotation, targetQuaternion) <= 1)
        {
            transform.localRotation = targetQuaternion;
            autoRotation = false;

            //Return pieces to its original parents(ungroup side)

            cubeState.UngroupSide(activeSide, transform.parent);

            string completedMove = AutomaticMovement.activeMove;

            if (string.IsNullOrEmpty(completedMove) && currentMoveIsManual)
            {
                completedMove = DetectManualMove();
            }
            //Check for cubeGameController before calling CompleteMove
            if (cubeGameController != null && !string.IsNullOrEmpty(completedMove))
            {
                cubeGameController.CompleteMove(completedMove);
                AutomaticMovement.activeMove = null;
                currentMoveIsManual = false;
            }

            readCube.ReadFacesForMap();
            AutomaticMovement.automaticMovementIsActive = false;
        }
    }

    private string GetFaceLetter(List<GameObject> side)
    {
        if (side == cubeState.front) return "F";
        if (side == cubeState.back) return "B";
        if (side == cubeState.right) return "R";
        if (side == cubeState.left) return "L";
        if (side == cubeState.up) return "U";
        if (side == cubeState.down) return "D";

        return null;
    }

    private string DetectManualMove()
    {
        string face = GetFaceLetter(activeSide);
        if (face == null)
        {
            return null;
        }

        float delta = Mathf.Round(manualTurnAmount / 90f) * 90f;

        if (face == "U" || face == "L" || face == "B")
        {
            delta *= -1f;
        }

        if (Mathf.Abs(delta) == 180f)
        {
            return face + "2";
        }

        if (delta == 90f)
        {
            return face + "'";
        }

        if (delta == -90f)
        {
            return face;
        }

        return null;
    }

    private float GetRotationAmountForSide(List<GameObject> side)
    {
        if (side == cubeState.front || side == cubeState.back)
        {
            return rotation.x;
        }

        if (side == cubeState.right || side == cubeState.left)
        {
            return rotation.z;
        }

        if (side == cubeState.up || side == cubeState.down)
        {
            return rotation.y;
        }

        return 0f;
    }

    public void InitAutomaticMove(List<GameObject> side, float angle)
    {
        currentMoveIsManual = false;

        cubeState.ParentSidePiecesToCenter(side);
        Vector3 localFwd = Vector3.zero - side[4].transform.parent.transform.localPosition;
        targetQuaternion = Quaternion.AngleAxis(angle, localFwd) * transform.localRotation;
        activeSide = side;
        autoRotation = true;
    }

    //Method below are for rotation of the sides using mouse
    public void RotateFrontSideMouse(int indexOfPiece, Vector3 mouseOffset)
    {
        //depending on which piece is dragged the direction of rotation is determined
        if (indexOfPiece == 0 || indexOfPiece == 1 || indexOfPiece == 3 || indexOfPiece == 6)
        {
            rotation.x = (mouseOffset.x + mouseOffset.y) * sensitivity * -1;
        }
        else if (indexOfPiece == 2 || indexOfPiece == 5 || indexOfPiece == 7 || indexOfPiece == 8)
        {
            rotation.x = (mouseOffset.x + mouseOffset.y) * sensitivity * 1;
        }
    }
    public void RotateBackSideMouse(int indexOfPiece, Vector3 mouseOffset)
    {
        if (indexOfPiece == 0 || indexOfPiece == 3 || indexOfPiece == 6)
        {
            rotation.x = (mouseOffset.x + mouseOffset.y) * sensitivity * 1;
        }
        else if (indexOfPiece == 2 || indexOfPiece == 5 || indexOfPiece == 8)
        {
            rotation.x = (mouseOffset.x + mouseOffset.y) * sensitivity * -1;
        }
    }

    public void RotateRightSideMouse(int indexOfPiece, Vector3 mouseOffset)
    {
        if (indexOfPiece == 0 || indexOfPiece == 3 || indexOfPiece == 6)
        {
            rotation.z = (mouseOffset.x + mouseOffset.y) * sensitivity * -1;
        }
        else if (indexOfPiece == 2 || indexOfPiece == 5 || indexOfPiece == 8)
        {
            rotation.z = (mouseOffset.x + mouseOffset.y) * sensitivity * 1;
        }
    }

    public void RotateLeftSideMouse(int indexOfPiece, Vector3 mouseOffset)
    {
        if (indexOfPiece == 0 || indexOfPiece == 3 || indexOfPiece == 6)
        {
            rotation.z = (mouseOffset.x + mouseOffset.y) * sensitivity * 1;
        }
        else if (indexOfPiece == 2 || indexOfPiece == 5 || indexOfPiece == 8)
        {
            rotation.z = (mouseOffset.x + mouseOffset.y) * sensitivity * -1;
        }
    }

    public void RotateUpSideMouse(int indexOfPiece, Vector3 mouseOffset)
    {
        if (indexOfPiece == 0 || indexOfPiece == 3 || indexOfPiece == 6 || indexOfPiece == 1)
        {
            rotation.y = (mouseOffset.x + mouseOffset.y) * sensitivity * 1;
        }
        else if (indexOfPiece == 7 || indexOfPiece == 2 || indexOfPiece == 5 || indexOfPiece == 8)
        {
            rotation.y = (mouseOffset.x + mouseOffset.y) * sensitivity * -1;
        }
    }

    public void RotateDownSideMouse(int indexOfPiece, Vector3 mouseOffset)
    {
        if (indexOfPiece == 0 || indexOfPiece == 3 || indexOfPiece == 6 || indexOfPiece == 7)
        {
            rotation.y = (mouseOffset.x + mouseOffset.y) * sensitivity * -1;
        }
        else if (indexOfPiece == 1 || indexOfPiece == 2 || indexOfPiece == 5 || indexOfPiece == 8)
        {
            rotation.y = (mouseOffset.x + mouseOffset.y) * sensitivity * 1;
        }
    }
}
