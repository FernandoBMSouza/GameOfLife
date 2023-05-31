using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOfLife2D : MonoBehaviour
{
    public int size = 100;
    private bool isCPU, isGPU;
    private float timer = 0;
    public float timeInterval = 0.1f;
    public Color deadColor = Color.black, aliveColor = Color.yellow;
    private Renderer _renderer;
    private Material material;
    private Texture2D texture;
    public ComputeShader computeShader;


    private void Start()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        _renderer = plane.GetComponent<Renderer>();
        material = _renderer.material;
        CreateTexture();
    }

    void Update()
    {
        CheckClick();

        if(isCPU)
        {
            timer += Time.deltaTime;

            if (timer >= timeInterval)
            {
                ProcessCPU();
                //Debug.Log("CPU Timer: " + (timer - timeInterval));
                timer = 0f;
            }
        }

        if(isGPU)
        {
            timer += Time.deltaTime;

            if (timer >= timeInterval)
            {
                ProcessGPU();
                //Debug.Log("GPU Timer: " + (timer - timeInterval));
                timer = 0f;
            } 
        }
    }

    private void CheckClick()
    {
        if (Input.GetMouseButton(0))
        {
            // Criar um raio a partir da posição do mouse na tela
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Verificar se o raio colide com algum objeto
            if (Physics.Raycast(ray, out hit))
            {
                // Verificar se o objeto possui um componente Renderer
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Verificar se o objeto possui uma textura
                    Texture2D targetTexture = renderer.material.mainTexture as Texture2D;
                    if (targetTexture == texture)
                    {
                        // Calcular as coordenadas da textura com base na colisão
                        Vector2 pixelUV = hit.textureCoord;
                        pixelUV.x *= targetTexture.width;
                        pixelUV.y *= targetTexture.height;

                        ChangePixelColor((int)pixelUV.x, (int)pixelUV.y);
                    }
                }
            }
        }
    }

    private void OnGUI() 
    {
        if (GUI.Button(new Rect(0, 0, 100, 50), !isCPU? "Start CPU" : "Stop CPU"))
        {
            isCPU = !isCPU;
        }

        if (GUI.Button(new Rect(110, 0, 100, 50), !isGPU? "Start GPU" : "Stop GPU"))
        {
            isGPU = !isGPU;
        }
    }

    private void ChangePixelColor(int x, int y)
    {
        Color pixelColor = texture.GetPixel(x, y);

        if(pixelColor == deadColor)
            texture.SetPixel(x, y, aliveColor);
        else
            texture.SetPixel(x, y, deadColor);
        
        texture.Apply();
    }

    private void CreateTexture()
    {
        texture = new Texture2D(size, size);
        texture.SetPixels(texture.GetPixels());
        texture.Apply();

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, deadColor);
            }
        }

        texture.Apply();
        material.mainTexture = texture;
    }

    int CountAliveNeighbors(int x, int y)
    {
        int aliveCount = 0;

        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                if (i == x && j == y)
                    continue;

                if (i >= 0 && i < texture.width && j >= 0 && j < texture.height)
                {
                    Color pixelColor = texture.GetPixel(i, j);
                    if (pixelColor == aliveColor)
                        aliveCount++;
                }
            }
        }

        return aliveCount;
    }

    private void ProcessCPU()
    {
        Texture2D newTexture = new Texture2D(size, size);

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                int aliveNeighbors = CountAliveNeighbors(x, y);
                Color pixelColor = texture.GetPixel(x, y);

                if (pixelColor == aliveColor)
                {
                    if (aliveNeighbors == 2 || aliveNeighbors == 3)
                        newTexture.SetPixel(x, y, aliveColor);
                    else
                        newTexture.SetPixel(x, y, deadColor);
                }
                else
                {
                    if (aliveNeighbors == 3)
                        newTexture.SetPixel(x, y, aliveColor);
                    else
                        newTexture.SetPixel(x, y, deadColor);
                }
            }
        }

        newTexture.Apply();
        material.mainTexture = newTexture;
        texture = newTexture;
    }

    private void ProcessGPU()
    {
        RenderTexture renderTexture = new RenderTexture(texture.width, texture.height, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        Graphics.Blit(texture, renderTexture);

        int kernelIndex = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernelIndex, "Result", renderTexture);
        computeShader.SetInt("Width", renderTexture.width);
        computeShader.SetInt("Height", renderTexture.height);

        computeShader.GetKernelThreadGroupSizes(kernelIndex, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        computeShader.Dispatch(kernelIndex, Mathf.CeilToInt(renderTexture.width / (float)threadGroupSizeX), Mathf.CeilToInt(renderTexture.width / (float)threadGroupSizeY), 1);

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        renderTexture.Release();

        material.mainTexture = texture;
        _renderer.material = material;
    }

}
