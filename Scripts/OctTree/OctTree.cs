using Godot;
using System.Collections.Generic;
public partial class OctTree : Node3D
{
    Vector3 Size;
    List<Boid> boids = new List<Boid>();
    int Capacity;
    bool divided;

    public OctTree(Vector3 Position, Vector3 Size, int Capacity)
    {
        this.Position = Position;
        this.Size = Size;
        this.Capacity = Capacity;
        this.Name = "OctTree";
        this.Position = Position;
        GD.Print(Name + "Position : " + Position + "Size : " + Size);
    }

    public void Insert(Boid boid)
    {
        if (!Contains(boid))
            return;
        if (boids.Count < this.Capacity)
            boids.Add(boid);
        else
        {
            if (!divided)
                this.Subdivide();
            ufl.Insert(boid);
            ufr.Insert(boid);
            ubl.Insert(boid);
            ubr.Insert(boid);
            dfl.Insert(boid);
            dfr.Insert(boid);
            dbl.Insert(boid);
            dbr.Insert(boid);
        }
    }
    public override void _Process(double delta)
    {
        DebugDraw3D.DrawBox(Position, Quaternion.Identity, Size);
    }

    public void Subdivide()
    {
        Vector3 uflPos = new(Size.X, Size.Y, Size.Z);
        Vector3 ufrPos = new(-Size.X, Size.Y, Size.Z);
        Vector3 ublPos = new(Size.X, Size.Y, -Size.Z);
        Vector3 ubrPos = new(-Size.X, Size.Y, -Size.Z);
        Vector3 dflPos = new(Size.X, -Size.Y, Size.Z);
        Vector3 dfrPos = new(-Size.X, -Size.Y, Size.Z);
        Vector3 dblPos = new(Size.X, -Size.Y, -Size.Z);
        Vector3 dbrPos = new(-Size.X, -Size.Y, -Size.Z);
        ufl = new(uflPos, Size, Capacity);
        ufr = new(ufrPos, Size, Capacity);
        ubl = new(ublPos, Size, Capacity);
        ubr = new(ubrPos, Size, Capacity);
        dfl = new(dflPos, Size, Capacity);
        dfr = new(dfrPos, Size, Capacity);
        dbl = new(dblPos, Size, Capacity);
        dbr = new(dbrPos, Size, Capacity);
        AddChild(ufl);
        AddChild(ufr);
        AddChild(ubl);
        AddChild(ubr);
        AddChild(dfl);
        AddChild(dfr);
        AddChild(dbl);
        AddChild(dbr);
        ufl.Name = "OctTree" + 0;
        ufr.Name = "OctTree" + 1;
        ubl.Name = "OctTree" + 2;
        ubr.Name = "OctTree" + 3;
        dfl.Name = "OctTree" + 4;
        dfr.Name = "OctTree" + 5;
        dbl.Name = "OctTree" + 6;
        dbr.Name = "OctTree" + 7;
        divided = true;
    }

    public bool Contains(Boid boid)
    {
        return (boid.Position.X > this.Position.X - this.Size.X &&
                boid.Position.X < this.Position.X + this.Size.X &&
                boid.Position.Y > this.Position.Y - this.Size.Y &&
                boid.Position.Y < this.Position.Y + this.Size.Y &&
                boid.Position.Z > this.Position.Z - this.Size.Z &&
                boid.Position.Z < this.Position.Z + this.Size.Z);
    }
}
