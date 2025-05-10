using Godot;
[GlobalClass]
public partial class OctreeResource : Resource
{
    [Export] public float RootSize = 46f;
    [Export] public int MaxElementsPerNode = 4;
}
