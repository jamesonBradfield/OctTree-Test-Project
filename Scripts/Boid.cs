using Godot;
using System.Collections.Generic;

public partial class Boid : Node3D
{
    Vector3 Velocity;
    Vector3 Acceleration;
    Vector3 Size;
    float AlignmentRange = 10;
    float CohesionRange = 7.5f;
    float SeparationRange = 2.5f;
    float MaxForce = .2f;
    float MaxSpeed = .8f;

    public Boid(Vector3 Size)
    {
        MeshInstance3D mesh = new MeshInstance3D();
        Velocity = new Vector3((float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5)).Normalized() * MaxSpeed;
        AddChild(mesh);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        DebugDraw3D.DrawSphere(Position, Size.X / 2f);
    }
    // NOTE: needs rework
    //
    // public void Edges()
    // {
    //     if (Position.X > Size.Position.X + (Size.Size.X / 2))
    //         Position = new(Size.Position.X - (Size.Size.X / 2), Position.Y, Position.Z);
    //     else if (Position.X < Size.Position.X - (Size.Size.X / 2))
    //         Position = new(Size.Position.X + (Size.Size.X / 2), Position.Y, Position.Z);
    //     if (Position.Y > Size.Position.Y + (Size.Size.Y / 2))
    //         Position = new(Position.X, Size.Position.Y - (Size.Size.Y / 2), Position.Z);
    //     else if (Position.Y < Size.Position.Y - (Size.Size.Y / 2))
    //         Position = new(Position.X, Size.Position.Y + (Size.Size.Y / 2), Position.Z);
    //     if (Position.Z > Size.Position.Z + (Size.Size.Y / 2))
    //         Position = new(Position.X, Position.Y, Size.Position.Z - (Size.Size.Y / 2));
    //     else if (Position.Z < -Size.Position.Z - (Size.Size.Y / 2))
    //         Position = new(Position.X, Position.Y, Size.Position.Z + (Size.Size.Y / 2));
    // }
    public void Update(double delta)
    {
        Position += Velocity;
        Velocity += (Acceleration * (float)delta);
        Velocity = Velocity.LimitLength(MaxSpeed);
        Acceleration = Vector3.Zero;
    }
    public void Flock(List<Boid> boids)
    {
        Vector3 alignment = Alignment(boids);
        Vector3 cohesion = Cohesion(boids);
        Vector3 separation = Separation(boids);
        Acceleration += alignment;
        Acceleration += cohesion;
        Acceleration += separation;
    }

    private Vector3 Separation(List<Boid> boids)
    {
        Vector3 Steering = Vector3.Zero;
        int total = 0;
        for (int index = 0; index < boids.Count; index++)
        {
            float Distance = Position.DistanceTo(boids[index].Position);
            if (boids[index] != this && Distance < SeparationRange)
            {
                Vector3 difference = Position - boids[index].Position;
                difference /= Distance;
                Steering += difference;
                total++;
            }
        }
        if (total > 0)
        {
            Steering /= total;
            Steering = Steering.Normalized() * MaxSpeed;
            Steering -= Velocity;
            Steering.LimitLength(MaxForce);
        }
        return Steering;
    }
    private Vector3 Cohesion(List<Boid> boids)
    {
        Vector3 Steering = new(0f, 0f, 0f);
        int total = 0;
        for (int index = 0; index < boids.Count; index++)
        {
            if (boids[index] != this && Position.DistanceTo(boids[index].Position) < CohesionRange)
            {
                Steering += boids[index].Position;
                total++;
            }
        }
        if (total > 0)
        {
            Steering /= total;
            Steering -= Position;
            Steering = Steering.Normalized() * MaxSpeed;
            Steering -= Velocity;
            Steering.LimitLength(MaxForce);
        }
        return Steering;
    }

    private Vector3 Alignment(List<Boid> boids)
    {
        Vector3 Steering = new(0f, 0f, 0f);
        int total = 0;
        for (int index = 0; index < boids.Count; index++)
        {
            if (boids[index] != this && Position.DistanceTo(boids[index].Position) < AlignmentRange)
            {
                Steering += boids[index].Velocity;
                total++;
            }
        }
        if (total > 0)
        {
            Steering /= total;
            Steering = Steering.Normalized() * MaxSpeed;
            Steering -= Velocity;
            Steering.LimitLength(MaxForce);
        }
        return Steering;
    }
}
