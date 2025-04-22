using Godot;
using System.Collections.Generic;

public partial class OctTreeTestSceneSetup : Node3D
{
    Aabb rootOctAabb = new(new(0, 0, 0), new(100f, 100f, 100f));
    Vector3 boidSize = new(2.5f, 2.5f, 2.5f);
    List<Boid> boids = new List<Boid>();
    OctTree ot;
    private float regenerateTimer = 0;
    private const float REGENERATE_INTERVAL = 0.5f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        DebugDraw3D.ScopedConfig().SetThickness(.4f);
        ot = new OctTree(rootOctAabb, 8);
        AddChild(ot);
        for (int i = 0; i < 50; i++)
        {
            Vector3 position = new((float)GD.RandRange((rootOctAabb.Position.X), (rootOctAabb.Position.X + rootOctAabb.Size.X) - boidSize.X), (float)GD.RandRange((rootOctAabb.Position.Y), (rootOctAabb.Position.Y + rootOctAabb.Size.Y) - boidSize.Y), (float)GD.RandRange((rootOctAabb.Position.Z), (rootOctAabb.Position.Z + rootOctAabb.Size.Z)) - boidSize.Z);
            Boid boid = new(new(position, boidSize), rootOctAabb);
            boids.Add(boid);
            ot.Insert(boid.aabb);
            GD.Print("[boid] " + i + " spawned at " + boid.aabb.Position);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        foreach (Boid boid in boids)
        {
            boid.Edges();
            boid.Flock(boids);
            boid.Update(delta);
            boid.Draw();
        }
        regenerateTimer += (float)delta;
        if (regenerateTimer >= REGENERATE_INTERVAL)
        {
            GD.Print("OctTree should Regenerate");
            ot.Regenerate();
            foreach (Boid boid in boids)
            {
                boid.aabb = new Aabb(boid.aabb.Position, boidSize);
                ot.Insert(boid.aabb);
            }
            regenerateTimer = 0;
        }
    }

}
