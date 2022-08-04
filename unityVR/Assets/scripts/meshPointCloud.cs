using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using NReco.Csv; // special csv package
using UnityEditor;


public class meshPointCloud : MonoBehaviour
{

    // Mesh object to display point cloud on initializaiton
    Mesh mesh;

    // List of all the frames so costly re-rendering is avoided. Sorted list based of frame ID
    SortedList<int, List<List<float>>> position_data = new SortedList<int, List<List<float>>>();

    // Queue to hold and then access all csvs being generated
    Queue<string> fileQueue = new Queue<string>();

    // indice value for position_data
    public int frame_number = -1; // needs to -1 at first bc it is pre-incremented when displaying. see threadT function

    // frames per second
    public int FPS = 10;
    public bool displayText = false;

    // thread that enables adjusting FPS
    private Thread _t1;

    // thread handles the rendering from the csvs
    private Thread[] _renderThread;

    // thread that signals to other threads when application is quit. Stops the other threads
    static Thread flagThread;


    // Pre-generated colors
    Color32 blue32 = Color.blue;
    Color32 red32 = Color.red;

    // bloocking queues: solve producer consumer problem 
    BlockingCollection<List<Vector3>> vertexQueue = new BlockingCollection<List<Vector3>>();
    BlockingCollection<List<int>> indexQueue = new BlockingCollection<List<int>>();
    BlockingCollection<List<Color32>> colorQueue = new BlockingCollection<List<Color32>>(); // blocking collection queue method


    /*********************************** Following is for VR inputs initialization ****************************************************/

    // establish connection function
    bool left_connected;
    bool right_connected;

    // pause function
    bool leftGrip;
    bool rightGrip;

    // updown function
    bool A_Button;
    bool B_Button;
    bool yButton;

    // speed control function
    bool leftTrigger;
    bool rightTrigger;
    bool slowdown;
    bool speed_up;
    int trigger_count = 0;
    int hold_down = 0;
    bool faster;

    // replay function 
    bool rewind = false;
    bool keep_current = true;
    bool go_back = false;
    bool forward = false;
    bool keep_current2 = true;
    bool go_forward = false;

    // color control function
    bool rightStick;
    bool keep_color = true;
    bool switch_color = false;
    int color_index = 0;
    bool color_change = false;
    bool change_paused_color = false;

    // quit function
    bool quitButton;

    //establish hand objects 
    List<UnityEngine.XR.InputDevice> rightHandedDevices = new List<UnityEngine.XR.InputDevice>();
    UnityEngine.XR.InputDevice rightHand;

    List<UnityEngine.XR.InputDevice> leftHandedDevices = new List<UnityEngine.XR.InputDevice>();
    UnityEngine.XR.InputDevice leftHand;

    // awake is the first thing called when application is played
    void Awake()
    {

        flagThread = new Thread(new ThreadStart(flag));
        flagThread.Start();
        string folder_path = @"./DemoScene/"; // DEFAULT FOLDER. SET TO WHEREVER THE CSVS ARE HOUSED. 
        mesh = GetComponent<MeshFilter>().mesh;

        getFiles(folder_path);

        // starts a thread that will get the data from csvs for visualization
        Task.Run(() => renderData());
    }

    void Start()
    {
        // thread that controls speed and organizes the frames in unity terms for visualization
        _t1 = new Thread(new ThreadStart(threadT));
        _t1.Start();
    }

    // Update is called once per frame (~90 FPS)
    void Update()
    {
        // overlapping checks to make sure nothing is incompleted
        if (vertexQueue.Count > 0 && colorQueue.Count > 0 && indexQueue.Count > 0)
        {

            mesh.Clear();

            mesh.vertices = vertexQueue.Take().ToArray();
            mesh.SetColors(colorQueue.Take());
            mesh.SetIndices(indexQueue.Take(), MeshTopology.Points, 0);

        }

        // get all vr inputs if connected
        if (establishConnection())
        {

            if (quit())
            {
                flagThread.Abort();
                Application.Quit();
            }

            if (Pause())
            {
                if (color_change)
                {
                    change_paused_color = true;
                    color_change = false;
                }
                replay(); // rewinding becomes available when paused
            }
            else
            {
                speedControl();
            }

            colorControl();
        }
    }

    // for the flag thread. keeps thread always alive
    void flag()
    {
        while (true);
    }

