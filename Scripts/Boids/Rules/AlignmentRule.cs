using Godot;
using System.Collections.Generic;

public partial class AlignmentRule : BoidRule
{
    [Export] public BoidResource resource;

    public override Vector3 CalculateForce(int boidIndex, List<int> neighbors,
                                         List<OctTreeElement> elements,
                                         List<Vector3> velocities)
    {
        if (neighbors.Count == 0)
            return Vector3.Zero;

        Vector3 averageVelocity = Vector3.Zero;
        int count = 0;

        foreach (int neighborIndex in neighbors)
        {
            // Skip self
            if (neighborIndex == boidIndex)
                continue;

            // Only consider neighbors within alignment range
            float distance = elements[boidIndex].Position.DistanceTo(elements[neighborIndex].Position);
            if (distance <= resource.AlignmentRange)
            {
                averageVelocity += velocities[neighborIndex];
                count++;
            }
        }

        if (count > 0)
        {
            averageVelocity /= count;

            // Calculate steering force
            Vector3 steeringForce = averageVelocity - velocities[boidIndex];

            // Limit force
            steeringForce = steeringForce.LimitLength(resource.MaxForce);

            return steeringForce;
        }

        return Vector3.Zero;
    }

    public override float GetRange()
    {
        return resource.AlignmentRange;
    }

    public override void _Ready()
    {
        // If resource isn't set in the Inspector, try to get it from BoidManager
        if (resource == null)
        {
            var boidManager = GetParent<BoidManager>();
            if (boidManager != null)
            {
                resource = boidManager.resource;
            }
        }
    }
}
