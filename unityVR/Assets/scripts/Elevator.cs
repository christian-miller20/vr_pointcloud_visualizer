using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class Elevator : MonoBehaviour
{

    // right hand 
    List<UnityEngine.XR.InputDevice> rightHandedDevices = new List<UnityEngine.XR.InputDevice>();
    UnityEngine.XR.InputDevice rightHand;
    bool right_connected;

    // left hand 
    List<UnityEngine.XR.InputDevice> leftHandedDevices = new List<UnityEngine.XR.InputDevice>();
    UnityEngine.XR.InputDevice leftHand;
    bool left_connected;

    bool A_Button;
    bool B_Button;

    public GameObject origin;
    public float vertical_speed = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        //origin = GameObject.Find("XR");
        Debug.Log("Connected to XR origin");
    }

    // Update is called once per frame
    void Update()
    {
        if (establishConnection())
        {
            UpDown();
        }
        
    }

    void UpDown()
    {
        if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out B_Button) && B_Button)
        {
            Vector3 moveup = new Vector3(0, vertical_speed, 0);
            origin.transform.position += moveup;
        }
        if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out A_Button) && A_Button)
        {
            Vector3 movedown = new Vector3(0, -vertical_speed, 0);
            origin.transform.position += movedown;
        }
    }

    bool establishConnection()
    {
        // check if left/right connection exist. If so establish connection
        if (leftHandedDevices.Count == 0)
        {
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, leftHandedDevices);
        }

        if (rightHandedDevices.Count == 0)
        {
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandedDevices);
        }

        // see if a connection is established
        if ((rightHandedDevices.Count) == 1 && (leftHandedDevices.Count == 1))
        {
            left_connected = true;
            right_connected = true;
        }

        // Access devices list => set left and right to respective device
        if (left_connected && right_connected)
        {
            leftHand = leftHandedDevices[0];
            rightHand = rightHandedDevices[0];
            return true;
        }

        return false;

    }


}
