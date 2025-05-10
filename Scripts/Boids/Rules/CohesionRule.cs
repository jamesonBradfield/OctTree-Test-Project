using Godot;
using System.Collections.Generic;

public partial class CohesionRule : BoidRule
{
    [Export] public BoidResource resource;

    public override Vector3 CalculateForce(int boidIndex, List<int> neighbors,
                                         List<OctTreeElement> elements,
                                         List<Vector3> velocities)
    {
        if (neighbors.Count == 0)
            return Vector3.Zero;

        Vector3 centerOfMass = Vector3.Zero;
        int count = 0;

        foreach (int neighborIndex in neighbors)
        {
            // Skip self
            if (neighborIndex == boidIndex)
                continue;

            // Only consider neighbors within cohesion range
            float distance = elements[boidIndex].Position.DistanceTo(elements[neighborIndex].Position);
            if (distance <= resource.CohesionRange)
            {
                centerOfMass += elements[neighborIndex].Position;
                count++;
            }
        }

        if (count > 0)
        {
            centerOfMass /= count;

            // Create desired velocity toward center of mass
            Vector3 desiredVelocity = (centerOfMass - elements[boidIndex].Position).Normalized() * resource.MaxSpeed;

            // Calculate steering force
            Vector3 steeringForce = desiredVelocity - velocities[boidIndex];

            // Limit force
            steeringForce = steeringForce.LimitLength(resource.MaxForce);

            return steeringForce;
        }

        return Vector3.Zero;
    }

    public override float GetRange()
    {
        return resource.CohesionRange;
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
