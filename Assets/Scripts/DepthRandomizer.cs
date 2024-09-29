using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthRandomizer : MonoBehaviour
{

    [SerializeField] float minDepth;
    [SerializeField] float maxDepth;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.up * Random.Range(minDepth, maxDepth);
    }
}
