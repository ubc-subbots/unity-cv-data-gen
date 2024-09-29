using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionRecorder : MonoBehaviour
{
    [SerializeField] Transform simScenario;
    [SerializeField] FileWrite fileManager;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (simScenario.GetChild(0).childCount != 0)
        {
            if (simScenario.GetChild(0).GetChild(0) != null)
            {
                //Debug.Log(simScenario.GetChild(0).GetChild(0).transform.position);
                fileManager.WriteToFile(simScenario.GetChild(0).GetChild(0).transform.position.ToString());
            }
        }
        else
        {
            fileManager.WriteToFile("N/A");
        }
        
    }
}
