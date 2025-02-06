using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WholeCubeRotation : MonoBehaviour
{
    Vector2 initialPressPos; //Position where the swipe started
    Vector2 finalPressPos; //Position where the swipe ended 
    Vector2 currentSwipe; //Direction and magnitude of the swipes

    Vector3 initialMousePos; //Position when the mouse button was held down
    Vector3 mouseDelta; //Difference between final mouse position and iniial mouse posiion after the mouse buton was held

    public GameObject target;

    float speed = 200f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Swipe();
        Drag();
        Arrows();
    }

    void Drag()
    {
        if (Input.GetMouseButton(1))
        {
            //while the mouse button is held down, the cube will provide visual feedback of moving around central axis
            mouseDelta = Input.mousePosition - initialMousePos;
            mouseDelta *= 0.1f; //reduction of rotation speed

            //Creates roation (Quaternion.Euler) relative to the cube's existing orientation(* transform.rotation), instead of resetting it
            transform.rotation = Quaternion.Euler(mouseDelta.y, -mouseDelta.x, 0) * transform.rotation; 
        }
        else
        {
            //automatically move to the target position:
            if (transform.rotation != target.transform.rotation)
            {
                float step = speed * Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target.transform.rotation, step); 
            }
        }
        initialMousePos = Input.mousePosition;
    }

    void Swipe()
    {
        //If the mouse button 1(Right mouse button) was pressed 
        if (Input.GetMouseButtonDown(1)) 
        {
            initialPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y); //get the 2D position of the initial press
        }

        //If the mouse button 1 was released
        if (Input.GetMouseButtonUp(1)) 
        {
            finalPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y); //get the 2D position of the final press
            
            //creates a current vector from the iniital press and final press positions
            currentSwipe = new Vector2(finalPressPos.x - initialPressPos.x, finalPressPos.y - initialPressPos.y);
            currentSwipe.Normalize(); //normalise the 2d vector
            
            if (LeftSwipe(currentSwipe)) 
            {
                RotateLeft(target); 
            }
            else if (RightSwipe(currentSwipe)) 
            {
                RotateRight(target); 
            }
            else if (UpLeftSwipe(currentSwipe)) 
            {
                RotateUpLeft(target); 
            }
            else if (UpRightSwipe(currentSwipe)) 
            {
                RotateUpRight(target); 
            }
            else if (DownLeftSwipe(currentSwipe)) 
            {
                RotateDownLeft(target);
            }
            else if (DownRightSwipe(currentSwipe)) 
            {
                RotateDownRight(target);
            }
        }
    }

    void Arrows()
    {
        if (Input.GetKey(KeyCode.UpArrow)) //If Up arrow and Left or Right arrow were pressed
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                RotateUpLeft(target);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                RotateUpRight(target);
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow)) //If Down arrow and Left or Right arrow were pressed
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                RotateDownLeft(target);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                RotateDownRight(target);
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) //If Left arrow were pressed
        {
            RotateLeft(target);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) //If Right arrow were pressed
        {
            RotateRight(target);
        }
    }


    //Swipe detection methods
    bool LeftSwipe(Vector2 swipe) //swipe to the left
    {
        return swipe.x < 0 && swipe.y > -0.2f && swipe.y < 0.2f;
    }
    bool RightSwipe(Vector2 swipe) //swipe to the right
    {
        return swipe.x > 0 && swipe.y > -0.2f && swipe.y < 0.2f;
    }
    bool UpLeftSwipe(Vector2 swipe) //swipe to the top relatively to the left(Front) side
    {
        return swipe.y > 0 && swipe.x < 0f;
    }
    bool UpRightSwipe(Vector2 swipe) //swipe to the top relatively to the right side
    {
        return swipe.y > 0 && swipe.x > 0f;
    }
    bool DownLeftSwipe(Vector2 swipe) //swipe to the bottom relatively to the left(Front) side
    {
        return swipe.y < 0 && swipe.x < 0f;
    }
    bool DownRightSwipe(Vector2 swipe) //swipe to the bottom relatively to the right side
    {
        return swipe.y < 0 && swipe.x > 0f;
    }
 
    //Rotation methods
    void RotateLeft(GameObject target) //rotation left
    {
        target.transform.Rotate(0, 90, 0, Space.World);
    }
    void RotateRight(GameObject target) //rotation right
    {
        target.transform.Rotate(0, -90, 0, Space.World);
    }
    void RotateUpLeft(GameObject target) //rotation up relatively to the left(Front) side
    {
        target.transform.Rotate(90, 0, 0, Space.World);
    }
    void RotateUpRight(GameObject target) //rotation up relatively to the right side
    {
        target.transform.Rotate(0, 0, -90, Space.World);
    }
    void RotateDownLeft(GameObject target) //rotation down relatively to the left(Front) side
    {
        target.transform.Rotate(-90, 0, 0, Space.World);
    }
    void RotateDownRight(GameObject target) //rotation down relatively to the right side
    {
        target.transform.Rotate(0, 0, 90, Space.World);
    }
}
