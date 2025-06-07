using UnityEngine;

public class RGBshifter : MonoBehaviour
{
    public FluidSolver fluidSolver;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RGBsmoke();
    }

    public void RGBsmoke()
    {
        float time = Time.time; // Use Time.time to get the total elapsed time
        float cycle = Mathf.PingPong(time, 3.0f); // Create a smooth cycle between 0 and 3

        if (cycle < 1.0f)
        {
            // Transition from red to blue
            fluidSolver.smokeColor = new Color(1.0f - cycle, 0.0f, cycle, 0.5f);
        }
        else if (cycle < 2.0f)
        {
            // Transition from blue to green
            float adjustedCycle = cycle - 1.0f;
            fluidSolver.smokeColor = new Color(0.0f, adjustedCycle, 1.0f - adjustedCycle, 0.5f);
        }
        else
        {
            // Transition from green to red
            float adjustedCycle = cycle - 2.0f;
           fluidSolver.smokeColor = new Color(adjustedCycle, 1.0f - adjustedCycle, 0.0f, 0.5f);
        }
    }
}
