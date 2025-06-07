public class FluidCube
{
    private int size;
    private float dt, diff, visc;

    public float[] s, density;
    private float[] Vx, Vy;
    private float[] Vx0, Vy0;
    private bool[,] isOccupied;

    public FluidCube(int size, float diffusion, float viscosity, float dt)
    {
        this.size = size;
        this.dt = dt;
        this.diff = diffusion;
        this.visc = viscosity;

        int N = size * size;
        s = new float[N];
        density = new float[N];
        Vx = new float[N];
        Vy = new float[N];
        Vx0 = new float[N];
        Vy0 = new float[N];
        isOccupied = new bool[size, size]; // Initialize occupancy grid
    }


    public void AddDensity(int x, int y, float amount)
    {
        density[IX(x, y)] += amount;
    }

    public void AddVelocity(int x, int y, float amountX, float amountY)
    {
        Vx[IX(x, y)] += amountX;
        Vy[IX(x, y)] += amountY;
    }

    public void Step(int iter)
    {
        Diffuse(1, Vx0, Vx, visc, dt, iter, size);
        Diffuse(2, Vy0, Vy, visc, dt, iter, size);
        Project(Vx0, Vy0, Vx, Vy, iter, size);
        Advect(1, Vx, Vx0, Vx0, Vy0, dt, size);
        Advect(2, Vy, Vy0, Vx0, Vy0, dt, size);
        Project(Vx, Vy, Vx0, Vy0, iter, size);
        Diffuse(0, s, density, diff, dt, iter, size);
        Advect(0, density, s, Vx, Vy, dt, size);
    }

    public int IX(int x, int y)
    {
        return x + y * size;
    }

    void LinSolve(int b, float[] x, float[] x0, float a, float c, int iter, int N)
    {
        float cRecip = 1.0f / c; // Reciprocal of c for efficiency
        for (int k = 0; k < iter; k++)
        {
            for (int j = 1; j < N - 1; j++)
            {
                for (int i = 1; i < N - 1; i++)
                {
                    x[IX(i, j)] =
                        (x0[IX(i, j)] +
                         a * (x[IX(i + 1, j)] + x[IX(i - 1, j)] +
                              x[IX(i, j + 1)] + x[IX(i, j - 1)])) * cRecip;
                }
            }
            SetBounds(b, x, N); // Reapply boundary conditions after each iteration
        }
    }

    void Project(float[] Vx, float[] Vy, float[] p, float[] div, int iter, int N)
    {
        // Calculate divergence and reset pressure
        for (int j = 1; j < N - 1; j++)
        {
            for (int i = 1; i < N - 1; i++)
            {
                div[IX(i, j)] = -0.5f * (
                    Vx[IX(i + 1, j)] - Vx[IX(i - 1, j)] +
                    Vy[IX(i, j + 1)] - Vy[IX(i, j - 1)]
                ) / N;
                p[IX(i, j)] = 0;
            }
        }

        SetBounds(0, div, N);
        SetBounds(0, p, N);
        LinSolve(0, p, div, 1, 4, iter, N);

        // Adjust velocities based on pressure field
        for (int j = 1; j < N - 1; j++)
        {
            for (int i = 1; i < N - 1; i++)
            {
                Vx[IX(i, j)] -= 0.5f * (p[IX(i + 1, j)] - p[IX(i - 1, j)]) * N;
                Vy[IX(i, j)] -= 0.5f * (p[IX(i, j + 1)] - p[IX(i, j - 1)]) * N;
            }
        }


        SetBounds(1, Vx, N);
        SetBounds(2, Vy, N);
    }

    void Advect(int b, float[] d, float[] d0, float[] Vx, float[] Vy, float dt, int N)
    {
        float dt0 = dt * (N - 2);
        for (int j = 1; j < N - 1; j++)
        {
            for (int i = 1; i < N - 1; i++)
            {
                float x = i - dt0 * Vx[IX(i, j)];
                float y = j - dt0 * Vy[IX(i, j)];

                // Clamp values to grid bounds
                if (x < 0.5f) x = 0.5f;
                if (x > N - 1.5f) x = N - 1.5f;
                if (y < 0.5f) y = 0.5f;
                if (y > N - 1.5f) y = N - 1.5f;

                int i0 = (int)x, i1 = i0 + 1;
                int j0 = (int)y, j1 = j0 + 1;

                float s1 = x - i0, s0 = 1 - s1;
                float t1 = y - j0, t0 = 1 - t1;

                d[IX(i, j)] =
                    s0 * (t0 * d0[IX(i0, j0)] + t1 * d0[IX(i0, j1)]) +
                    s1 * (t0 * d0[IX(i1, j0)] + t1 * d0[IX(i1, j1)]);
            }
        }

        SetBounds(b, d, N);
    }
    public void SetOccupied(int x, int y, bool occupied)
    {
        isOccupied[x, y] = occupied;
    }

    void SetBounds(int b, float[] x, int N)
    {
        for (int i = 1; i < N - 1; i++)
        {
            for (int j = 1; j < N - 1; j++)
            {
                if (isOccupied[i, j])
                {
                    // For occupied cells, reflect velocities as with boundaries
                    x[IX(i, j)] = b == 1 ? -x[IX(i + 1, j)] : (b == 2 ? -x[IX(i, j + 1)] : 0);
                }
            }
        }

        // Handle standard boundary edges
        for (int i = 1; i < N - 1; i++)
        {
            x[IX(0, i)] = b == 1 ? -x[IX(1, i)] : x[IX(1, i)];
            x[IX(N - 1, i)] = b == 1 ? -x[IX(N - 2, i)] : x[IX(N - 2, i)];
            x[IX(i, 0)] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
            x[IX(i, N - 1)] = b == 2 ? -x[IX(i, N - 2)] : x[IX(i, N - 2)];
        }

        // Handle corners
        x[IX(0, 0)] = 0.5f * (x[IX(1, 0)] + x[IX(0, 1)]);
        x[IX(0, N - 1)] = 0.5f * (x[IX(1, N - 1)] + x[IX(0, N - 2)]);
        x[IX(N - 1, 0)] = 0.5f * (x[IX(N - 2, 0)] + x[IX(N - 1, 1)]);
        x[IX(N - 1, N - 1)] = 0.5f * (x[IX(N - 2, N - 1)] + x[IX(N - 1, N - 2)]);
    }








    void Diffuse(int b, float[] x, float[] x0, float diff, float dt, int iter, int N)
    {
        float a = dt * diff * (N - 2) * (N - 2); // Coefficient for diffusion
        LinSolve(b, x, x0, a, 1 + 4 * a, iter, N); // Solve diffusion equation
    }





}
