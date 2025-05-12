using Godot;
[GlobalClass]
public partial class BoidResource : Resource
{
    [Export] public float AlignmentRange = 3f;     // How far to look for alignment
    [Export] public float CohesionRange = 5f;      // Larger range for cohesion
    [Export] public float SeparationRange = 2f;     // Keep this smaller than alignment
    [Export] public float followRange = 1f;  // not implemented yet.

    [Export] public float MaxForce = 0.3f;          // Reduced to prevent sharp turns
    [Export] public float MaxSpeed = .6f;          // Slightly slower for smoother movement

    [Export] public float AlignmentWeight = 1.0f;   // Keep boids pointing same direction
    [Export] public float CohesionWeight = 0.8f;    // Pull toward center of flock 
    [Export] public float SeparationWeight = 1.2f;  // Slightly higher to prevent crowding
    [Export] public float FollowWeight = 1.0f;
    [Export] public int MaxNeighborsToConsider = 7;
    [Export] public int count = 100;
}
