using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubController : MonoBehaviour
{
    public GameObject[] propellers;
    public float[] propellerSpeeds;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            GetComponent<Rigidbody>().AddForceAtPosition(Vector3.forward * propellerSpeeds[0], propellers[0].transform.position);
        }

        if (Input.GetKey(KeyCode.E))
        {
            GetComponent<Rigidbody>().AddForceAtPosition(Vector3.forward * propellerSpeeds[1], propellers[1].transform.position);
        }

        if (Input.GetKey(KeyCode.A))
        {
            GetComponent<Rigidbody>().AddForceAtPosition(Vector3.forward * propellerSpeeds[2], propellers[2].transform.position);
        }

        if (Input.GetKey(KeyCode.D))
        {
            GetComponent<Rigidbody>().AddForceAtPosition(Vector3.forward * propellerSpeeds[3], propellers[3].transform.position);
        }

    }

    public void UpdatePropellerSpeeds()
    {

    }
}
