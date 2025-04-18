using Godot;
using System.Collections.Generic;
public partial class OctTree : Node3D
{
    Vector3 Size;
    List<Boid> boids = new List<Boid>();
    OctTree ufl, ufr, ubl, ubr, dfl, dfr, dbl, dbr;
    int Capacity;
    bool divided;

    public OctTree(Vector3 Position, Vector3 Size, int Capacity)
    {
        this.Position = Position;
        this.Size = Size;
        this.Capacity = Capacity;
        MeshInstance3D meshInstance = new();
        BoxMesh boxMesh = new BoxMesh();
        boxMesh.Size = Size;
        meshInstance.Mesh = boxMesh;
        AddChild(meshInstance);
        this.Position = Position;
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

    public void Subdivide()
    {
        Vector3 newPos = Position;
        Vector3 newSize = Size / 2f;
        Vector3 uflPos = new(newPos.X + newSize.X, newPos.Y + newSize.Y, newPos.Z + newSize.Z);
        Vector3 ufrPos = new(newPos.X - newSize.X, newPos.Y + newSize.Y, newPos.Z + newSize.Z);
        Vector3 ublPos = new(newPos.X + newSize.X, newPos.Y + newSize.Y, newPos.Z - newSize.Z);
        Vector3 ubrPos = new(newPos.X - newSize.X, newPos.Y + newSize.Y, newPos.Z - newSize.Z);
        Vector3 dflPos = new(newPos.X + newSize.X, newPos.Y - newSize.Y, newPos.Z + newSize.Z);
        Vector3 dfrPos = new(newPos.X - newSize.X, newPos.Y - newSize.Y, newPos.Z + newSize.Z);
        Vector3 dblPos = new(newPos.X + newSize.X, newPos.Y - newSize.Y, newPos.Z - newSize.Z);
        Vector3 dbrPos = new(newPos.X - newSize.X, newPos.Y - newSize.Y, newPos.Z - newSize.Z);
        ufl = new(uflPos, newSize, Capacity);
        ufr = new(ufrPos, newSize, Capacity);
        ubl = new(ublPos, newSize, Capacity);
        ubr = new(ubrPos, newSize, Capacity);
        dfl = new(dflPos, newSize, Capacity);
        dfr = new(dfrPos, newSize, Capacity);
        dbl = new(dblPos, newSize, Capacity);
        dbr = new(dbrPos, newSize, Capacity);
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
