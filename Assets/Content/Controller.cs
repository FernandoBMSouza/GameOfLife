using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    struct Cube
    {
        public Vector3 position;
        public Color color;
    }
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private int nCubes = 20;
    [SerializeField] private float timeInterval = 0.5f;
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
                timer = 0f;
            } 
        }

        if(isGPU)
        {
            timer += Time.deltaTime;

            if (timer >= timeInterval)
            {
                SimulateGameOfLifeGPU();
                timer = 0f;
            }
        }
    }

    private void OnGUI() 
    {
        if (GUI.Button(new Rect(0, 0, 100, 50), "Create CPU"))
        {
            CreateCubes();
        }

        if (GUI.Button(new Rect(110, 0, 100, 50), "Pause CPU/GPU Simulation"))
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
        data = new Cube[nCubes * nCubes];
        gameObjects = new GameObject[nCubes * nCubes];

        for(int i = 0; i < nCubes; i++)
        {
            float offsetX = (-nCubes / 2 + i);
            for(int j = 0; j < nCubes; j++)
            {
                float offsetZ = (-nCubes / 2 + j);
                GameObject go = GameObject.Instantiate(cubePrefab, 
                    new Vector3(offsetX * 1.1f, 0, offsetZ * 1.1f), 
                    Quaternion.identity);

                go.GetComponent<MeshRenderer>().material.SetColor("_Color", _colorInic);
                gameObjects[j * nCubes + i] = go;
                data[i * nCubes + j] = new Cube();
                data[i * nCubes + j].position = go.transform.position;
                data[i * nCubes + j].color = _colorInic;
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
        bool[] nextGeneration = new bool[nCubes * nCubes];

        for (int i = 0; i < gameObjects.Length; i++)
        {
            int aliveNeighbors = CountYellowNeighbors(gameObjects[i]);
            
            if(gameObjects[i].GetComponent<MeshRenderer>().material.color == _colorInic)
                nextGeneration[i] = aliveNeighbors == 3;
            else
                nextGeneration[i] = aliveNeighbors == 2 || aliveNeighbors == 3;
        }

        for (int i = 0; i < nCubes * nCubes; i++)
        {
            if(nextGeneration[i])
                gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.yellow);
            else
                gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", _colorInic);
        }
    }

    private int CountYellowNeighbors(GameObject cube)
    {
        int count = 0;
        Vector3 cubePosition = cube.transform.position;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                Vector3 neighborPosition = cubePosition + new Vector3(i * 1.1f, 0f, j * 1.1f);
                Collider[] colliders = Physics.OverlapSphere(neighborPosition, 0.1f);

                foreach (var collider in colliders)
                {
                    GameObject neighborCube = collider.gameObject;
                    if (neighborCube != cube && neighborCube.GetComponent<MeshRenderer>().material.color == Color.yellow)
                    {
                        count++;
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

        computeShader.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        computeShader.Dispatch(0, Mathf.CeilToInt(nCubes / (float)threadGroupSizeX), Mathf.CeilToInt(nCubes / (float)threadGroupSizeY), 1);

        cubeBuffer.GetData(data);

        for (int i = 0; i < gameObjects.Length; i++)
        {
            gameObjects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", data[i].color);
        }

        cubeBuffer.Dispose();
    }

}
