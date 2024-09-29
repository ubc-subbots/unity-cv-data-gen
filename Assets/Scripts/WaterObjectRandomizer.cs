using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;

using UnityEngine.Perception.Randomization.Samplers;

/// <summary>
/// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
/// </summary>
[Serializable]
[AddRandomizerMenu("Water Object Randomizer")]
public class WaterObjectRandomizer : Randomizer
{
    /// <summary>
    /// The Samplers used to place objects in 3D space. Defaults to a uniform distribution in x, normal distribution
    /// in y, and constant value in z. These Samplers can be modified from the Inspector or via code.
    /// </summary>
    //public Vector3Parameter positionDistribution = new()
    //{
    //    x = new NormalSampler(-1f, 1f, 0f, .5f),
    //    y = new UniformSampler(-1f, 1f),
    //    z = new ConstantSampler(2f)
    //};

    public GameObject cameraRotationParent;
    public GameObject waterSurface;
    public GameObject floor;
    public Vector2 depthRange;
    public Vector2 pitchRange;
    public Vector2 yawRange;

    int iterationCounter = 0;

    /// <summary>
    /// The sampler controlling the number of objects to place.
    /// </summary>
    public IntegerParameter objectCount = new() { value = new ConstantSampler(10f) };

    /// <summary>
    /// The list of Prefabs to choose from
    /// </summary>
    public CategoricalParameter<GameObject> prefabs;

    //The container object that will be the parent of all placed objects from this Randomizer
    GameObject m_Container;
    //This cache allows objects to be reused across placements
    UnityEngine.Perception.Randomization.Utilities.GameObjectOneWayCache m_GameObjectOneWayCache;

    /// <inheritdoc/>
    protected override void OnAwake()
    {
        m_Container = new GameObject("Objects");
        m_Container.transform.parent = scenario.transform;
        m_GameObjectOneWayCache = new UnityEngine.Perception.Randomization.Utilities.GameObjectOneWayCache(
            m_Container.transform, prefabs.categories.Select(element => element.Item1).ToArray(), this);
    }

    /// <summary>
    /// Generates a foreground layer of objects at the start of each Scenario Iteration
    /// </summary>
    protected override void OnIterationStart()
    {
        var count = objectCount.Sample();

        float floorHeight = 0;
        float waterHeight = 0;
        bool gateSpawned = false;
        List<Vector3> positionsSpawned = new List<Vector3>();

        // Randomize the camera's position and pitch
        float cameraDistance = UnityEngine.Random.Range(depthRange.x, depthRange.y);
        float cameraRelativePitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        //float cameraRelativeYaw = UnityEngine.Random.Range(yawRange.x, yawRange.y);
        cameraRotationParent.transform.GetChild(0).localPosition = Vector3.back * cameraDistance;
        cameraRotationParent.transform.localEulerAngles = new Vector3(cameraRelativePitch, 0, 0);

        for (int i = 0; i < count; i++)
        {
            var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());

            if (instance.GetComponent<RandomizerSettings>().fixedYPos)
            {
                if (gateSpawned) continue;
                gateSpawned = true;
            }

            int spawnAttempts = 0;

            do
            {
                // Randomize the object's position
                Camera mainCamera = cameraRotationParent.transform.GetChild(0).GetComponent<Camera>();
                float safeMarginX = instance.GetComponent<RandomizerSettings>().boundSize.x / (mainCamera.aspect * 2 * Mathf.Tan(mainCamera.fieldOfView / 2 * Mathf.Deg2Rad) * cameraDistance);
                float safeMarginY = instance.GetComponent<RandomizerSettings>().boundSize.y / (2 * Mathf.Tan(mainCamera.fieldOfView / 2 * Mathf.Deg2Rad) * cameraDistance);
                float randomX = UnityEngine.Random.Range(0.1f + safeMarginX, 0.9f - safeMarginX);
                float randomY = UnityEngine.Random.Range(0.1f + safeMarginY, 0.9f - safeMarginY);
                float randomZ = UnityEngine.Random.Range(Mathf.Max(-0.5f - (cameraDistance - 3), -2f), 2f);

                Vector3 viewportPoint = new Vector3(randomX, randomY, cameraDistance + randomZ);
                Vector3 worldPoint = mainCamera.ViewportToWorldPoint(viewportPoint);

                instance.transform.position = worldPoint + instance.GetComponent<RandomizerSettings>().positionOffset;

                // Randomize the object's rotation (all axes for images, just yaw for gate)
                if (!instance.GetComponent<RandomizerSettings>().fixedYPos)
                {
                    Vector3 rotation = new Vector3(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-15f, 15f));
                    if (rotation.magnitude > 40)
                    {
                        rotation = rotation * 40 / rotation.magnitude;
                    }
                    instance.transform.eulerAngles = Vector3.right * cameraRelativePitch + rotation + instance.GetComponent<RandomizerSettings>().rotationOffset;
                }
                else
                {
                    instance.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(-40f, 40f), 0) + instance.GetComponent<RandomizerSettings>().rotationOffset;
                }

