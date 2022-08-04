using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class Sprint : MonoBehaviour
{

    // right hand
    List<UnityEngine.XR.InputDevice> rightHandedDevices = new List<UnityEngine.XR.InputDevice>();
    UnityEngine.XR.InputDevice rightHand;
    bool right_connected;

    // left hand 
    List<UnityEngine.XR.InputDevice> leftHandedDevices = new List<UnityEngine.XR.InputDevice>();
    UnityEngine.XR.InputDevice leftHand;
    bool left_connected;


    bool leftStick;
    bool rightStick;

    ContinuousMoveProviderBase moveBase;
    float temp_spd;

    // Start is called before the first frame update
    void Start()
    {
        moveBase = GameObject.Find("Locomotion System").GetComponent<ContinuousMoveProviderBase>();
        temp_spd = moveBase.moveSpeed;

    }

    // Update is called once per frame
    void Update()
    {
        if (establishConnection())
        {
            sprint();
        }
    }


    // if left stick pressed down, user "sprints"
    void sprint()
    {

        if ((leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out leftStick) && leftStick))
        {
            moveBase.moveSpeed = temp_spd * 3;
        }
        else
        {
            moveBase.moveSpeed = temp_spd;
        }
    }

    // connect to each remote
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
