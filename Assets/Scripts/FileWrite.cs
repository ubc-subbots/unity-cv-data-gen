using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileWrite : MonoBehaviour
{
    private string filePath;

    void Start()
    {
        // Setting the file path to the persistent data path
        

        //// Writing to the file
        //WriteToFile("Hello, this is a test message stored in persistent data.");

        //// Reading from the file
        //string content = ReadFromFile();
        //Debug.Log(content);
    }

    public void InitializeFile(string fileName)
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName + ".txt");
        Debug.Log(filePath);

        File.Delete(filePath);
    }

    public void InitializeSoloFile()
    {
        int counter = 1;
        if(Directory.Exists(Path.Combine(Application.persistentDataPath, "solo")))
        {
            while(Directory.Exists(Path.Combine(Application.persistentDataPath, "solo_" + counter))){
                counter++;
            }
        }
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "solo_" + counter + "_annotations"));
        filePath = Path.Combine(Application.persistentDataPath, "solo_" + counter + "_annotations");
        Debug.Log(filePath);
    }

    public void WriteToFile(string text)
    {
        try
        {
            //File.WriteAllText(filePath, text);
            File.AppendAllText(filePath, "\n" + text);
            Debug.Log("Data written: " + text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error writing to file: " + e.Message);
        }
    }

    public void WriteToNewFile(string text, int count)
    {
        try
        {
            //File.WriteAllText(filePath, text);
            File.AppendAllText(Path.Combine(filePath, "annotation_" + count + ".txt"), text);
            //Debug.Log("Data written: " + text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error writing to file: " + e.Message);
        }
    }

    string ReadFromFile()
    {
        try
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                Debug.LogWarning("File not found.");
                return "";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error reading from file: " + e.Message);
            return "";
        }
    }
}
