using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Simulator : Node3D
{
    private List<OctTreeElement> elements = new();

    private BoidManager boidManager;
    public ISpatialPartitioning spatialSystem;

    // Configuration
    [Export] public SpatialPartitioningMode Mode = SpatialPartitioningMode.Automatic;
    [Export] public float RefreshInterval = 0.25f;
    [Export] public float ColliderMarkingInterval = 1.0f;
    [Export] public BoidResource boidResource;
    [Export] public OctreeResource octreeResource;

    // Timing
    private float timeSinceLastRefresh = 0f;
    private float timeSinceLastColliderUpdate = 0f;

    // For automatic mode
    private const int AUTO_SWITCH_THRESHOLD = 500; // Switch to cells above this count

    private List<int> refreshIndices = new List<int>();
    private SpatialPartitioningVisualizer visualizer;

    public enum SpatialPartitioningMode
    {
        OctTree,
        SpatialCell,
        BVH,
        Automatic // Switch based on boid count
    }

    public override void _Ready()
    {
        boidManager = GetNode<BoidManager>("BoidManager");
        boidManager.resource = boidResource;
        visualizer = new SpatialPartitioningVisualizer();
        AddChild(visualizer);
        // Create initial spatial system based on configuration
        InitializeSpatialSystem();

        // Initialize boids
        InitializeBoids();
    }
    public void RestartSimulation()
    {
        boidManager = GetNode<BoidManager>("BoidManager");
        boidManager.resource = boidResource;

        // Create new spatial system
        InitializeSpatialSystem();

        // Initialize boids
        InitializeBoids();
    }

    private void InitializeSpatialSystem()
    {
        // Decide which system to use
        SpatialPartitioningMode currentMode = Mode;
        if (Mode == SpatialPartitioningMode.Automatic)
        {
            currentMode = (boidResource.count > AUTO_SWITCH_THRESHOLD) ?
                SpatialPartitioningMode.SpatialCell :
                SpatialPartitioningMode.OctTree;
            // Note: BVH isn't part of automatic switching yet
        }

        // Clean up existing spatial system
        if (spatialSystem != null && spatialSystem is Node node)
        {
            node.QueueFree();
        }

        // Create new system with standardized initialization
        if (currentMode == SpatialPartitioningMode.OctTree)
        {
            var octree = new DataOrientedOctTree();
            AddChild(octree);
            octree.Initialize(
                Position,
                octreeResource.RootSize,
                octreeResource.MaxElementsPerNode,
                index => elements[index]
            );
            spatialSystem = octree;
        }
        else if (currentMode == SpatialPartitioningMode.BVH)
        {
            var bvhManager = new BVHManager();
            AddChild(bvhManager);
            bvhManager.Initialize(
                Position,
                octreeResource.RootSize,  // Use same size for consistency
                octreeResource.MaxElementsPerNode,  // Use as density hint
                index => elements[index]  // Same callback function
            );
            spatialSystem = bvhManager;
        }
        else // SpatialCell mode
        {
            var cellManager = new SpatialCellManager();
            AddChild(cellManager);
            cellManager.Initialize(
                Position,
                octreeResource.RootSize,  // Use same size for consistency
                octreeResource.MaxElementsPerNode,  // Use as density hint
                index => elements[index]  // Same callback function
            );
            spatialSystem = cellManager;
        }
        visualizer.Target = spatialSystem;
    }

    // Add a method to toggle visualization
    public void ToggleVisualization()
    {
        visualizer.Toggle();
    }
    // Add this method to your Simulator class
    [Conditional("DEBUG")]
    private void AssertDataSynchronization()
    {
        // Check that element count matches velocity/acceleration count
        Debug.Assert(elements.Count == boidManager.GetVelocityCount(),
            $"Data desync: elements.Count ({elements.Count}) != velocity count ({boidManager.GetVelocityCount()})");

        // You could add more detailed checks if needed
    }
    private void InitializeBoids()
    {
        elements.Clear();
        boidManager.ClearBoids();

        for (int i = 0; i < boidResource.count; i++)
        {
            // Calculate random position within bounds
            float halfSize = octreeResource.RootSize / 2;
            Vector3 position = new(
                (float)GD.RandRange((Position.X - halfSize), (Position.X + halfSize)),
                (float)GD.RandRange((Position.Y - halfSize), (Position.Y + halfSize)),
                (float)GD.RandRange((Position.Z - halfSize), (Position.Z + halfSize))
            );

            // Add positional data to simulator
            elements.Add(new OctTreeElement(position, 1.0f));

            // Add velocity and acceleration to BoidManager
            boidManager.AddBoidData(
                (float)GD.RandRange(-1.5, 1.5),
                (float)GD.RandRange(-1.5, 1.5),
                (float)GD.RandRange(-1.5, 1.5)
            );
        }

        // Ensure capacity for reusable list
        refreshIndices.EnsureCapacity(elements.Count);

        // Initial spatial system update
        RefreshSpatialSystem();
        UpdateColliderInfo();
        AssertDataSynchronization();
    }

    public override void _PhysicsProcess(double delta)
    {
        AssertDataSynchronization();

        // Let BoidManager update velocities and accelerations based on rules
        boidManager.UpdateBoidBehaviors(elements, spatialSystem);

        // Update positions based on physics
        for (int i = 0; i < elements.Count; i++)
        {
            Vector3 newPosition = elements[i].Position + boidManager.GetVelocity(i);

            // Only check collisions for boids near colliders
            // if (spatialSystem.IsNearCollider(elements[i].Position) ||
            //     spatialSystem.IsNearCollider(newPosition))
            // {
            //     // Perform raycast for collision
            //     var spaceState = GetWorld3D().DirectSpaceState;
            //     var origin = elements[i].Position;
            //     var end = newPosition;
            //     var query = PhysicsRayQueryParameters3D.Create(origin, end);
            //     var result = spaceState.IntersectRay(query);
            //
            //     if (result.ContainsKey("collider"))
            //     {
            //         // Handle collision
            //         Vector3 intersectionPoint = (Vector3)result["position"];
            //         Vector3 normal = (Vector3)result["normal"];
            //
            //         newPosition = intersectionPoint + normal * 0.5f;
            //         boidManager.HandleCollision(i, normal);
            //     }
            // }

            // Apply position wrapping if needed
            newPosition = spatialSystem.WrapPosition(newPosition);
            elements[i] = new OctTreeElement(newPosition, elements[i].Size);
        }

        // Update spatial system periodically
        timeSinceLastRefresh += (float)delta;
        if (timeSinceLastRefresh >= RefreshInterval)
        {
            RefreshSpatialSystem();
            timeSinceLastRefresh = 0f;
        }

        // Update collider info less frequently
        timeSinceLastColliderUpdate += (float)delta;
        if (timeSinceLastColliderUpdate >= ColliderMarkingInterval)
        {
            UpdateColliderInfo();
            timeSinceLastColliderUpdate = 0f;
        }

        // Check for mode switching in Automatic mode
        if (Mode == SpatialPartitioningMode.Automatic)
        {
            bool shouldUseCells = elements.Count > AUTO_SWITCH_THRESHOLD;
            bool isCellManager = spatialSystem is SpatialCellManager;

            if (shouldUseCells != isCellManager)
            {
                // Need to switch modes
                InitializeSpatialSystem();
                RefreshSpatialSystem();
                UpdateColliderInfo();
            }
        }

        AssertDataSynchronization();
    }
    private void RefreshSpatialSystem()
    {
        // Clear and reuse list for indices
        refreshIndices.Clear();

        for (int i = 0; i < elements.Count; i++)
        {
            refreshIndices.Add(i);
        }

        spatialSystem.Clear();
        spatialSystem.Insert(refreshIndices);

        // Special case for SpatialCellManager
        if (spatialSystem is SpatialCellManager cellManager)
        {
            cellManager.UpdateCells(refreshIndices);
        }
    }

    private void UpdateColliderInfo()
    {
        spatialSystem.UpdateColliderInfo();
    }
    // Methods to add/remove boids
    public void AddBoid(Vector3 position, float size)
    {
        elements.Add(new OctTreeElement(position, size));
        boidManager.AddBoidData((float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5));
        AssertDataSynchronization();
    }

    public void RemoveBoid(int index)
    {
        if (index >= 0 && index < elements.Count)
        {
            elements.RemoveAt(index);
            boidManager.RemoveBoidData(index);
        }
        AssertDataSynchronization();
    }

    public override void _Process(double delta)
    {
        // Visualization (you might want to move this to a separate component later)
        for (int index = 0; index < elements.Count; index++)
        {
            DebugDraw3D.DrawSquare(elements[index].Position, elements[index].Size, Colors.Black);
        }
    }
}
