using UnityEngine;
using System.Collections;

public class NavierStokesSimulation : MonoBehaviour
{
    public int gridSizeX = 256;
    public int gridSizeY = 256;
    public float timeStep = 0.01f;
    public float viscosity = 0.1f;
    public float pressureDiffusion = 0.01f;
    public float density = 1.0f;

    private Vector2[,] velocityField;
    private float[,] pressureField;
    private float[,] smokeDensity;
    private float[,] newSmokeDensity;
    private GameObject[,] gridSquares;
    private LineRenderer[,] velocityLines;
    public bool SmokeTest;
    void Start()
    {
        InitGrid(gridSizeX, gridSizeY);
        InitVelocityField();
        InitPressureField();
        InitSmokeDensity();

        StartCoroutine(Simulate());
    }

    void InitGrid(int sizeX, int sizeY)
    {
        gridSquares = new GameObject[sizeX, sizeY];
        velocityLines = new LineRenderer[sizeX, sizeY];

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.transform.position = new Vector3(x, y, 0);
                square.transform.localScale = new Vector3(0.9f, 0.9f, 1);
                square.transform.SetParent(transform);
                gridSquares[x, y] = square;

                GameObject lineObj = new GameObject("VelocityLine");
                lineObj.transform.SetParent(transform);
                LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.05f;
                lineRenderer.endWidth = 0.05f;
                lineRenderer.positionCount = 2;
                velocityLines[x, y] = lineRenderer;
            }
        }
    }

    void InitVelocityField()
    {
        velocityField = new Vector2[gridSizeX, gridSizeY];
    }

    void InitPressureField()
    {
        pressureField = new float[gridSizeX, gridSizeY];
    }

    void InitSmokeDensity()
    {
        smokeDensity = new float[gridSizeX, gridSizeY];
        newSmokeDensity = new float[gridSizeX, gridSizeY];
    }

    IEnumerator Simulate()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeStep);
            if (SmokeTest)
            {
                AddSmokeAndApplyForces();
            }
            UpdateVelocityField();
            UpdatePressureField();
            DiffuseSmoke();
            AdvectSmoke();
            VisualizeGrid();
        }
    }

    void AddSmokeAndApplyForces()
    {
        int centerX = gridSizeX / 2;
        int centerY = gridSizeY / 2;
        float smokeAmount = 1.0f;
        Vector2 forceDirection = new Vector2(1.0f, 0.0f); // Applying a force to the right

        // Add smoke to the center
        smokeDensity[centerX, centerY] += smokeAmount;

        // Apply force at the center
        velocityField[centerX, centerY] += forceDirection;
    }

    void UpdateVelocityField()
    {
        Vector2[,] newVelocityField = new Vector2[gridSizeX, gridSizeY];

        for (int i = 1; i < gridSizeX - 1; i++)
        {
            for (int j = 1; j < gridSizeY - 1; j++)
            {
                // Calculate the velocity difference from neighboring nodes
                Vector2 velocityDiff = new Vector2(
                    velocityField[i + 1, j].x + velocityField[i - 1, j].x + velocityField[i, j + 1].x + velocityField[i, j - 1].x - 4 * velocityField[i, j].x,
                    velocityField[i + 1, j].y + velocityField[i - 1, j].y + velocityField[i, j + 1].y + velocityField[i, j - 1].y - 4 * velocityField[i, j].y
                );

                // Update velocity field based on Navier-Stokes equations
                // Viscous term:
                // ∂u/∂t + u∇u = -1/ρ ∇p - ν ∇^2 u
                Vector2 fxx = new Vector2(
                    viscosity * velocityDiff.x,
                    viscosity * velocityDiff.y
                );

                // Update the velocity field
                newVelocityField[i, j] = velocityField[i, j] + timeStep * fxx / density;
            }
        }

        // Update the velocity field with the new values
        for (int i = 1; i < gridSizeX - 1; i++)
        {
            for (int j = 1; j < gridSizeY - 1; j++)
            {
                velocityField[i, j] = newVelocityField[i, j];
            }
        }
    }

    void UpdatePressureField()
    {
        for (int iter = 0; iter < 20; iter++) // Iterative solver
        {
            for (int i = 1; i < gridSizeX - 1; i++)
            {
                for (int j = 1; j < gridSizeY - 1; j++)
                {
                    pressureField[i, j] = 0.25f * (pressureField[i + 1, j] + pressureField[i - 1, j] + pressureField[i, j + 1] + pressureField[i, j - 1]
                                                   - (velocityField[i + 1, j].x - velocityField[i - 1, j].x + velocityField[i, j + 1].y - velocityField[i, j - 1].y) / density);
                }
            }
        }
    }

    void DiffuseSmoke()
    {
        for (int iter = 0; iter < 20; iter++) // Iterative solver
        {
            for (int i = 1; i < gridSizeX - 1; i++)
            {
                for (int j = 1; j < gridSizeY - 1; j++)
                {
                    newSmokeDensity[i, j] = smokeDensity[i, j] + viscosity * (smokeDensity[i + 1, j] + smokeDensity[i - 1, j] + smokeDensity[i, j + 1] + smokeDensity[i, j - 1]
                                              - 4 * smokeDensity[i, j]) * timeStep;
                }
            }
            // Swap references
            float[,] temp = smokeDensity;
            smokeDensity = newSmokeDensity;
            newSmokeDensity = temp;
        }
    }

    void AdvectSmoke()
    {
        for (int i = 1; i < gridSizeX - 1; i++)
        {
            for (int j = 1; j < gridSizeY - 1; j++)
            {
                float xPrev = i - velocityField[i, j].x * timeStep;
                float yPrev = j - velocityField[i, j].y * timeStep;

                xPrev = Mathf.Clamp(xPrev, 0, gridSizeX - 1);
                yPrev = Mathf.Clamp(yPrev, 0, gridSizeY - 1);

                int x0 = (int)xPrev;
                int y0 = (int)yPrev;
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float s1 = xPrev - x0;
                float s0 = 1 - s1;
                float t1 = yPrev - y0;
                float t0 = 1 - t1;

                smokeDensity[i, j] = s0 * (t0 * smokeDensity[x0, y0] + t1 * smokeDensity[x0, y1]) + s1 * (t0 * smokeDensity[x1, y0] + t1 * smokeDensity[x1, y1]);
            }
        }
    }

    void VisualizeGrid()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                float density = smokeDensity[x, y];
                Color color = new Color(density, density, density, 0.5f);
                gridSquares[x, y].GetComponent<Renderer>().material.color = color;

                LineRenderer lineRenderer = velocityLines[x, y];
                lineRenderer.SetPosition(0, new Vector3(x, y, 0));
                lineRenderer.SetPosition(1, new Vector3(x + velocityField[x, y].x, y + velocityField[x, y].y, 0));
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
    }
}
