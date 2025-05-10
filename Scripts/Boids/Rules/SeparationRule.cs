using Godot;
using System.Collections.Generic;

public partial class SeparationRule : BoidRule
{
    [Export] public BoidResource resource;

    public override Vector3 CalculateForce(int boidIndex, List<int> neighbors,
                                        List<OctTreeElement> elements,
                                        List<Vector3> velocities)
    {
        if (neighbors.Count == 0)
            return Vector3.Zero;

        Vector3 separationForce = Vector3.Zero;
        int count = 0;

        foreach (int neighborIndex in neighbors)
        {
            // Skip self
            if (neighborIndex == boidIndex)
                continue;

            float distance = elements[boidIndex].Position.DistanceTo(elements[neighborIndex].Position);

            // Only consider neighbors within separation range
            if (distance <= resource.SeparationRange && distance > 0)
            {
                // Calculate repulsion vector (away from neighbor)
                Vector3 repulsion = elements[boidIndex].Position - elements[neighborIndex].Position;

                // Scale by distance (closer = stronger)
                repulsion = repulsion.Normalized() / distance;

                separationForce += repulsion;
                count++;
            }
        }

        if (count > 0)
        {
            // Average the forces
            separationForce /= count;

            // Scale to max speed if needed
            if (separationForce.Length() > 0)
            {
                separationForce = separationForce.Normalized() * resource.MaxSpeed;
            }

            // Calculate steering force
            Vector3 steeringForce = separationForce - velocities[boidIndex];

            // Limit force
            steeringForce = steeringForce.LimitLength(resource.MaxForce);

            return steeringForce;
        }

        return Vector3.Zero;
    }

    public override float GetRange()
    {
        return resource.SeparationRange;
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
