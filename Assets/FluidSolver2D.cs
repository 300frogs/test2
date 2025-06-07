using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class FluidSolver : MonoBehaviour
{
    public const int gridSize = 48;
    private float[,] velocitiesU = new float[gridSize, gridSize];
    private float[,] velocitiesV = new float[gridSize, gridSize];
    public float[,] pressure = new float[gridSize, gridSize];
    private float[,] smokeDensity = new float[gridSize, gridSize];
    private GameObject[,] gridSquares = new GameObject[gridSize, gridSize];
    private LineRenderer[,] velocityLines = new LineRenderer[gridSize, gridSize];
    public float dampingFactor = 0.99f;
    public float viscosity = 0.1f;
    public float timeStep = 0.1f;
    public float force = 1.0f;
    public float SwirlStrength = 2f;
    public Vector2 forceDir = new Vector2(1.0f, 1.0f); // Default force direction
    public float smokeDiffusion = 0.1f;
    public Color smokeColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
    private Vector2 lastMousePos;
    public bool smoketest;
    private float currentAngle = 0f; // Keeps track of the rotation angle of the cone
    void Start()
    {
        InitializeGrid();
    }

    void Update()
    {
        if (smoketest)
        {
            SmokeTesting();
        }

       AdvectVelocity();
        UpdateFluid();
        DiffuseSmoke();
        AdvectSmoke();
        VisualizeGrid();
    }


    private void InitializeGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // Initialize grid squares
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.transform.position = new Vector3(x, y, 0);
                square.transform.localScale = new Vector3(0.9f, 0.9f, 1); // Adjust size for better visibility
                square.transform.SetParent(transform);
                gridSquares[x, y] = square;

                // Initialize line renderers for velocity visualization
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
    public void AddVelocity(Vector2[] positions, Vector2 velocity)
    {
        foreach (Vector2 position in positions)
        {
            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y);

            if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
            {
                // Apply velocity to the cell
                velocitiesU[x, y] += velocity.x;
                velocitiesV[x, y] += velocity.y;

                // Apply positive pressure in the direction of force
                pressure[x, y] += force * 0.5f;

                // Calculate the opposite direction
                Vector2 oppositeDir = -velocity.normalized;
                int xOpposite = Mathf.Clamp(x + Mathf.RoundToInt(oppositeDir.x), 0, gridSize - 1);
                int yOpposite = Mathf.Clamp(y + Mathf.RoundToInt(oppositeDir.y), 0, gridSize - 1);

                // Apply negative pressure to the cell in the opposite direction
                pressure[xOpposite, yOpposite] -= force * 0.5f;
            }
        }
    }



    public void AddDensity(Vector2 mousePos, float amount)
    {
        int x = Mathf.FloorToInt(mousePos.x);
        int y = Mathf.FloorToInt(mousePos.y);

        if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
        {
            smokeDensity[x, y] += amount;
        }
    }

    private void UpdateFluid()
    {
        // Apply viscosity
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
        ApplyVorticityConfinement();


        // Update pressure and apply pressure gradients
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                pressure[x, y] = 0.25f * (pressure[x - 1, y] + pressure[x + 1, y] + pressure[x, y - 1] + pressure[x, y + 1]);
                velocitiesU[x, y] -= (pressure[x + 1, y] - pressure[x - 1, y]) * 0.5f;
                velocitiesV[x, y] -= (pressure[x, y + 1] - pressure[x, y - 1]) * 0.5f;
            }
        }

        // Set boundary conditions to mirror forces (enclosed box)
        for (int x = 0; x < gridSize; x++)
        {
            velocitiesU[x, 0] = -velocitiesU[x, 1];
            velocitiesV[x, 0] = -velocitiesV[x, 1];
            velocitiesU[x, gridSize - 1] = -velocitiesU[x, gridSize - 2];
            velocitiesV[x, gridSize - 1] = -velocitiesV[x, gridSize - 2];
        }

        for (int y = 0; y < gridSize; y++)
        {
            velocitiesU[0, y] = -velocitiesU[1, y];
            velocitiesV[0, y] = -velocitiesV[1, y];
            velocitiesU[gridSize - 1, y] = -velocitiesU[gridSize - 2, y];
            velocitiesV[gridSize - 1, y] = -velocitiesV[gridSize - 2, y];
        }
    }


    public void SmokeTesting()
    {
        Vector2 center = new Vector2(gridSize / 2, gridSize / 2);

        // Add smoke density at the center
        AddDensity(center, Random.Range(50, 150));

        // Define cone parameters
        int coneRadius = 5; // Radius of the cone
        float coneSharpness = 0.7f; // Controls the width of the cone
        float rotationSpeed = Mathf.PI / 4; // Speed of rotation (radians per frame)

        // Create a list of affected positions for the cone
        List<Vector2> conePositions = new List<Vector2>();

        for (int x = -coneRadius; x <= coneRadius; x++)
        {
            for (int y = -coneRadius; y <= coneRadius; y++)
            {
                Vector2 offset = new Vector2(x, y);
                float distance = offset.magnitude;

                if (distance <= coneRadius) // Check if within the radius
                {
                    // Calculate the direction of the cell relative to the center
                    Vector2 direction = offset.normalized;

                    // Calculate the cone's current orientation using the angle
                    Vector2 coneDirection = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));

                    // Use a dot product to check if the cell is within the cone
                    float dotProduct = Vector2.Dot(direction, coneDirection);
                    if (dotProduct > (1 - coneSharpness)) // Higher coneSharpness narrows the cone
                    {
                        Vector2 cellPosition = new Vector2(
                            Mathf.Floor(center.x) + x,
                            Mathf.Floor(center.y) + y
                        );

                        // Clamp the cell position within bounds
                        cellPosition.x = Mathf.Clamp(cellPosition.x, 0, gridSize - 1);
                        cellPosition.y = Mathf.Clamp(cellPosition.y, 0, gridSize - 1);

                        conePositions.Add(cellPosition);
                    }
                }
            }
        }

        // Apply velocity to the cone positions
        Vector2 velocity = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * 0.2f;
        AddVelocity(conePositions.ToArray(), velocity); // Convert List to Array

        // Increment the rotation angle
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle >= Mathf.PI * 2) currentAngle -= Mathf.PI * 2; // Keep angle within [0, 2?]
    }


    private void DiffuseSmoke()
    {
        float[,] newSmokeDensity = new float[gridSize, gridSize];
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                newSmokeDensity[x, y] = smokeDensity[x, y] + smokeDiffusion * (smokeDensity[x - 1, y] + smokeDensity[x + 1, y] + smokeDensity[x, y - 1] + smokeDensity[x, y + 1] - 4 * smokeDensity[x, y]) * timeStep;
            }
        }
        // Set the boundary smoke densities to zero
        for (int i = 0; i < gridSize; i++)
        {
            newSmokeDensity[i, 0] = 0;               // Bottom boundary
            newSmokeDensity[i, gridSize - 1] = 0;    // Top boundary
            newSmokeDensity[0, i] = 0;               // Left boundary
            newSmokeDensity[gridSize - 1, i] = 0;    // Right boundary
        }

        smokeDensity = newSmokeDensity;
    }
    void ApplyVorticityConfinement()
    {
        float[,] vorticity = new float[gridSize, gridSize];
        float[,] vorticityForceX = new float[gridSize, gridSize];
        float[,] vorticityForceY = new float[gridSize, gridSize];


        // Compute vorticity for all grid points
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                vorticity[x, y] = CalculateVorticity(x, y);
            }
        }

        // Compute vorticity force
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                // Gradient of vorticity (magnitude and direction)
                float gradVorticityX = (vorticity[x + 1, y] - vorticity[x - 1, y]) * 0.5f;
                float gradVorticityY = (vorticity[x, y + 1] - vorticity[x, y - 1]) * 0.5f;
                float length = Mathf.Sqrt(gradVorticityX * gradVorticityX + gradVorticityY * gradVorticityY) + 0.0001f; // Prevent division by zero

                // Normalize gradient and compute vorticity confinement force
                vorticityForceX[x, y] = gradVorticityY / length * vorticity[x, y];
                vorticityForceY[x, y] = -gradVorticityX / length * vorticity[x, y];

                vorticityForceX[x, y] *= SwirlStrength;
                vorticityForceY[x, y] *= SwirlStrength;
            }
        }

        // Apply vorticity force to velocity fields
        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                velocitiesU[x, y] += vorticityForceX[x, y] * timeStep;
                velocitiesV[x, y] += vorticityForceY[x, y] * timeStep;

                // Apply damping to vorticity force

                velocitiesU[x, y] *= dampingFactor;
                velocitiesV[x, y] *= dampingFactor;
            }

        }
    }
    private void AdvectVelocity()
    {
        float[,] newVelocitiesU = new float[gridSize, gridSize];
        float[,] newVelocitiesV = new float[gridSize, gridSize];

        for (int x = 1; x < gridSize - 1; x++)
        {
            for (int y = 1; y < gridSize - 1; y++)
            {
                // Calculate the previous position of this cell in the velocity field
                float xPrev = x - velocitiesU[x, y] * timeStep;
                float yPrev = y - velocitiesV[x, y] * timeStep;

                // Clamp to keep within grid bounds
                xPrev = Mathf.Clamp(xPrev, 1, gridSize - 2);
                yPrev = Mathf.Clamp(yPrev, 1, gridSize - 2);

                // Perform bilinear interpolation for velocity U
                int x0 = Mathf.FloorToInt(xPrev);
                int y0 = Mathf.FloorToInt(yPrev);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float s1 = xPrev - x0;
                float s0 = 1 - s1;
                float t1 = yPrev - y0;
                float t0 = 1 - t1;

                newVelocitiesU[x, y] =
                    s0 * (t0 * velocitiesU[x0, y0] + t1 * velocitiesU[x0, y1]) +
                    s1 * (t0 * velocitiesU[x1, y0] + t1 * velocitiesU[x1, y1]);

                // Perform bilinear interpolation for velocity V
                newVelocitiesV[x, y] =
                    s0 * (t0 * velocitiesV[x0, y0] + t1 * velocitiesV[x0, y1]) +
                    s1 * (t0 * velocitiesV[x1, y0] + t1 * velocitiesV[x1, y1]);
            }
        }

        // Update velocities with the new advected values
        velocitiesU = newVelocitiesU;
        velocitiesV = newVelocitiesV;
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

                xPrev = Mathf.Clamp(xPrev, 1, gridSize - 2);
                yPrev = Mathf.Clamp(yPrev, 1, gridSize - 2);

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
        // Make sides periodic (right to left)
        for (int y = 0; y < gridSize; y++)
        {
            newSmokeDensity[0, y] = smokeDensity[gridSize - 2, y];
            newSmokeDensity[gridSize - 1, y] = smokeDensity[1, y];
        }
        smokeDensity = newSmokeDensity;
    }


    private void VisualizeGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // Update color based on smoke density
                float density = smokeDensity[x, y];
                Color color = smokeColor * density;
                color.a = Mathf.Clamp01(density);
                gridSquares[x, y].GetComponent<Renderer>().material.color = color;

                // Update line renderer to visualize velocity direction
                LineRenderer lineRenderer = velocityLines[x, y];
                lineRenderer.SetPosition(0, new Vector3(x, y, 0));
                lineRenderer.SetPosition(1, new Vector3(x + velocitiesU[x, y], y + velocitiesV[x, y], 0));
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
    }
    float CalculateVorticity(int x, int y)
    {
        return (velocitiesV[x + 1, y] - velocitiesV[x - 1, y]) * 0.5f - (velocitiesU[x, y + 1] - velocitiesU[x, y - 1]) * 0.5f;
    }

}