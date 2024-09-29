using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowSim : MonoBehaviour
{
    //[SerializeField] Transform simulationScenario;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.childCount != 0)
        {
            //transform.position = new Vector3(transform.position.x, simulationScenario.GetChild(0).GetChild(0).position.y, transform.position.z);
            for (int i = 1; i < transform.GetChild(0).childCount; i++){
                transform.GetChild(0).GetChild(i).gameObject.SetActive(false);
            }
            
        }
    }
}
