using Godot;

public struct OctTreeElement
{
    public Vector3 Position;
    public float Size;
    public OctTreeElement(Vector3 Position, float Size)
    {
        this.Position = Position;
        this.Size = Size;
    }
}
