using Godot;
using System.Collections.Generic;

public partial class BoidTestSceneSetup : Node3D
{
    [Export] int count = 50;
    [Export] Vector3 bounds;
    [Export] Mesh mesh;
    List<Boid> boids = new List<Boid>();
    public override void _Ready()
    {
        for (int i = 0; i <= count; i++)
        {
            Vector3 position = new Vector3((float)GD.RandRange(-bounds.X, bounds.X), (float)GD.RandRange(-bounds.Y, bounds.Y), (float)GD.RandRange(-bounds.Z, bounds.Z));
            Boid newBoid = new(bounds, mesh);
            boids.Add(newBoid);
            this.AddChild(newBoid);
            newBoid.Position = position;
        }
    }
    public override void _Process(double delta)
    {
        foreach (Boid boid in boids)
        {
            boid.Edges();
            boid.Flock(boids);
            boid.Update(delta);
        }
    }
}
