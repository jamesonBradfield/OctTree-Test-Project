using Godot;
using System.Collections.Generic;
public partial class BoidManager : Node
{
    public float AlignmentRange = 10;
    public float CohesionRange = 7.5f;
    public float SeparationRange = 2.5f;
    public float MaxForce = 1f;
    public float MaxSpeed = .1f;
    public float AlignmentWeight = .8f;
    public float CohesionWeight = .8f;
    public float SeparationWeight = .5f;
    public List<OctTreeElement> elements = new();
    public List<Vector3> Velocity = new();
    public List<Vector3> Acceleration = new();
    public int AddBoid(Vector3 Position, float Size)
    {
        elements.Add(new(Position, Size));
        Velocity.Add(new Vector3((float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5), (float)GD.RandRange(-1.5, 1.5)).Normalized() * MaxSpeed);
        Acceleration.Add(new(0, 0, 0));
        return elements.Count - 1;
    }
    public void RemoveBoid(int index)
    {
        elements.RemoveAt(index);
        Velocity.RemoveAt(index);
        Acceleration.RemoveAt(index);
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        for (int index = 0; index < elements.Count; index++)
        {
            DebugDraw3D.DrawSquare(elements[index].Position, elements[index].Size, Colors.Black);
        }
    }

    public void Edges()
    {
        // if (Position.X > Size.Position.X + (Size.Size.X / 2))
        //     Position = new(Size.Position.X - (Size.Size.X / 2), Position.Y, Position.Z);
        // else if (Position.X < Size.Position.X - (Size.Size.X / 2))
        //     Position = new(Size.Position.X + (Size.Size.X / 2), Position.Y, Position.Z);
        // if (Position.Y > Size.Position.Y + (Size.Size.Y / 2))
        //     Position = new(Position.X, Size.Position.Y - (Size.Size.Y / 2), Position.Z);
        // else if (Position.Y < Size.Position.Y - (Size.Size.Y / 2))
        //     Position = new(Position.X, Size.Position.Y + (Size.Size.Y / 2), Position.Z);
        // if (Position.Z > Size.Position.Z + (Size.Size.Y / 2))
        //     Position = new(Position.X, Position.Y, Size.Position.Z - (Size.Size.Y / 2));
        // else if (Position.Z < -Size.Position.Z - (Size.Size.Y / 2))
        //     Position = new(Position.X, Position.Y, Size.Position.Z + (Size.Size.Y / 2));
    }
    public void Update(double delta)
    {
        for (int index = 0; index < elements.Count; index++)
        {
            float Size = elements[index].Size;
            Vector3 NewPosition = elements[index].Position;
            NewPosition += Velocity[index];
            Velocity[index] += (Acceleration[index] * (float)delta);
            Velocity[index] = Velocity[index].LimitLength(MaxSpeed);
            Acceleration[index] = Vector3.Zero;
            elements[index] = new(NewPosition, Size);
        }
    }

    public void Flock(int index, List<int> neighbors)
    {
        Vector3 alignment = Alignment(index, neighbors);
        Vector3 cohesion = Cohesion(index, neighbors);
        Vector3 separation = Separation(index, neighbors);
        Acceleration[index] += alignment * AlignmentWeight;
        Acceleration[index] += cohesion * CohesionWeight;
        Acceleration[index] += separation * SeparationWeight;
    }

    private Vector3 Separation(int currentIndex, List<int> neighbors)
    {
        Vector3 Steering = Vector3.Zero;
        int total = 0;
        for (int index = 0; index < neighbors.Count; index++)
        {
            float Distance = elements[currentIndex].Position.DistanceTo(elements[neighbors[index]].Position);
            if (index != currentIndex && Distance < SeparationRange)
            {
                Vector3 difference = elements[currentIndex].Position - elements[neighbors[index]].Position;
                difference /= Distance;
                Steering += difference;
                total++;
            }
        }
        if (total > 0)
        {
            Steering /= total;
            Steering = Steering.Normalized() * MaxSpeed;
            Steering -= Velocity[currentIndex];
            Steering.LimitLength(MaxForce);
        }
        return Steering;
    }

    private Vector3 Cohesion(int currentIndex, List<int> neighbors)
    {
        Vector3 Steering = new(0f, 0f, 0f);
        int total = 0;
        for (int index = 0; index < neighbors.Count; index++)
        {
            if (index != currentIndex && elements[currentIndex].Position.DistanceTo(elements[neighbors[index]].Position) < CohesionRange)
            {
                Steering += elements[neighbors[index]].Position;
                total++;
            }
        }
        if (total > 0)
        {
            Steering /= total;
            Steering -= elements[currentIndex].Position;
            Steering = Steering.Normalized() * MaxSpeed;
            Steering -= Velocity[currentIndex];
            Steering.LimitLength(MaxForce);
        }
        return Steering;
    }

    private Vector3 Alignment(int currentIndex, List<int> neighbors)
    {
        Vector3 Steering = new(0f, 0f, 0f);
        int total = 0;
        for (int index = 0; index < neighbors.Count; index++)
        {
            if (index != currentIndex && elements[currentIndex].Position.DistanceTo(elements[neighbors[index]].Position) < AlignmentRange)
            {
                Steering += Velocity[neighbors[index]];
                total++;
            }
        }
        if (total > 0)
        {
            Steering /= total;
            Steering = Steering.Normalized() * MaxSpeed;
            Steering -= Velocity[currentIndex];
            Steering.LimitLength(MaxForce);
        }
        return Steering;
    }
}
