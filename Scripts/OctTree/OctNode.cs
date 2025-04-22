using Godot;
public class OctNode
{
    public Aabb bounds;
    // -1 if its not a leaf, otherwise its holding data
    public int count;

    //constructor for not-leaf nodes
    public OctNode(float X, float Y, float Z, Vector3 Size, int count)
    {
        this.bounds = new(new(X, Y, Z), Size);
        this.count = count;
    }
    public OctNode(Vector3 Pos, Vector3 Size, int count)
    {
        this.bounds = new(Pos, Size);
        this.count = count;
    }
    //constructor for not-leaf nodes
    public OctNode(Aabb bounds)
    {
        this.bounds = bounds;
        this.count = -1;
    }

    public OctNode(Aabb bounds, int count)
    {
        this.bounds = bounds;
        this.count = count;
    }
}