    void threadT()
    {
        float frame_per_second = 10;
        while (flagThread.IsAlive) // if the flagThread symbolizing the mainthread is killed, then the other remaining threads are ensured to be killed as well 
        {
            frame_per_second = (float)FPS;
            if ((frame_per_second != 0 || go_back || go_forward || change_paused_color) && position_data.Count() > 50)
            {
                if (go_back) // go back and go foward display next of last frame when paused
                {


                    frame_number--;

                    if (frame_number < 0)
                    {
                        frame_number = position_data.Count() - 1; 
                    }

                    createMesh(position_data.Values[frame_number]);
                    go_back = false;
                }
                else if (go_forward)
                {
                    frame_number++;

                    if (frame_number == position_data.Count())
                    {
                        frame_number = 0;
                    }

                    createMesh(position_data.Values[frame_number]);
                    go_forward = false;
                }
                else if (change_paused_color)
                {
                    createMesh(position_data.Values[frame_number]);
                    change_paused_color = false;
                }
                else // handles normal rendering when not paused
                {
                    int speed = Math.Abs((int)Math.Round(1 / frame_per_second * 1000));

                    if(frame_per_second > 0) // when fps is pos.
                    {
                        frame_number++;
                        if (frame_number == (position_data.Count() - 1))
                        {
                            frame_number = 0;
                        }
                    }
                    else
                    {
                        frame_number--; // when fps is neg
                        if (frame_number < 0) // look for wrapping
                        {
                            frame_number = position_data.Count() - 1;
                        }
                    }


                    createMesh(position_data.Values[frame_number]);

                    System.Threading.Thread.Sleep(speed);
                }
            }
        }
        Debug.Log("Play thread stopped");
    }
   

    // looks for VR left & right hand controllers
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

