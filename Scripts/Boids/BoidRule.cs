using Godot;
using System.Collections.Generic;
// Updated BoidRule interface
public abstract partial class BoidRule : Node
{
    [Export] public float Weight { get; set; } = 1.0f;

    // Virtual method now includes access to all boid data
    public virtual Vector3 CalculateForce(int boidIndex, List<int> neighbors,
                                         List<SpatialElement> elements,
                                         List<Vector3> velocities)
    {
        return Vector3.Zero;
    }

    // Method to get the effective range of this rule
    public virtual float GetRange()
    {
        return 0.0f;  // Override in concrete rules
    }
}
