using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float axisSpeed = 100f, rotationAndZoomSpeed = 500f;

    [SerializeField] private Vector3 initialPosition;
    [SerializeField] private Quaternion initialRotation;

    private bool enableCamera;

    private void Start()
    {
        Debug.Log("Pressione SPACE para liberar o movimento da câmera");
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            enableCamera = !enableCamera;
        
        if(enableCamera)
        {
            // Horizontal
            float horizontal = Input.GetAxis("Horizontal");
            transform.Translate(Vector3.right * horizontal * axisSpeed * Time.deltaTime);

            // Vertical
            float vertical = Input.GetAxis("Vertical");
            transform.Translate(Vector3.up * vertical * axisSpeed * Time.deltaTime);

            // Zoom In
            if (Input.GetKeyDown(KeyCode.Z))
                transform.Translate(Vector3.forward * rotationAndZoomSpeed * Time.deltaTime);
            // Zoom Out
            if (Input.GetKeyDown(KeyCode.X))
                transform.Translate(Vector3.back * rotationAndZoomSpeed * Time.deltaTime);

            // Rotation
            float rotationX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up * rotationX * rotationAndZoomSpeed * Time.deltaTime);
        }
    }
}
