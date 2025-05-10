using Godot;
using System.Collections.Generic;

public partial class BoidManager : Node3D
{
    [Export] public BoidResource resource;

    // Store velocity and acceleration
    private List<Vector3> velocities = new();
    private List<Vector3> accelerations = new();

    // List of rules
    private List<BoidRule> rules = new();

    // Spatial cell manager for optimization (optional)
    private SpatialCellManager cellManager;

    // Reusable collections
    private List<int> filteredNeighborsCache = new List<int>();

    public override void _Ready()
    {
        // Subscribe to child added notification
        ChildEnteredTree += OnChildEnteredTree;
        ChildExitingTree += OnChildExitingTree;

        // Collect any rules that are already children
        foreach (Node child in GetChildren())
        {
            if (child is BoidRule rule)
            {
                rules.Add(rule);
            }
        }
    }

    private void OnChildEnteredTree(Node node)
    {
        if (node is BoidRule rule)
        {
            rules.Add(rule);
        }
    }

    private void OnChildExitingTree(Node node)
    {
        if (node is BoidRule rule)
        {
            rules.Remove(rule);
        }
    }

    public void ClearBoids()
    {
        velocities.Clear();
        accelerations.Clear();
    }

    public void AddBoidData(float vx, float vy, float vz)
    {
        velocities.Add(new Vector3(vx, vy, vz).Normalized() * resource.MaxSpeed);
        accelerations.Add(Vector3.Zero);
    }

    public void RemoveBoidData(int index)
    {
        if (index >= 0 && index < velocities.Count)
        {
            velocities.RemoveAt(index);
            accelerations.RemoveAt(index);
        }
    }

    public Vector3 GetVelocity(int index)
    {
        return velocities[index];
    }

    public void HandleCollision(int index, Vector3 normal)
    {
        // Reflect velocity using the normal
        float dotProduct = velocities[index].Dot(normal);
        Vector3 reflection = velocities[index] - 2 * dotProduct * normal;

        // Maintain max speed while preserving direction
        velocities[index] = reflection.Normalized() * resource.MaxSpeed;
    }

    // Return velocity count for data validation
    public int GetVelocityCount()
    {
        return velocities.Count;
    }

    // Get the maximum range among all active rules
    public float GetMaxRuleRange()
    {
        float maxRange = 0f;
        foreach (var rule in rules)
        {
            maxRange = Mathf.Max(maxRange, rule.GetRange());
        }
        return maxRange;
    }

    // Process all boid behaviors - now works with ISpatialPartitioning
    public void UpdateBoidBehaviors(List<OctTreeElement> elements, ISpatialPartitioning spatialSystem)
    {
        // Check if we should use cell manager's optimized processing
        if (spatialSystem is SpatialCellManager cellManager)
        {
            // Let cell manager handle optimization decisions
            if (cellManager.ShouldUseCellOptimization(elements.Count))
            {
                // Process with cell manager's optimized approach
                cellManager.ProcessCells(
                    elements,
                    spatialSystem, // Pass the spatial system (which could be itself as ISpatialPartitioning)
                    (boidIdx, neighbors, elems) => FilterNeighborsByDistance(boidIdx, neighbors, elems),
                    (boidIdx, filteredNeighbors) => ProcessBoid(boidIdx, filteredNeighbors, elements)
                );
                return;
            }
        }

        // Generic approach for any spatial system (including octree or cell manager fallback)
        for (int index = 0; index < elements.Count; index++)
        {
            // Get neighbors from spatial system
            float maxRange = GetMaxRuleRange();
            List<int> neighbors = spatialSystem.FindNearby(elements[index].Position, maxRange);

            // Filter by distance only (no visibility checks)
            List<int> filteredNeighbors = FilterNeighborsByDistance(index, neighbors, elements);

            // Apply rules
            ProcessBoid(index, filteredNeighbors, elements);
        }
    }

    // Process an individual boid
    private void ProcessBoid(int boidIdx, List<int> neighbors, List<OctTreeElement> elements)
    {
        // Apply all rules to calculate total force
        Vector3 totalForce = Vector3.Zero;

        foreach (var rule in rules)
        {
            totalForce += rule.CalculateForce(boidIdx, neighbors, elements, velocities) * rule.Weight;
        }

        // Update physics
        accelerations[boidIdx] += totalForce;
        velocities[boidIdx] += accelerations[boidIdx];
        velocities[boidIdx] = velocities[boidIdx].LimitLength(resource.MaxSpeed);
        accelerations[boidIdx] = Vector3.Zero;
    }

    // Filter neighbors by distance only (no visibility checks)
    private List<int> FilterNeighborsByDistance(int self, List<int> neighbors, List<OctTreeElement> elements)
    {
        filteredNeighborsCache.Clear();
        float maxRangeSqr = GetMaxRuleRange() * GetMaxRuleRange();

        foreach (int i in neighbors)
        {
            // Skip self
            if (i == self) continue;

            // Check distance using squared distance (more efficient)
            float distSqr = elements[self].Position.DistanceSquaredTo(elements[i].Position);
            if (distSqr <= maxRangeSqr)
            {
                filteredNeighborsCache.Add(i);
            }
        }

        return filteredNeighborsCache;
    }
}
