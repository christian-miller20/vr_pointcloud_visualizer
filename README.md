# VR Visualizer
### By Christian Miller with AEYE.Inc

### Overview: 
The VR Visualizer editable utilizes Unity Game Engine in order to render saved csv LiDAR point cloud data in a VR environment. 
Once inside, one can manuever throughout and manipulate the point cloud to get unique views and insights.
NOTE: the two folders resemble an editable Unity project and an already built version of this project

### Requirements:
1. VR Headset connected to PC by steamVR
2. High performance PC (nvidia GPU 3000+ or equivalent) 
3. nreco.csv installed: https://github.com/nreco/csv
4. Unity (only needed if using editable version)


### Setup:

##### FOR BUILT VERSION:
1. Download/clone repo to computer. 
2. Double click the trial4_VRexplore application with the unity logo
3. Put on connected headset. Should load into scene


##### IF USING EDITABLE VERSION:
1. Download/Clone repo to computer
2. Use Unity to open the project

##### FOR CHANGING THE PLAYED BACK SCENE:
Within both folders, there is a folder called demoScene. This contains all the csvs to be played. To change what is played simply update what is in these folders


## How it Works:
There are three major components: Data extraction, Unity, and VR inputs. In order to promote the immediate and realtime display of point cloud data in the VR headset these happen
simulataneously.

### Unity
A quick talk about Unity is necessary to understand how everything comes together. When play is pressed, Unity calls the awake() and start() functions automatically in any
scripts. Thus, a few major components and data structures such as the mesh object and file queue are established/initialized before the viewer runs. Next, Unity operates by calling the update() function
once per every frame. For example, the Vive HTC Pro 3 headset has an ideal frame rate of 90 frames per second, so update() would be called 90 times. If there are
any major processing events or holdups on the main thread, then the frame rate drops correspondingly as Unity must go through and finish any code execution before
calling the next update(). In order to avoid any backups, multi-threading must be utilized with seperate CPUs providing the rendered data for Unity to display. 

### Data Extraction
Now that the limitations and defining principles of Unity are better understood, the data extraction can be explored. When the user presses play, a file queue is created
and gets the paths to the csvs within the folder. Once finished, a new thread is created to get all the csv data and organize it into a large sorted list of frames
data structure which allows for both sequential frame organization and avoids any costly re-rendering. This thread works by creating up to as many more threads as there
are CPUs free to go through each csv and parse for the essential data such as coordinates and intensity utilizing the specialized csv reader required. In the meantime,
all the newly extracted data is placed in a frame data structure which is then added to the sorted list of frames by frame number. The data extraction component speed
is relative to the frame data. Nonetheless, for a normal scan pattern and fire rate (approx 16000 points per frame), the render rate is about 16-18 FPS. 

### Unity and Data Extraction Together: The Display Thread
In combination, the data extraciton and Unity work together by operating the non-competing threads which ensure fast performance and no lag within the headset.
The Unity component will create and ensure fluid VR locomotion and head movements by checking these each time update() is called while the data extraction continues to make the frames.
However, there is one more component which ties it all together: the display thread. 
This thread will open the list of frames data structure and transform each points data in a Unity useable format.
After the display thread sets the colors, indices, and vertices data, a producer consumer design sends signals saying the frame is ready. 
update() receives the signals and will take the set the mesh to the organized data. 

### The VR Component
The last feature is how the VR system all comes together to work. Within all the scripts, connection is established to the VR controllers and inputs are grabbed
and processed accordingly. For more information, further examine the scripts, and it is recommended to look at the Unity Documentation regarding VR inputs.

### Changing Point Display and Size
The points are displayed as triangles which are rendered each frame to always face the user. The way to change the size of these triangles is
to go into the materials folder, click on movingTris, and, within the inspector tab, change the sphere radius size box. In order to change the actual point
geometry/display, a lot more is required. Specifically, a new shader would have to be created and then applied to the movingTris material. To do this, 
go into the shaders folder and follow any tutorials/steps to create a shader and apply it to a material.

### How to Develop Further
The Assets folder is where everything modifiable is stored. The main driver behind the renderer is within the scripts folder and is called
meshPointCloud.cs. The other scripts control the text displays and some of the locomotion components of pertaining to VR. The other folders
hold many of the scene objects, graphical settings such as how the points are displayed (shaders/materials), and VR presets which enable the locomotion system.


#### Additonal Questions and Concerns
If you have any questions or need more information, feel free to reach me at: xmiller32878@gmail.com or over Linkedin
