using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOfLife3DXYZ : MonoBehaviour
{
    struct Cube
    {
        public Vector3 position;
        public Color color;
    }
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private int nCubes = 20;
    [SerializeField] private float timeInterval = 1f;
    private float timer = 0;
    private GameObject[] gameObjects;
    private Cube[] data;
    private Color _colorInic = Color.white;
    private bool isCPU, isGPU;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedCube = hit.collider.gameObject;
                ChangeCubeColor(clickedCube);
            }
        }

        if(isCPU)
        {
            timer += Time.deltaTime;

            if (timer >= timeInterval)
            {
                SimulateGameOfLifeCPU();
                Debug.Log("CPU Timer: " + (timer - timeInterval));
                timer = 0f;
            } 
        }

        if(isGPU)
        {
            timer += Time.deltaTime;

            if (timer >= timeInterval)
            {
                SimulateGameOfLifeGPU();
                Debug.Log("GPU Timer: " + (timer - timeInterval));
                timer = 0f;
            }
        }
    }

    private void OnGUI() 
    {
        if (GUI.Button(new Rect(0, 0, 100, 50), "Create"))
        {
            CreateCubes();
        }

        if (GUI.Button(new Rect(110, 0, 100, 50), "Stop"))
        {
            isCPU = false;
            isGPU = false;
        }

        if (GUI.Button(new Rect(0, 60, 100, 50), "Simulate CPU"))
        {
            isCPU = true;
        }

        if (GUI.Button(new Rect(110, 60, 100, 50), "Simulate GPU"))
        {
            isGPU = true;
        }
    }

    private void CreateCubes()
    {
        int cubeCount = nCubes * nCubes * nCubes;
        data = new Cube[cubeCount];
        gameObjects = new GameObject[cubeCount];

        int index = 0;

        for (int x = 0; x < nCubes; x++)
        {
            float offsetX = (-nCubes / 2 + x);

            for (int y = 0; y < nCubes; y++)
            {
                float offsetY = (-nCubes / 2 + y);

                for (int z = 0; z < nCubes; z++)
                {
                    float offsetZ = (-nCubes / 2 + z);

                    GameObject go = GameObject.Instantiate(cubePrefab,
                        new Vector3(offsetX * 1.1f, offsetY * 1.1f, offsetZ * 1.1f),
                        Quaternion.identity);

                    go.GetComponent<MeshRenderer>().material.SetColor("_Color", _colorInic);
                    gameObjects[index] = go;

                    data[index] = new Cube();
                    data[index].position = go.transform.position;
                    data[index].color = _colorInic;

                    index++;
                }
            }
        }
    }

    private void ChangeCubeColor(GameObject cube)
    {
        int cubeIndex = Array.IndexOf(gameObjects, cube);
        //Debug.Log(cubeIndex);

        if(cube.GetComponent<MeshRenderer>().material.color == _colorInic)
        {
            cube.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.yellow);
            data[cubeIndex].color = Color.yellow;
        }
        else
        {
            cube.GetComponent<MeshRenderer>().material.SetColor("_Color", _colorInic);
            data[cubeIndex].color = _colorInic;
        }
    }

    private void SimulateGameOfLifeCPU()
    {
        bool[] nextGeneration = new bool[nCubes * nCubes * nCubes];

        for (int i = 0; i < gameObjects.Length; i++)
        {
            int aliveNeighbors = CountYellowNeighbors(gameObjects[i]);

            if (gameObjects[i].GetComponent<MeshRenderer>().material.GetColor("_Color") == _colorInic)
                nextGeneration[i] = aliveNeighbors == 3;
            else
                nextGeneration[i] = aliveNeighbors == 2 || aliveNeighbors == 3;
        }

        for (int i = 0; i < nCubes * nCubes * nCubes; i++)
        {
            if (nextGeneration[i])
            {
                gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.yellow);
                gameObjects[i].SetActive(true);
            }
            else
            {
                gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", _colorInic);
                gameObjects[i].SetActive(false);
            }
        }
    }


    private int CountYellowNeighbors(GameObject cube)
    {
        int count = 0;
        Vector3 cubePosition = cube.transform.position;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && y == 0 && z == 0)
                        continue;

                    Vector3 neighborPosition = cubePosition + new Vector3(x * 1.1f, y * 1.1f, z * 1.1f);

                    Collider[] colliders = Physics.OverlapSphere(neighborPosition, 0.1f);

                    foreach (var collider in colliders)
                    {
                        GameObject neighborCube = collider.gameObject;
                        if (neighborCube != cube && neighborCube.GetComponent<MeshRenderer>().material.GetColor("_Color") == Color.yellow)
                        {
                            count++;
                        }
                    }
                }
            }
        }

        return count;
    }

    
    private void SimulateGameOfLifeGPU()
    {
        ComputeBuffer cubeBuffer = new ComputeBuffer(data.Length, sizeof(float) * 7);
        cubeBuffer.SetData(data);
        computeShader.SetBuffer(0, "cubeBuffer", cubeBuffer);
        computeShader.SetInt("nCubes", nCubes);

        computeShader.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out uint threadGroupSizeZ);
        computeShader.Dispatch(0, Mathf.CeilToInt(nCubes / (float)threadGroupSizeX), Mathf.CeilToInt(nCubes / (float)threadGroupSizeY), Mathf.CeilToInt(nCubes / (float)threadGroupSizeZ));

        cubeBuffer.GetData(data);

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (data[i].color.r == 1 && data[i].color.g == 1 && data[i].color.b == 1)
            {
                gameObjects[i].SetActive(false);
            }
            else
            {
                gameObjects[i].SetActive(true);
                gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", data[i].color);
            }
        }

        cubeBuffer.Dispose();
    }
}