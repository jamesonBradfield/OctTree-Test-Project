using Godot;
using System.Collections.Generic;

public partial class BoidTestSceneSetup : Node3D
{
    [Export] int count = 50;
    Vector3 Size = new(200f, 200f, 200f);
    List<Boid> boids = new List<Boid>();
    public override void _Ready()
    {
        for (int i = 0; i <= count; i++)
        {
            Vector3 position = new((float)GD.RandRange((double)(Position.X - (Size.X / 2)), (double)(Position.X + (Size.X / 2))), (float)GD.RandRange((double)(Position.Y - (Size.Y / 2)), (double)(Position.Y + (Size.Y / 2))), (float)GD.RandRange((double)(Position.Z - (Size.X / 2)), (double)(Position.Z + (Size.X / 2))));
            Boid newBoid = new(Size);
            boids.Add(newBoid);
            this.AddChild(newBoid);
            newBoid.Position = position;
            GD.Print(newBoid.Position);
        }
    }
    public override void _Process(double delta)
    {
        foreach (Boid boid in boids)
        {
            // boid.Edges();
            boid.Flock(boids);
            boid.Update(delta);
        }
    }
}
