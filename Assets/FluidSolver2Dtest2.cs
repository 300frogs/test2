using UnityEditor.AssetImporters;
using UnityEngine;

public class FluidSolver2Dtest2 : MonoBehaviour
{
    public const int gridSize = 100;
    private float[,] velocitiesU = new float[gridSize, gridSize];
    private float[,] velocitiesV = new float[gridSize, gridSize];
    private float[,] smokeDensity = new float[gridSize, gridSize];
    private GameObject[,] gridSquares = new GameObject[gridSize, gridSize];

    public float viscosity = 0.2f;
    public float diffusion = 0.0000001f;
    public float timeStep = 0.1f;
    public float Force = .2f;
    public float Density = 100f;
    private float t = 0.0f;

    

    void Start()
    {
        InitializeGrid();
    }

    void Update()
    {
        AddDensity();
        AddVelocity();
        Step();

        VisualizeGrid();
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.transform.position = new Vector3(x, y, 0);
                square.transform.localScale = new Vector3(0.9f, 0.9f, 1);
                square.transform.SetParent(transform);
                gridSquares[x, y] = square;
            }
        }
    }

    private void AddDensity()
    {
        int cx = gridSize / 2;
        int cy = gridSize / 2;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                smokeDensity[cx + i, cy + j] += Density;
            }
        }
    }

    private void AddVelocity()
    {
        


        int cx = gridSize / 2;
        int cy = gridSize / 2;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                float angle = Mathf.PerlinNoise(t, 0) * Mathf.PI * 2; // Adjust the angle range
                //Vector2 v = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.2f;
                Vector2 v = Vector2.down * Force;
                velocitiesU[cx + i, cy + j] += v.x;
                velocitiesV[cx + i, cy + j] += v.y;
                t += 0.01f;
            }
        }
    }


    private void Step()
    {
        // Implement fluid simulation steps here (diffusion, advection, etc.)
        DiffuseSmoke();
        AdvectSmoke();
        ApplyVelocity();
        ApplyDamping(0.99f);

    }

    private void DiffuseSmoke()
    {
        float[,] newSmokeDensity = new float[gridSize, gridSize];
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                newSmokeDensity[x, y] = smokeDensity[x, y] + diffusion * (smokeDensity[x - 1, y] + smokeDensity[x + 1, y] + smokeDensity[x, y - 1] + smokeDensity[x, y + 1] - 4 * smokeDensity[x, y]) * timeStep;
            }
        }
        smokeDensity = newSmokeDensity;
    }

    private void AdvectSmoke()
    {
        float[,] newSmokeDensity = new float[gridSize, gridSize];
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                float xPrev = x - velocitiesU[x, y] * timeStep;
                float yPrev = y - velocitiesV[x, y] * timeStep;

                xPrev = Mathf.Clamp(xPrev, 0, gridSize - 1);
                yPrev = Mathf.Clamp(yPrev, 0, gridSize - 1);

                int x0 = Mathf.FloorToInt(xPrev);
                int y0 = Mathf.FloorToInt(yPrev);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float s1 = xPrev - x0;
                float s0 = 1 - s1;
                float t1 = yPrev - y0;
                float t0 = 1 - t1;

                newSmokeDensity[x, y] = s0 * (t0 * smokeDensity[x0, y0] + t1 * smokeDensity[x0, y1]) + s1 * (t0 * smokeDensity[x1, y0] + t1 * smokeDensity[x1, y1]);
            }
        }
        smokeDensity = newSmokeDensity;
    }

    private void ApplyVelocity()
    {
        // Apply the velocities to the smoke density
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                float u = velocitiesU[x, y];
                float v = velocitiesV[x, y];
                velocitiesU[x, y] += viscosity * (velocitiesU[x - 1, y] + velocitiesU[x + 1, y] + velocitiesU[x, y - 1] + velocitiesU[x, y + 1] - 4 * u) * timeStep;
                velocitiesV[x, y] += viscosity * (velocitiesV[x - 1, y] + velocitiesV[x + 1, y] + velocitiesV[x, y - 1] + velocitiesV[x, y + 1] - 4 * v) * timeStep;
            }
        }
    }
    private void ApplyDamping(float dampingFactor)
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                velocitiesU[x, y] *= dampingFactor;
                velocitiesV[x, y] *= dampingFactor;
                smokeDensity[x, y] *= dampingFactor;
            }
        }
    }

    private void VisualizeGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                float density = smokeDensity[x, y];
                Color color = new Color(density / 255, density / 255, density / 255, Mathf.Clamp01(density / 255));
                gridSquares[x, y].GetComponent<Renderer>().material.color = color;
            }
        }
    }
}
