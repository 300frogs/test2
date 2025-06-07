using Mono.Cecil.Cil;
using UnityEngine;

public class FluidSimTest4 : MonoBehaviour
{

    public class Cell
    {
        public float density;
        public Vector2 velocity;

        public Cell(float density = 0, Vector2 velocity = default)
        {
            this.density = density;
            this.velocity = velocity;
        }
    }


    int n = 64;
    int size;
    float dt; // time step
    float diff;
    float visc;



    float[] previousDensities;
    float[] densities;

    float[] PrevVel_x;
    float[] PrevVel_y;

    float[] Vel_x;
    float[] Vel_y;
    //array of cells, 
    Cell[,] grid;
    Cell[,] NextGrid;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grid = new Cell[n, n];

        // Initialize each cell with default values
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                grid[x, y] = new Cell(0, Vector2.zero);
            }
        }

        foreach(Cell cell in grid)
        {
           // tell me each possition of cells
        }
    }



    // Update is called once per frame
    void Update()
    {
        //visualizeCells();
    }

    void visualizeCells()
    {
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                
            }
        }
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;

        float cellSize = 1.0f / n; // Size of each cell
        Gizmos.color = Color.white;

        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                if (grid[x, y] != null)
                {
                    // Map density to color intensity (darker for higher density)
                    float density = Mathf.Clamp(grid[x, y].density, 0, 1);
                    Gizmos.color = new Color(density, density, density); // Gray scale based on density

                    // Correctly position each cell based on its grid coordinates
                    Vector3 position = new Vector3(x * cellSize, y * cellSize, 0);
                    Gizmos.DrawCube(position, new Vector3(cellSize, cellSize, cellSize));
                }
            }
        }
    }

}
