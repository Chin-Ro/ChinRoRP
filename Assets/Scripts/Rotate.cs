using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float rotateSpeed;

    [Serializable] 
    public enum RotateDirection
    {
        X,
        Y,
        Z,
        All
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate( new Vector3(Time.deltaTime * rotateSpeed % 360.0f, Time.deltaTime * rotateSpeed % 360.0f, Time.deltaTime * rotateSpeed % 360.0f));
    }
}
