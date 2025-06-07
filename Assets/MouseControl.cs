using System.Collections.Generic;
using UnityEngine;

public class MouseControl : MonoBehaviour
{
    public FluidSolver fluidSolver;
    public Vector2 forceDir;
    public Vector2 lastMousePos;
    public int radius = 3; // Radius of cells affected
    private Vector2 conePosition; // Center of the cone
    public float coneSharpness = 0.7f; // Controls how pointy the cone is (range: 0 to 1)
    void Start()
    {
        conePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Initialize cone position
    }

    // Update is called once per frame
    void Update()
    {
        TrackMouseMovement();

        // Apply force to the simulation
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ApplyForceToConeSquares();
        }

        // Add density to the simulation
        if (Input.GetMouseButton(1))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            fluidSolver.AddDensity(mousePos, 20);
        }

        // Update the cone position
        MouseMovementCone();
    }

    void TrackMouseMovement()
    {
        Vector2 currentMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // Get current mouse position in world space

        if (Input.GetMouseButton(0)) // If left mouse button is pressed
        {
            Vector2 mouseDelta = currentMousePos - lastMousePos; // Calculate the movement direction
            forceDir = ClampForceDirection(mouseDelta.normalized); // Normalize and clamp the direction
        }

        lastMousePos = currentMousePos; // Update the last mouse position
       // fluidSolver.forceDir = forceDir; // Pass the force direction to the fluid solver
    }

    Vector2 ClampForceDirection(Vector2 direction)
    {
        // Ensure the x and y components of the direction are at least +/- 1
        float clampedX = Mathf.Sign(direction.x) * Mathf.Max(1, Mathf.Abs(direction.x));
        float clampedY = Mathf.Sign(direction.y) * Mathf.Max(1, Mathf.Abs(direction.y));

        return new Vector2(clampedX, clampedY);
    }









    void MouseMovementCone()
    {
        // Update the cone position to the current mouse position if it's over the grid
        Vector2 currentMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (currentMousePos.x >= 0 && currentMousePos.x < FluidSolver.gridSize &&
            currentMousePos.y >= 0 && currentMousePos.y < FluidSolver.gridSize)
        {
            conePosition = currentMousePos; // Update cone position to mouse position
        }

        // If mouse velocity is greater than zero, update the force direction
        float mouseSpeed = (currentMousePos - lastMousePos).magnitude / Time.deltaTime;
        if (mouseSpeed > 0.1f)
        {
            forceDir = (currentMousePos - lastMousePos).normalized; // Update force direction
        }
    }
    void ApplyForceToConeSquares()
    {
        if (fluidSolver == null) return;

        // Store affected positions
        List<Vector2> affectedPositions = new List<Vector2>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2 offset = new Vector2(x, y);
                float distance = offset.magnitude;

                // Check circle or cone shape logic
                if (forceDir.magnitude < 0.1f) // Circle condition
                {
                    if (distance <= radius)
                    {
                        Vector2 cellPosition = new Vector2(
                            Mathf.Floor(conePosition.x) + x,
                            Mathf.Floor(conePosition.y) + y
                        );

                        // Clamp cell position within bounds
                        cellPosition.x = Mathf.Clamp(cellPosition.x, 0, FluidSolver.gridSize - 1);
                        cellPosition.y = Mathf.Clamp(cellPosition.y, 0, FluidSolver.gridSize - 1);

                        affectedPositions.Add(cellPosition);
                    }
                }
                else // Cone condition
                {
                    float dotProduct = Vector2.Dot(offset.normalized, forceDir);

                    if (distance <= radius && dotProduct > (1 - coneSharpness)) // Higher coneSharpness narrows the cone
                    {
                        Vector2 cellPosition = new Vector2(
                            Mathf.Floor(conePosition.x) + x,
                            Mathf.Floor(conePosition.y) + y
                        );

                        // Clamp cell position within bounds
                        cellPosition.x = Mathf.Clamp(cellPosition.x, 0, FluidSolver.gridSize - 1);
                        cellPosition.y = Mathf.Clamp(cellPosition.y, 0, FluidSolver.gridSize - 1);

                        affectedPositions.Add(cellPosition);
                    }
                }
            }
        }

        // Apply the force to all affected positions
        fluidSolver.AddVelocity(affectedPositions.ToArray(), forceDir);
    }


    void OnDrawGizmos()
    {
        if (fluidSolver == null) return;

        Gizmos.color = Color.green;

        // Visualize the affected cells
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2 offset = new Vector2(x, y);
                float distance = offset.magnitude;

                // When the mouse is stationary, keep the shape circular
                if (forceDir.magnitude < 0.1f)
                {
                    if (distance <= radius) // Circle condition
                    {
                        Vector2 cellPosition = new Vector2(
                            Mathf.Floor(conePosition.x) + x,
                            Mathf.Floor(conePosition.y) + y
                        );

                        cellPosition.x = Mathf.Clamp(cellPosition.x, 0, FluidSolver.gridSize - 1);
                        cellPosition.y = Mathf.Clamp(cellPosition.y, 0, FluidSolver.gridSize - 1);

                        Gizmos.DrawCube(new Vector3(cellPosition.x, cellPosition.y, 0), Vector3.one * 0.9f);
                    }
                }
                else // If moving, form a cone in the direction of the mouse velocity
                {
                    float dotProduct = Vector2.Dot(offset.normalized, forceDir);

                    // Use `coneSharpness` to control the cone's angle
                    if (distance <= radius && dotProduct > (1 - coneSharpness)) // Higher coneSharpness narrows the cone
                    {
                        Vector2 cellPosition = new Vector2(
                            Mathf.Floor(conePosition.x) + x,
                            Mathf.Floor(conePosition.y) + y
                        );

                        cellPosition.x = Mathf.Clamp(cellPosition.x, 0, FluidSolver.gridSize - 1);
                        cellPosition.y = Mathf.Clamp(cellPosition.y, 0, FluidSolver.gridSize - 1);

                        Gizmos.DrawCube(new Vector3(cellPosition.x, cellPosition.y, 0), Vector3.one * 0.9f);
                    }
                }
            }
        }
    }





}
