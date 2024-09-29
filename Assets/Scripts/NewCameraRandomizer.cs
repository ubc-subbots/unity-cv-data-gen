using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Recorder;
using TMPro;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor;
using UnityEngine.UI;

public class NewCameraRandomizer : MonoBehaviour
{
    [SerializeField] FileWrite fileManager;
    [SerializeField] FixedLengthScenario simScenario;
    [SerializeField] DepthRandomizer depthRandomizer;
    [SerializeField] TMP_Dropdown objectDropdown;
    [SerializeField] Transform objectSpawnPoint;
    [SerializeField] TMP_InputField iterationCount;
    [SerializeField] GameObject objectCheckboxContainer;
    [SerializeField] Slider depthSlider;
    [SerializeField] Slider oddSlider;
    [SerializeField] Slider heightSlider;
    [SerializeField] FileWrite fileWriter;
    [SerializeField] GameObject boundingBoxUI;
    [SerializeField] GameObject floor;
    [SerializeField] GameObject walls;

    [HideInInspector] public Dataset dataset = new Dataset();

    // Gate: maxX = 1.6, derivSlopeX = 1.13, maxY = 1, derivSlopeY = 0.5

    Vector3 initialObjectPos;
    Vector3 initialObjectRot;
    int frame = 0;
    Camera mainCamera;
    bool generationStarted = false;
    GameObject detectionObject;
    Vector3 objectSize;
    Vector2 depthRange;
    Vector2 pitchRange;
    Vector2 yawRange;
    string objectType;
    int frameCount = 1000;
    bool constantHeight = false;
    bool floorEnabled = false;
    bool wallsEnabled = false;
    float floorSpawnChance = 0;
    float floorHeightDeviation = 0;
    int waterDepthCst = 1;
    bool[] objectsToSpawn = new bool[9];

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        depthRandomizer.enabled = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (generationStarted && frame <= frameCount)
        {
            // Reset the object's position
            detectionObject.transform.position = initialObjectPos;
            detectionObject.transform.eulerAngles = initialObjectRot;
    
            // Randomize the cemera's position/rotation
            float cameraDistance = Random.Range(depthRange.x, depthRange.y);
            float cameraRelativePitch = Random.Range(pitchRange.x, pitchRange.y);
            float cameraRelativeYaw = Random.Range(yawRange.x, yawRange.y);
            transform.GetChild(0).localPosition = Vector3.back * cameraDistance;
            transform.localEulerAngles = new Vector3(cameraRelativePitch, cameraRelativeYaw, 0);

            // Randomize the object's position
            float safeMarginX = objectSize.x / (mainCamera.aspect * 2 * Mathf.Tan(mainCamera.fieldOfView / 2 * Mathf.Deg2Rad) * cameraDistance);
            float safeMarginY = objectSize.y / (2 * Mathf.Tan(mainCamera.fieldOfView / 2 * Mathf.Deg2Rad) * cameraDistance);
            float randomX = Random.Range(0.05f + safeMarginX, 0.95f - safeMarginX);
            float randomY = Random.Range(0.15f + safeMarginY, 0.85f - safeMarginY);
            Vector3 viewportPoint = new Vector3(randomX, randomY, cameraDistance);
            Vector3 worldPoint = transform.GetChild(0).GetComponent<Camera>().ViewportToWorldPoint(viewportPoint);
            detectionObject.transform.position = worldPoint + initialObjectPos;

            // Write the final relative positions/rotations of the camera
            Vector3 relativeCameraPosition = transform.GetChild(0).position - detectionObject.transform.position;
            Vector3 relativeCameraRotation = transform.GetChild(0).eulerAngles;

            // Write the pos/rot into a JSON file (to-do)
            ObjectData newObjectData = new ObjectData
            {
                image_name = objectType + "_" + frame, // Set this appropriately
                object_type = objectType,
                position = new Position
                {
                    x = relativeCameraPosition.x,
                    y = relativeCameraPosition.y,
                    z = relativeCameraPosition.z
                },
                rotation = new Rotation
                {
                    roll = relativeCameraRotation.x > 180 ? relativeCameraRotation.x - 360 : relativeCameraRotation.x, // Assuming these are directly assignable
                    yaw = relativeCameraRotation.y > 180 ? relativeCameraRotation.y - 360 : relativeCameraRotation.y,
                    pitch = relativeCameraRotation.z
                }
            };

            // Add or update the object data
            dataset.objects.Add(newObjectData);

            // If constant water height is enabled, set the water height to just above the highest object
            if (constantHeight)
            {
                depthRandomizer.gameObject.transform.position = Vector3.up * (Mathf.Max(transform.GetChild(0).position.y, 
                    detectionObject.transform.position.y + objectSize.y/2) + waterDepthCst);
            }

            // If floor enabled, then position it onto the lowest object.
            if (floorEnabled)
            {
                if (floorSpawnChance != 0 && Random.Range(0, 1) <= floorSpawnChance)
                {
                    floor.SetActive(true);
                    floor.transform.position = Vector3.up * (Mathf.Min(transform.GetChild(0).position.y, detectionObject.transform.position.y - objectSize.y / 2) 
                        - Random.Range(0,floorHeightDeviation));
                }
                else
                {
                    floor.SetActive(false);
                }
                    
            }

            // Bounding box data for the YOLOv5 model
            float[] boundingBoxData = GetBoundingBoxData();

            //fileWriter.WriteToFile(ObjectToIndex() + " " + boundingBoxData[0] + " " + boundingBoxData[1] + " " + boundingBoxData[2] + " " + boundingBoxData[3]);
            fileWriter.WriteToNewFile(ObjectToIndex() + " " + boundingBoxData[0] + " " + boundingBoxData[1] + " " + boundingBoxData[2] + " " + boundingBoxData[3], frame);

            boundingBoxUI.transform.GetChild(0).GetComponent<RectTransform>().position = new Vector3((boundingBoxData[0] - boundingBoxData[2] / 2) * 1920, (boundingBoxData[1] - boundingBoxData[3] / 2) * 1080);
            boundingBoxUI.transform.GetChild(1).GetComponent<RectTransform>().position = new Vector3((boundingBoxData[0] + boundingBoxData[2] / 2) * 1920, (boundingBoxData[1] - boundingBoxData[3] / 2) * 1080);
            boundingBoxUI.transform.GetChild(2).GetComponent<RectTransform>().position = new Vector3((boundingBoxData[0] - boundingBoxData[2] / 2) * 1920, (boundingBoxData[1] + boundingBoxData[3] / 2) * 1080);
            boundingBoxUI.transform.GetChild(3).GetComponent<RectTransform>().position = new Vector3((boundingBoxData[0] + boundingBoxData[2] / 2) * 1920, (boundingBoxData[1] + boundingBoxData[3] / 2) * 1080);

            // Optionally, write to JSON file here or do it at another time
            //WriteToJsonFile(objectType+"_dataset_test.json", JsonUtility.ToJson(dataset, true));
            frame += 1;

        }
    }

    int ObjectToIndex()
    {
        for(int i = 0; i < objectDropdown.options.Count; i++)
        {
            if (objectType.Equals(objectDropdown.options[i].text)) return i;
        }
        return -1;
    }

    float[] GetBoundingBoxData()
    {        
        Vector3[] worldSpaceCorners = new Vector3[8];
        Vector3 objectCenter = detectionObject.transform.position;

        // Half sizes
        float halfX = objectSize.x * 0.5f;
        float halfY = objectSize.y * 0.5f;
        float halfZ = objectSize.z * 0.5f;

        // Calculate each corner position relative to the object's center
        worldSpaceCorners[0] = objectCenter + new Vector3(-halfX, -halfY, -halfZ);
        worldSpaceCorners[1] = objectCenter + new Vector3(halfX, -halfY, -halfZ);
        worldSpaceCorners[2] = objectCenter + new Vector3(halfX, -halfY, halfZ);
        worldSpaceCorners[3] = objectCenter + new Vector3(-halfX, -halfY, halfZ);
        worldSpaceCorners[4] = objectCenter + new Vector3(-halfX, halfY, -halfZ);
        worldSpaceCorners[5] = objectCenter + new Vector3(halfX, halfY, -halfZ);
        worldSpaceCorners[6] = objectCenter + new Vector3(halfX, halfY, halfZ);
        worldSpaceCorners[7] = objectCenter + new Vector3(-halfX, halfY, halfZ);

        Vector3[] cameraSpaceCorners = new Vector3[8];

        for (int i = 0; i < worldSpaceCorners.Length; i++)
        {
            // Transform the world space corners to camera space
            cameraSpaceCorners[i] = mainCamera.WorldToViewportPoint(worldSpaceCorners[i]);
        }

        // Determine the four corners of the space
        float minX, minY, maxX, maxY;

        float[] corners = new float[4];

        float least = 1;
        float most = 0;

        foreach (Vector3 corner in cameraSpaceCorners)
        {
            if (corner.x < least) least = corner.x;
            if (corner.x > most) most = corner.x;
        }
        minX = least;
        maxX = most;

        least = 1;
        most = 0;
        foreach (Vector3 corner in cameraSpaceCorners)
        {
            if (corner.y < least) least = corner.y;
            if (corner.y > most) most = corner.y;
        }
        minY = least;
        maxY = most;

        // Assign values for "corners"
        corners[0] = (minX + maxX) / 2;
        corners[1] = (minY + maxY) / 2;
        corners[2] = maxX - minX;
        corners[3] = maxY - minY;

        return corners;
    }

    public void ToggleConstantHeight()
    {
        constantHeight = !constantHeight;
    }

    public void ToggleFloor()
    {
        floorEnabled = !floorEnabled;
    }

    public void ToggleWall()
    {
        wallsEnabled = !wallsEnabled;
    }

    public void SliderUpdate()
    {
        floorSpawnChance = oddSlider.value;
        floorHeightDeviation = heightSlider.value;
        waterDepthCst = (int) depthSlider.value;
        oddSlider.transform.GetChild(0).GetComponent<TMP_Text>().text = (floorSpawnChance*100).ToString("F0") + "%";
        heightSlider.transform.GetChild(0).GetComponent<TMP_Text>().text = floorHeightDeviation.ToString("F1") + "m";
        depthSlider.transform.GetChild(0).GetComponent<TMP_Text>().text = waterDepthCst.ToString() + "m";
    }

    void WriteToJsonFile(string filename, string jsonData)
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        using (StreamWriter stream = new StreamWriter(path))
        {
            stream.Write(jsonData);
        }
    }

    public void StartGeneration()
    {
        simScenario.enabled = true;
        mainCamera.GetComponent<PerceptionCamera>().enabled = true;
        depthRandomizer.enabled = true;
        GameObject.Find("Canvas").SetActive(false);

        detectionObject = objectSpawnPoint.GetChild(objectDropdown.value).gameObject;
        detectionObject.SetActive(true);
        initialObjectPos = detectionObject.transform.position;
        initialObjectRot = detectionObject.transform.eulerAngles;

        objectSize = detectionObject.GetComponent<RandomizerSettings>().boundSize;
        //depthRange = detectionObject.GetComponent<RandomizerSettings>().depthRange;
        //pitchRange = detectionObject.GetComponent<RandomizerSettings>().pitchRange;
        //yawRange = detectionObject.GetComponent<RandomizerSettings>().yawRange;
        objectType = detectionObject.name;

        try
        {
            simScenario.constants.iterationCount = int.Parse(iterationCount.text);
            frameCount = int.Parse(iterationCount.text);
        }
        catch
        {
            simScenario.constants.iterationCount = 1000;
            frameCount = 1000;
        }

        //fileWriter.InitializeFile(objectType);
        fileWriter.InitializeSoloFile();
        generationStarted = true;

        // Debug
        boundingBoxUI.SetActive(true);

        depthRandomizer.enabled = !constantHeight;
        floor.SetActive(floorEnabled);
        walls.SetActive(wallsEnabled);
    }

    [System.Serializable]
    public class ObjectData
    {
        public string image_name;
        public string object_type;
        public Position position;
        public Rotation rotation;
    }

    //[System.Serializable]
    //public class ImageData
    //{
    //    public string image_id;
    //    public List<ObjectData> objects = new List<ObjectData>();
    //}

    [System.Serializable]
    public class Dataset
    {
        public List<ObjectData> objects = new List<ObjectData>();
    }

    [System.Serializable]
    public class Position
    {
        public float x, y, z;
    }

    [System.Serializable]
    public class Rotation
    {
        public float roll, pitch, yaw;
    }
}
