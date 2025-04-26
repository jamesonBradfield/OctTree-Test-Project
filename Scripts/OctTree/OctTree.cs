using System.Collections.Generic;
using Godot;

public partial class OctTree : Node3D
{
    Vector3 size { get; set; }
    List<OctTreeElement> elements { get; set; } = new();
    int capacity { get; set; }
    List<OctTree> children { get; set; } = new();

    public OctTree(Vector3 position, Vector3 size, int capacity)
    {
        Position = position;
        this.size = size;
        this.capacity = capacity;
        Name = "OctTree";
    }

    public void Insert(OctTreeElement element)
    {
        if (!Contains(element))
            return;

        if (elements.Count < capacity)
        {
            elements.Add(element);
        }
        else
        {
            if (children.Count != 0)
                Subdivide();

            foreach (var child in children)
                child.Insert(element);
        }
    }

    public override void _Process(double delta)
    {
        DebugDraw3D.DrawBox(Position, Quaternion.Identity, size, Colors.Aqua, true);
    }

    private void Subdivide()
    {
        var newSize = size / 2f;
        for (int x = -1; x < 2; x += 2)
        {
            for (int y = -1; y < 2; y += 2)
            {
                for (int z = -1; z < 2; z += 2)
                {
                    var childPos = new Vector3(
                        Position.X + (x * newSize.X / 2f),
                        Position.Y + (y * newSize.Y / 2f),
                        Position.Z + (z * newSize.Z / 2f)
                    );
                    var child = new OctTree(childPos, newSize, capacity);
                    AddChild(child);
                    child.Name = "OctTree";
                    children.Add(child);
                    elements.Clear();
                }
            }
        }
    }
    // NOTE: we have a problem where we can't return a list of ints like we hoped,
    // this stems from the OctTree and our BoidManager not using a unified data structure as reference for elements.
    public List<OctTreeElement> Search(Vector3 center, float radius)
    {
        List<OctTreeElement> returnIndices = new();
        if (IsInSearch(center, radius))
        {
            if (children.Count != 0)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    returnIndices.AddRange(children[i].Search(center, radius));
                }
            }
            else
            {
                // check if elements are in our searchRadius
                for (int i = 0; i < elements.Count; i++)
                {
                    if (ElementIsInSearch(center, radius, elements[i].Position))
                    {
                        returnIndices.Add(elements[i]);
                    }
                }
            }
        }
        return returnIndices;
    }
    // this should return true if any part of our search radius intersects with our OctTree, else false.
    public bool IsInSearch(Vector3 center, float radius)
    {
        return (center.X + radius < Position.X - size.X &&
               center.X - radius > Position.X + size.X &&
               center.Y + radius < Position.Y - size.Y &&
               center.Y - radius > Position.Y + size.Y &&
               center.Z + radius < Position.Z - size.Z &&
               center.Z - radius > Position.Z + size.Z);
    }
    // we will assume if the elements Position is in our OctTree the element also is.
    public bool ElementIsInSearch(Vector3 center, float radius, Vector3 elementPosition)
    {
        return (center.X + radius < elementPosition.X &&
                center.X - radius > elementPosition.X &&
                center.Y + radius < elementPosition.Y &&
                center.Y - radius > elementPosition.Y &&
                center.Z + radius < elementPosition.Z &&
                center.Z - radius > elementPosition.Z);
    }

    public bool Contains(OctTreeElement element)
    {
        return (element.Position.X > Position.X - size.X &&
                element.Position.X < Position.X + size.X &&
                element.Position.Y > Position.Y - size.Y &&
                element.Position.Y < Position.Y + size.Y &&
                element.Position.Z > Position.Z - size.Z &&
                element.Position.Z < Position.Z + size.Z);
    }
}