 // color control listens for input and changes how the color of the displayed points
    void colorControl() 
    {

        if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out rightStick) && rightStick)
        {
            switch_color = true; // flip flop needed to get only one input instance. Inputs signals are received continuously
            keep_color = false;
        }
        else
        {
            keep_color = true;
        }

        if (switch_color && keep_color)
        {
            color_index++; 
            if (color_index == 3) // if the max # of colors reached in index reset to 0
            {
                color_index = 0;
            }
            color_change = true;
            switch_color = false;
        }
    }

    // quit simulation and ends all threads if quit (X) is pressed
    bool quit()
    {
        if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out quitButton) && quitButton)
        {
            return true;
        }
        return false;
    }

    // stores FPS when application is paused
    int tempFPS;
    // if left grip is pressed. FPS is set to 0 => scene paused.
    // if right grip is pressed, FPS is set to its last value => scene resumed or continues
    bool Pause()
    {
        if (FPS != 0)
        {
            tempFPS = FPS;
        }

        if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out leftGrip) && leftGrip)
        {
            FPS = 0;
        }

        if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out rightGrip) && rightGrip)
        {
            FPS = tempFPS;
        }

        if (FPS == 0)
        {
            return true;
        }

        return false;
    }

    void replay() // enables going back and forward by one frame when paused
    {
        if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out leftTrigger) && leftTrigger) // go back checks and swithces
        {
            rewind = true;
            keep_current = false;
        }
        else
        {
            keep_current = true;
        }

        if (keep_current && rewind)
        {
            rewind = false;
            go_back = true;
        }

        if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out rightTrigger) && rightTrigger) // go forward checks and switches
        {
            forward = true;
            keep_current2 = false;
        }
        else
        {
            keep_current2 = true;
        }

        if (keep_current2 && forward)
        {
            forward = false;
            go_forward = true;
        }

        if ((rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out rightTrigger) && rightTrigger) && (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out yButton) && yButton)){
            displayText = true;
        }
        else
        {
            displayText = false;
        }

    }

    // speed up/slow down frame rate with triggers
    void speedControl()
    {

        if (leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out leftTrigger) && leftTrigger)
        {

            // if coming from speed up state, reset hold count => not accelerating
            if (faster)
            {
                trigger_count = 0;
                hold_down = 0;
                faster = false;
            }

            trigger_count++; // triggers are received at same rate as VR framerate (~90 triggers per second)

            // change in frame rate = (number of triggers received / half the ideal FPS rate)
            int change = (int)Math.Round(trigger_count / 45.0);

            if ((change > hold_down) && (change + FPS > -90)) // 90 is the headset ideal FPS. Should not exceed it
            {
                if ((FPS -= change) == 0)
                {
                    FPS = -1;
                }
                else
                {
                    FPS -= change;
                }
                FPS -= change;
                hold_down++;
            }
        }
        else if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out rightTrigger) && rightTrigger)
        {
            // if coming from slow state
            if (!faster)
            {
                trigger_count = 0;
                hold_down = 0;
                faster = true;
            }

            trigger_count++;
            int change = (int)Math.Round(trigger_count / 45.0);

            if ((change > hold_down) && (change + FPS < 90)) // 90 is the headset ideal FPS. Should not exceed it
            {
                if ((FPS += change) == 0)
                {
                    FPS = 1;
                }
                else
                {
                    FPS += change;
                }

                FPS += change;
                hold_down++;
            }
        }
        else // reset counts for when nothing is going on 
        {
            trigger_count = 0;
            hold_down = 0;
        }
    }   



    // creates the actual mesh visualization each time
    float x_max = 0;
    void createMesh(List<List<float>> frame)
    {
        int counter = 0;
        List<int> indices = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Color32> colors = new List<Color32>();

        foreach (List<float> point in frame)
        {

            vertices.Add(new Vector3(point[0], point[1], point[2]));

            indices.Add(counter);

            float r = 0;
            float g = 0;
            float b = 0;

            if (point[0] > x_max) // find max range of pcap THIS MAY HAVE TO BE MODIFIED FOR SITUATIONS WHERE SENSOR MOVES AND SUCH. INTERESTING...
            {
                x_max = point[0];
            }

            if (color_index == 0) // 0 => color by intensity (thermal)
            {
                r = GetRedValue(point[3]);
                g = GetGreenValue(point[3]);
                b = GetBlueValue(point[3]);
                colors.Add(new Color(r, g, b, 1));
            }
            else if (color_index == 1) // 1 => all green
            {
                r = 0;
                g = 1;
                b = 0;
                colors.Add(new Color(r, g, b, 1));
            }
            else if (color_index == 2) //  range off of x
            {
                float x = point[0] / x_max;
                Color lerpedColor = Color.Lerp(blue32, red32, x);
                colors.Add(lerpedColor);
            }

            counter++;
        }

        colorQueue.Add(colors);
        indexQueue.Add(indices);
        vertexQueue.Add(vertices);
    }

    // extracts frame data from a provided csv
    List<List<float>> getData(string data_path)
    {
        List<List<float>> data = new List<List<float>>(); // data array is a single frame. A list of points

        //  read each csv
        using (var reader = new StreamReader(data_path))
        {
            // throw away first line with headers
            var getfirst = reader.ReadLine();
            var csvReader = new CsvReader(reader, ","); // function from the plug-in; more efficient

            // read each line and get position data
            while (csvReader.Read())
            {
                List<float> vertex = new List<float>(); // holds necessary information regarding single point

                // convert string to float
                float x = float.Parse(csvReader[2], CultureInfo.InvariantCulture.NumberFormat);
                float y = float.Parse(csvReader[3], CultureInfo.InvariantCulture.NumberFormat);
                float z = float.Parse(csvReader[4], CultureInfo.InvariantCulture.NumberFormat);

                float intensity = float.Parse(csvReader[11], CultureInfo.InvariantCulture.NumberFormat);
                float mapping = intensity / 65535.0f; // convert intesity values to range from 0 to 1
                
                vertex.Add(x); 
                vertex.Add(y);
                vertex.Add(z);
                vertex.Add(mapping);

                data.Add(vertex); 
            }
            reader.Close();
        }
        return data;
    }

    // linear step and following functions create the intensity color mapping. From this stackoverflow: https://stackoverflow.com/questions/15868234/map-a-value-0-0-1-0-to-color-gain
    private float LinearStep(float x, float start, float width = 0.2f)
    {
        if (x < start)
            return 0f;
        else if (x > start + width)
            return 1;
        else
            return (x - start) / width;
    }

    private float GetRedValue(float intensity)
    {
        return LinearStep(intensity, 0.2f);
    }

    private float GetGreenValue(float intensity)
    {
        return LinearStep(intensity, 0.6f);
    }

    private float GetBlueValue(float intensity)
    {
        return LinearStep(intensity, 0f)
        - LinearStep(intensity, 0.4f)
        + LinearStep(intensity, 0.8f);
    }

    // get the frame ID from file name MAY NEED TO CHANGE TO GET FRAME ID FROM FILE ITSELF
    int getFrameID(string file)
    {
        int frameID = 0;
        //  read each csv
        using (var reader = new StreamReader(file))
        {
            // throw away first line with headers
            var getfirst = reader.ReadLine();
            var csvReader = new CsvReader(reader, ","); // function from the plug-in; more efficient

            // read each line and get position data
            csvReader.Read();
            frameID = Int32.Parse(csvReader[1]);
            reader.Close();
        }

        return frameID;
    }

    // get all frame data and organizes in list of frames. Utilizes threading to maximize speed/efficiency
    // approximately renders at 16-18 fps
    void renderData()
    {
        Debug.Log("start");

        while (fileQueue.Count > 0)
        {
            // implement threads to go through all files in quickest manner possible
            // to prevent deadlocking/competition over resources threads are created in a sequential manner
            for (int i = 0; ((i < 20) && fileQueue.Count > 0); i++)
            {
                string file = fileQueue.Dequeue();
                int ID = getFrameID(file);
                position_data.Add(ID, Task.Factory.StartNew(() => getData(file)).Result); // create task to be picked up by thread and completed
            }
        }

        Debug.Log("finish");

    }

    // seraches through provided folder and adds all csv files to be processed and displayed to queue
    void getFiles(string folder_path)
    {
        foreach (string file in Directory.EnumerateFiles(folder_path, "*.*", SearchOption.AllDirectories))
        {
            if (!(fileQueue.Contains(file)))
            {
                fileQueue.Enqueue(file);
            }
        }
    }
}
