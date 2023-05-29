using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;

    public Vector3 initialPosition;
    public Quaternion initialRotation;

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
            // Movimento horizontal
            float horizontal = Input.GetAxis("Horizontal");
            transform.Translate(Vector3.right * horizontal * moveSpeed * Time.deltaTime);

            // Movimento vertical
            float vertical = Input.GetAxis("Vertical");
            transform.Translate(Vector3.forward * vertical * moveSpeed * Time.deltaTime);

            // Rotação com o mouse
            float rotationX = Input.GetAxis("Mouse X");
            float rotationY = Input.GetAxis("Mouse Y");
            transform.Rotate(Vector3.up * rotationX * rotationSpeed * Time.deltaTime);
            transform.Rotate(Vector3.left * rotationY * rotationSpeed * Time.deltaTime);
        }
    }
}