                spawnAttempts++;

            } while (!CheckCanSpawn(positionsSpawned, instance.transform.position) && spawnAttempts < 5);

            positionsSpawned.Add(instance.transform.position);

            //float lowestYPoint = instance.transform.position.y - (instance.GetComponent<RandomizerSettings>().boundSize.y * Mathf.Abs(Mathf.Sin(instance.transform.eulerAngles.x * Mathf.Deg2Rad)));
            //float highestYPoint = instance.transform.position.y + (instance.GetComponent<RandomizerSettings>().boundSize.y * Mathf.Abs(Mathf.Sin(instance.transform.eulerAngles.x * Mathf.Deg2Rad)));

        }

        // Set the water height to just above the camera and the floor height below the lowest object.
        waterSurface.transform.position = Vector3.up * (Mathf.Max(cameraRotationParent.transform.GetChild(0).position.y, ReturnWaterHeight()) + 1);
        floor.transform.position = Vector3.up * ReturnFloorHeight();
        positionsSpawned.Clear();
        iterationCounter += 1;
    }

    /// <summary>
    /// Hides all foreground objects after each Scenario Iteration is complete
    /// </summary>
    protected override void OnIterationEnd()
    {
        m_GameObjectOneWayCache.ResetAllObjects();
    }

    bool CheckCanSpawn(List<Vector3> posList, Vector3 currentPos)
    {
        foreach(Vector3 pos in posList) { 
            // Determine if object is too close to an object in terms of x & y axes only
            if(Mathf.Sqrt(Mathf.Pow(pos.x - currentPos.x, 2) + Mathf.Pow(pos.y - currentPos.y, 2)) < 0.35f)
            {
                return false;
            }
        }

        return true;
    }

    float ReturnFloorHeight()
    {
        GameObject objectContainer = GameObject.Find("Objects");
        float floorHeight = 1000;

        for(int i = 0; i < objectContainer.transform.childCount; i++)
        {
            if (objectContainer.transform.GetChild(i).transform.position.x > 100) continue;

            float lowestPointY = objectContainer.transform.GetChild(i).transform.position.y - objectContainer.transform.GetChild(i).GetComponent<RandomizerSettings>().boundSize.y;
            if (lowestPointY < floorHeight) floorHeight = lowestPointY;
        }

        Debug.Log(floorHeight);
        if (floorHeight == 1000) return 0;
        return floorHeight;
    }

    float ReturnWaterHeight()
    {
        GameObject objectContainer = GameObject.Find("Objects");
        float floorHeight = -1000;

        for (int i = 0; i < objectContainer.transform.childCount; i++)
        {
            if (objectContainer.transform.GetChild(i).transform.position.x > 100) continue;

            float lowestPointY = objectContainer.transform.GetChild(i).transform.position.y + objectContainer.transform.GetChild(i).GetComponent<RandomizerSettings>().boundSize.y;
            if (lowestPointY > floorHeight) floorHeight = lowestPointY;
        }

        if (floorHeight == -1000) return 0;
        return floorHeight;
    }


}
