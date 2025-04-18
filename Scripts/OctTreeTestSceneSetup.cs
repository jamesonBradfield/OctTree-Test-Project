using Godot;

public partial class OctTreeTestSceneSetup : Node3D
{
    Vector3 rootOctSize = new(200f, 200f, 200f);
    OctTree ot;
    [Export] Mesh mesh;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ot = new OctTree(Position, rootOctSize, 4);
        AddChild(ot);
        ot.Name = "OctTree";
        for (int i = 0; i < 50; i++)
        {
            Boid boid = new(rootOctSize, mesh);
            AddChild(boid);
            Vector3 position = new((float)GD.RandRange((Position.X - (rootOctSize.X / 2)), (Position.X + (rootOctSize.X / 2))), (float)GD.RandRange((Position.Y - (rootOctSize.Y / 2)), (Position.Y + (rootOctSize.Y / 2))), (float)GD.RandRange((Position.Z - (rootOctSize.X / 2)), (Position.Z + (rootOctSize.X / 2))));
            ot.Insert(boid);
            boid.Position = position;
            boid.Name = "boid" + i;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
