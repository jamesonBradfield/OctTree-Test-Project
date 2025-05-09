using Godot;

public partial class OctTreeTestSceneSetup : Node3D
{
    BoidManager boidManager;
    public override void _Ready()
    {
        boidManager = GetNode<BoidManager>("BoidManager");
        RestartSimulation();
    }
    public void RestartSimulation()
    {
        if (boidManager.elements.Count != 0)
            boidManager.elements.Clear();
        if (boidManager.Octree != null)
            boidManager.Octree.Clear();
        for (int i = 0; i < boidManager.resource.count; i++)
        {
            // GD.Print($"boidManager.resource.rootOctSize set to {boidManager.resource.rootOctSize}");
            Vector3 position = new((float)GD.RandRange((Position.X - (boidManager.resource.rootOctSize / 2f)), (Position.X + (boidManager.resource.rootOctSize / 2f))), (float)GD.RandRange((Position.Y - (boidManager.resource.rootOctSize / 2f)), (Position.Y + (boidManager.resource.rootOctSize / 2f))), (float)GD.RandRange((Position.Z - (boidManager.resource.rootOctSize / 2)), (Position.Z + (boidManager.resource.rootOctSize / 2))));
            // GD.Print($"position generated at {position} in OctTreeTestSceneSetup");
            boidManager.AddBoid(position, 1.0f);
        }
        boidManager.BuildOctTree(Position, 4);
    }
}
