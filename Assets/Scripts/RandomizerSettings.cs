using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizerSettings : MonoBehaviour
{
    public Vector2 boundSize;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public bool fixedYPos;

    private void Start()
    {
        boundSize *= transform.localScale.x;
    }
}
