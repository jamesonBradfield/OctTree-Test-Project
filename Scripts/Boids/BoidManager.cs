using Godot;
using System.Collections.Generic;

public partial class BoidManager : Node3D
{
    [Export] public BoidResource resource;
    public float OctreeRefreshInterval
    {
        get => resource.octreeRefreshInterval;
        set => resource.octreeRefreshInterval = Mathf.Max(0.01f, value); // Prevent extreme values
    }

    public float timeSinceLastRefresh = 0f;
    private IOctree octree;
    public IOctree Octree { get => octree; set => octree = value; }
    public List<OctTreeElement> elements = new();
    public List<Vector3> Velocity = new();
    public List<Vector3> Acceleration = new();
    [Export] public Node3D leader;

    public void BuildOctTree(Vector3 worldCenter, int capacity)
    {
        octree = new DataOrientedOctTree(worldCenter, resource.rootOctSize, capacity,
            index => elements[index]); // Pass position lookup function
        AddChild((Node)octree);
        RefreshOctTree();
    }

    public void RefreshOctTree()
    {
        octree.Clear();
        // Create a list of indices (0 to elements.Count-1)
        List<int> elementIndices = new List<int>(elements.Count);
        for (int i = 0; i < elements.Count; i++)
        {
            elementIndices.Add(i);
        }

        octree.Insert(elementIndices);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Update boid positions and flocking behavior
        for (int index = 0; index < elements.Count; index++)
        {
            // Get neighbors from octree (using maximum of the three ranges)
            float maxRange = Mathf.Max(resource.AlignmentRange, Mathf.Max(resource.CohesionRange, resource.SeparationRange));

            List<int> neighbors = octree.Search(elements[index].Position, maxRange);
            // neighbors = filterVisible(index, neighbors);

            Vector3 alignment = Alignment(index, neighbors);
            Vector3 cohesion = Cohesion(index, neighbors);
            Vector3 separation = Separation(index, neighbors);
            Vector3 follow = Follow(index, leader.Position);
            Acceleration[index] += alignment * resource.AlignmentWeight;
            Acceleration[index] += cohesion * resource.CohesionWeight;
            Acceleration[index] += separation * resource.SeparationWeight;
            Acceleration[index] += follow * resource.FollowWeight;
            Vector3 NewPosition = elements[index].Position;
            NewPosition += Velocity[index];
            Velocity[index] += Acceleration[index];
            Velocity[index] = Velocity[index].LimitLength(resource.MaxSpeed);
            Acceleration[index] = Vector3.Zero;

            // var spaceState = GetWorld3D().DirectSpaceState;
            // var origin = elements[index].Position;
            // var end = NewPosition;
            // var query = PhysicsRayQueryParameters3D.Create(origin, end);
            // var result = spaceState.IntersectRay(query);
            // if (result.ContainsKey("collider"))
            // {
            //     // Calculate exact position adjustment using intersection point and normal
            //     Vector3 intersectionPoint = (Vector3)result["position"];
            //     Vector3 normal = (Vector3)result["normal"];
            //
            //     // Move boid out of collider along the surface normal
            //     NewPosition = intersectionPoint + normal * 0.5f; // Adjust scale based on your scene
            //
            //     // Reflect velocity using the normal
            //     float dotProduct = Velocity[index].Dot(normal);
            //     Vector3 reflection = Velocity[index] - 2 * dotProduct * normal;
            //
            //     // Maintain max speed while preserving direction
            //     Velocity[index] = reflection.Normalized() * resource.MaxSpeed;
            // }
            NewPosition = octree.WrapPosition(NewPosition);
            elements[index] = new(NewPosition, elements[index].Size);
        }
        // Update the octree only at specific intervals
        timeSinceLastRefresh += (float)delta;
        if (timeSinceLastRefresh >= resource.octreeRefreshInterval)
        {
            RefreshOctTree();
            timeSinceLastRefresh = 0f;
        }
    }
    // public List<int> filterVisible(int self, List<int> neighbors)
    // {
    //     List<int> visibleNeighbors = new();
    //     var spaceState = GetWorld3D().DirectSpaceState;
    //     for (int i = 0; i < neighbors.Count; i++)
    //     {
    //         var query = PhysicsRayQueryParameters3D.Create(elements[self].Position, elements[i].Position);
    //         var result = spaceState.IntersectRay(query);
    //         if (!result.ContainsKey("collider"))
    //         {
    //             visibleNeighbors.Add(i);
    //         }
    //     }
    //     return visibleNeighbors;
    // }

