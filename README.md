# unity-cv-data-gen

This repository contains the Unity project used to generate data for CV model training.

## Important Info
Unity Version: 2023.1.16 (any version of 2023.1.x should work)
Perhaps someone can try loading this in a later version and see if it works.

[Unity Hub](https://unity.com/download)

## Basic Usage
1. Clone this repository
2. Open Unity Hub, and install a version of Unity if not done already.
3. On the top, click add, then select the root directory of this repository on your local device.
4. Once the project is opened, ensure you are in the *Data_Gen* scene (shown in the hierarchy). If not, navigate to Assets/Scenes in the project tab, and open *Data_Gen*
5. Click the button on top to run the program to generate data
6. Access the images/annotations by accessing *RotationPoint/Main Camera* in the hierarchy, then go to the *Percpetion Camera* object, scroll down and click "Show Folder" on the Latest Generated Dataset

Accessing the annotations is a bit of a pain (currently we use an external python script to organize everything.

## Important Objects/Scripts
Scripts are found in Assets/Scripts.

**Simulation Scenario:** This object is responsible for starting the process of spawning the objects. This object relies on WaterObjectRandomizer.cs, and its parameters can be modified within the object.
**WaterObjectRandomizer.cs**: The main script associated with placing the objects and orienting the camera. 
**Main Camera** (in RotationPoint): Visualizes boudning boxes and saves image data. 

## Known Issues
- Initial UI before starting generation doesn't work

