using UnityEngine;

public class PressureVisualizer : MonoBehaviour
{
    public FluidSolver fluidSolver; // Reference to the FluidSolver script
    public GameObject[,] pressureGrid;
    public Material pressureMaterial;
    public bool visualizePressure = false;
    public KeyCode toggleKey = KeyCode.P; // Key to toggle visualization

    void Start()
    {
        // Initialize the pressure grid with the same size as the fluid solver grid
        pressureGrid = new GameObject[FluidSolver.gridSize, FluidSolver.gridSize];
        InitializePressureGrid();
    }

    void Update()
    {
        // Toggle visualization on key press
        if (Input.GetKeyDown(toggleKey))
        {
            visualizePressure = !visualizePressure;
            TogglePressureVisualization(visualizePressure);
        }

        if (visualizePressure)
        {
            UpdatePressureGrid();
        }
    }

    void InitializePressureGrid()
    {
        for (int x = 0; x < FluidSolver.gridSize; x++)
        {
            for (int y = 0; y < FluidSolver.gridSize; y++)
            {
                // Create a square for each grid cell
                GameObject square = GameObject.CreatePrimitive(PrimitiveType.Quad);
                square.transform.position = new Vector3(x, y, 0);
                square.transform.localScale = new Vector3(0.9f, 0.9f, 1);
                square.transform.SetParent(transform);

                // Assign the pressure material
                square.GetComponent<Renderer>().material = pressureMaterial;

                pressureGrid[x, y] = square;
                square.SetActive(false); // Initially hidden
            }
        }
    }

    void UpdatePressureGrid()
    {
        for (int x = 0; x < FluidSolver.gridSize; x++)
        {
            for (int y = 0; y < FluidSolver.gridSize; y++)
            {
                float pressureValue = fluidSolver.pressure[x, y];
                Color color = GetColorFromPressure(pressureValue);
                pressureGrid[x, y].GetComponent<Renderer>().material.color = color;
            }
        }
    }

    Color GetColorFromPressure(float pressureValue)
    {
        // Map pressure values to a color gradient (e.g., blue to red)
        float normalizedPressure = Mathf.InverseLerp(-1.0f, 1.0f, pressureValue);
        return Color.Lerp(Color.blue, Color.red, normalizedPressure);
    }

    void TogglePressureVisualization(bool enable)
    {
        for (int x = 0; x < FluidSolver.gridSize; x++)
        {
            for (int y = 0; y < FluidSolver.gridSize; y++)
            {
                pressureGrid[x, y].SetActive(enable);
            }
        }
    }
}