    public override void _Process(double delta)
    {
        for (int index = 0; index < elements.Count; index++)
            DebugDraw3D.DrawSquare(elements[index].Position, elements[index].Size, Colors.Black);
    }

    public void AddBoid(Vector3 Position, float Size)
    {
        elements.Add(new(Position, Size));
        Velocity.Add(new Vector3((float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5)).Normalized() * resource.MaxSpeed);
        Acceleration.Add(new(0, 0, 0));
    }
    public void RemoveBoid(int index)
    {
        elements.RemoveAt(index);
        Velocity.RemoveAt(index);
        Acceleration.RemoveAt(index);
    }

    public Vector3 Follow(int currentIndex, Vector3 leaderPosition)
    {
        // Calculate vector pointing towards the leader
        Vector3 direction = leaderPosition - elements[currentIndex].Position;
        direction.Normalized();
        return direction * resource.MaxSpeed; // Use MaxSpeed as scaling factor
    }

    public Vector3 Separation(int currentIndex, List<int> neighbors)
    {
        Vector3 Steering = Vector3.Zero;
        int total = 0;
        foreach (int neighborIndex in neighbors)
        {
            // Skip self
            if (neighborIndex == currentIndex)
                continue;

            float Distance = elements[currentIndex].Position.DistanceTo(elements[neighborIndex].Position);
            if (Distance < resource.SeparationRange)
            {
                Vector3 difference = elements[currentIndex].Position - elements[neighborIndex].Position;
                difference /= Distance;
                Steering += difference;
                total++;
            }
        }

        if (total > 0)
        {
            Steering /= total;
            Steering = Steering.Normalized() * resource.MaxSpeed;
            Steering -= Velocity[currentIndex];
            Steering = Steering.LimitLength(resource.MaxForce);
        }
        return Steering;
    }

    public Vector3 Cohesion(int currentIndex, List<int> neighbors)
    {
        Vector3 Steering = new(0f, 0f, 0f);
        int total = 0;
        foreach (int neighborIndex in neighbors)
        {
            // Skip self
            if (neighborIndex == currentIndex)
                continue;

            if (elements[currentIndex].Position.DistanceTo(elements[neighborIndex].Position) < resource.CohesionRange)
            {
                Steering += elements[neighborIndex].Position;
                total++;
            }
        }

        if (total > 0)
        {
            Steering /= total;
            Steering -= elements[currentIndex].Position;
            Steering = Steering.Normalized() * resource.MaxSpeed;
            Steering -= Velocity[currentIndex];
            Steering = Steering.LimitLength(resource.MaxForce);
        }
        return Steering;
    }

    public Vector3 Alignment(int currentIndex, List<int> neighbors)
    {
        Vector3 Steering = new(0f, 0f, 0f);
        int total = 0;
        foreach (int neighborIndex in neighbors)
        {
            // Skip self
            if (neighborIndex == currentIndex)
                continue;

            if (elements[currentIndex].Position.DistanceTo(elements[neighborIndex].Position) < resource.AlignmentRange)
            {
                Steering += Velocity[neighborIndex];
                total++;
            }
        }

        if (total > 0)
        {
            Steering /= total;
            Steering = Steering.Normalized() * resource.MaxSpeed;
            Steering -= Velocity[currentIndex];
            Steering = Steering.LimitLength(resource.MaxForce);
        }
        return Steering;
    }
}
