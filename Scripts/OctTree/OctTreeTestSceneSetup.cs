using Godot;

public partial class OctTreeTestSceneSetup : Node3D
{
    Vector3 rootOctSize = new(100f, 100f, 100f);
    OctTree ot;
    BoidManager boidManager;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ot = new OctTree(Position, rootOctSize, 4);
        AddChild(ot);
        boidManager = GetNode<BoidManager>("/root/BoidManagerGlobal");
        for (int i = 0; i < 40; i++)
        {
            Vector3 position = new((float)GD.RandRange((Position.X - (rootOctSize.X / 2)), (Position.X + (rootOctSize.X / 2))), (float)GD.RandRange((Position.Y - (rootOctSize.Y / 2)), (Position.Y + (rootOctSize.Y / 2))), (float)GD.RandRange((Position.Z - (rootOctSize.X / 2)), (Position.Z + (rootOctSize.X / 2))));
            int index = boidManager.AddBoid(position, 1.0f);
            ot.Insert(boidManager.elements[index]);
        }
    }
    public override void _Process(double delta)
    {
        for (int i = 0; i < boidManager.elements.Count; i++)
        {
            // boidManager.Flock(i,); // NOTE: need to be able to find a boids index in OctTree search function.
            boidManager.Update(delta);
        }

    }
}
