using Godot;
using System.Collections.Generic;

public class Boid
{
    Vector3 Velocity;
    Vector3 Acceleration;
    public Aabb aabb;
    public Aabb rootOctAabb;
    float AlignmentRange = 10;
    float CohesionRange = 7.5f;
    float SeparationRange = 2.5f;
    float MaxForce = .2f;
    float MaxSpeed = .8f;

    public Boid(Aabb aabb, Aabb rootOctAabb)
    {
        this.aabb = aabb;
        Velocity = new Vector3((float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5)).Normalized() * MaxSpeed;
        this.rootOctAabb = rootOctAabb;
    }

    public void Edges()
    {
        if (aabb.Position.X > rootOctAabb.Position.X + rootOctAabb.Size.X)
            aabb.Position = new(rootOctAabb.Position.X, aabb.Position.Y, aabb.Position.Z);
        else if (aabb.Position.X < rootOctAabb.Position.X)
            aabb.Position = new(rootOctAabb.Position.X + rootOctAabb.Size.X, aabb.Position.Y, aabb.Position.Z);
        if (aabb.Position.Y > rootOctAabb.Position.Y + rootOctAabb.Size.Y)
            aabb.Position = new(aabb.Position.X, rootOctAabb.Position.Y, aabb.Position.Z);
        else if (aabb.Position.Y < rootOctAabb.Position.Y)
            aabb.Position = new(aabb.Position.X, rootOctAabb.Position.Y + rootOctAabb.Size.Y, aabb.Position.Z);
        if (aabb.Position.Z > rootOctAabb.Position.Z + rootOctAabb.Size.Z)
            aabb.Position = new(aabb.Position.X, aabb.Position.Y, rootOctAabb.Position.Z);
        else if (aabb.Position.Z < rootOctAabb.Position.Z)
            aabb.Position = new(aabb.Position.X, aabb.Position.Y, rootOctAabb.Position.Z);
    }

    public void Update(double delta)
    {
        aabb.Position += Velocity;
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

    public void Draw()
    {
        DebugDraw3D.DrawAabb(aabb);
    }

    private Vector3 Separation(List<Boid> boids)
    {
        Vector3 Steering = Vector3.Zero;
        int total = 0;
        for (int index = 0; index < boids.Count; index++)
        {
            float Distance = aabb.Position.DistanceTo(boids[index].aabb.Position);
            if (boids[index] != this && Distance < SeparationRange)
            {
                Vector3 difference = aabb.Position - boids[index].aabb.Position;
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
            if (boids[index] != this && aabb.Position.DistanceTo(boids[index].aabb.Position) < CohesionRange)
            {
                Steering += boids[index].aabb.Position;
                total++;
            }
        }
        if (total > 0)
        {
            Steering /= total;
            Steering -= aabb.Position;
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
            if (boids[index] != this && aabb.Position.DistanceTo(boids[index].aabb.Position) < AlignmentRange)
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
