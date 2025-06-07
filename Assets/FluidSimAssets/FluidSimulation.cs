using UnityEngine;

public class FluidSimulation : MonoBehaviour
{
    public int gridSize = 64; // Size of the simulation grid
    public float diffusion = 0.0001f; // Diffusion rate
    public float viscosity = 0.0001f; // Fluid viscosity
    public float timeStep = 0.1f; // Time step for the simulation
    public int iterations = 4; // Solver iterations
    public GameObject cellPrefab; // Prefab for individual grid cells
    public float cellSize = 1;
    private FluidCube fluid;       // Fluid simulation object
    private GameObject[,] cells;   // 2D array of grid cell GameObjects
    private GameObject highlightedCell; // Store the currently highlighted cell
    public float SmokeDensity = 100.0f;
    public float fadeRate = 0.95f; // The rate at which density fades (adjust as needed)
    private Vector2 previousMousePos;
    public bool isDragging = false;
        void Start()
    {
        // Initialize the fluid simulation
        fluid = new FluidCube(gridSize, diffusion, viscosity, timeStep);

        // Create the grid of cells
        cells = new GameObject[gridSize, gridSize];
        GenerateGrid();
    }

    void Update()
    {
        HandleInput();  // Handle user input
        SimulateFluid(); // Perform fluid simulation
        FadeDensity(); // Fade out density
       UpdateCellColors(); // Update colors of the cells based on density

    }



    void HandleInput()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Map mouse position to grid space
        int x = Mathf.Clamp((int)((mousePos.x + gridSize / 2.0f) / cellSize), 0, gridSize - 1);
        int y = Mathf.Clamp((int)((mousePos.y + gridSize / 2.0f) / cellSize), 0, gridSize - 1);

        // Highlight the cell under the mouse
        HighlightCell(x, y);

        if (Input.GetMouseButton(0))
        {
            // Add density and velocity on mouse click
            fluid.AddDensity(x, y, SmokeDensity);
            fluid.AddVelocity(x, y, Random.Range(-10f, 10f), Random.Range(-10f, 10f));
        }
        if (Input.GetMouseButton(1))
        {
            ExhaleSmoke();
        }
    
            
    

        if ( isDragging) // While holding the back button
        {
            Debug.Log("dragging");
            // Compute the drag velocity
            Vector2 dragVelocity = (mousePos - previousMousePos) ;

            // Add density and velocity at the current mouse position
            
            fluid.AddVelocity(x, y, dragVelocity.x, dragVelocity.y);

            // Update the previous mouse position
            previousMousePos = mousePos;
        }

      
    }
    void ExhaleSmoke()
    {
        Debug.Log("ExhaleSmoke");
        fluid.AddDensity(48, 1, SmokeDensity);
        float Ranx = Random.Range(-1f, 1f);
        float Rany = Random.Range(5f, 7f);
        fluid.AddVelocity(48, 1, Ranx,Rany);
    }

    void HighlightCell(int x, int y)
    {
        // Reset the previously highlighted cell, if any
        if (highlightedCell != null)
        {
            highlightedCell.GetComponent<Renderer>().material.color = Color.white; // Reset to default color
        }

        // Highlight the current cell
        highlightedCell = cells[x, y];
        highlightedCell.GetComponent<Renderer>().material.color = Color.green;
    }
    void FadeDensity()
    {

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                // Multiply the density by the fade rate
                fluid.density[fluid.IX(x, y)] *= fadeRate;
            }
        }
    }


    void SimulateFluid()
    {
        fluid.Step(iterations); // Advance the simulation by one step
    }

    void GenerateGrid()
    {


        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                // Create a new cell GameObject
                GameObject cell = Instantiate(cellPrefab);
                cell.transform.SetParent(transform);

                // Set the scale of each cell
                cell.transform.localScale = new Vector3(cellSize, cellSize, 1);

                // Adjust the position of the cell
                cell.transform.position = new Vector3(x * cellSize - gridSize / 2.0f, y * cellSize - gridSize / 2.0f, 0);

                // Store the cell in the array
                cells[x, y] = cell;

                CheckCellOccupancy(x, y, cell);
            }
        }
    }
    void CheckCellOccupancy(int x, int y, GameObject cell)
    {
        Vector2 position = new Vector2(cell.transform.position.x, cell.transform.position.y);

        // Cast a ray from the cell's position
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero);

        // Check if the raycast hit something
        bool occupied = hit.collider != null;

        // Update the FluidCube occupancy grid
        fluid.SetOccupied(x, y, occupied);

        // Update the cell color for visualization
        if (occupied)
        {
            cell.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            cell.GetComponent<Renderer>().material.color = Color.white;
        }
    }



    void UpdateCellColors()
    {
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                // Get density value and normalize it
                float value = Mathf.Clamp(fluid.density[fluid.IX(x, y)], 0, 1);

                // Set the cell's color based on density
                cells[x, y].GetComponent<Renderer>().material.color = new Color(value, value, value, 1.0f); // Grayscale
            }
        }
    }
}
