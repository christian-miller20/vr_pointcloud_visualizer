using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class showFPS : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _FPS;

    meshPointCloud meshpointcloud;

    // Start is called before the first frame update
    void Awake()
    {
        meshpointcloud = GameObject.Find("meshPointCloud").GetComponent<meshPointCloud>();
    }

    // Update is called once per frame
    void Update()
    {
        if (meshpointcloud.FPS != 0 && !meshpointcloud.displayText)
        {
            _FPS.text = "FPS: " + meshpointcloud.FPS.ToString();
        }
        else if (meshpointcloud.displayText)
        {
            _FPS.text = "Created by Christian Miller 2022. ND. Go Irish!";
        }
        else
        {
            _FPS.text = "PAUSED";
        }
    }
}
