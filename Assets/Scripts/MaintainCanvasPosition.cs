using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaintainCanvasPosition : MonoBehaviour
{
    private Quaternion initialRotation;
    private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        initialRotation = cameraTransform.rotation;
    }

    void LateUpdate()
    {
        transform.rotation = cameraTransform.rotation;
    }
}