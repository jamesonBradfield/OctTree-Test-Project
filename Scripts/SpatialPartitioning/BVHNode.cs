using Godot;
using System;
using System.Collections.Generic;
public partial class BVHNode : Node3D, ISpatialPartitioning
{
    //position is stored in our Node3D.
    public Vector3 Size;
    public BVHNode[] Children = new BVHNode[2];
    public List<int> Elements = new List<int>();
    public bool IsLeaf;
    private Func<int, OctTreeElement> getElement;
    private int maxElementsPerLeaf = 8; // Or whatever you prefer
    public BVHNode(Vector3 Position, Vector3 Size, BVHNode[] Children, List<int> Elements, bool IsLeaf)
    {
        this.GlobalPosition = Position;
        this.Size = Size;
        this.Children = Children;
        this.Elements = Elements;
        this.IsLeaf = IsLeaf;
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public List<int> FindNearby(Vector3 position, float range)
    {
        throw new NotImplementedException();
    }

    public void Initialize(Vector3 rootPosition, float rootSize, int capacity, Func<int, OctTreeElement> getElement)
    {
        throw new NotImplementedException();
    }

    public void Insert(List<int> elementIndices)
    {
        throw new NotImplementedException();
    }

    public bool IsNearCollider(Vector3 position)
    {
        throw new NotImplementedException();
    }

    public void ToggleDebug()
    {
        throw new NotImplementedException();
    }

    public void UpdateColliderInfo()
    {
        throw new NotImplementedException();
    }

    public Vector3 WrapPosition(Vector3 position)
    {
        throw new NotImplementedException();
    }
}
