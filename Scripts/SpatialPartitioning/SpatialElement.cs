using Godot;

public class SpatialElement : ICollider
{
    public Vector3 Position;
    public Vector3 Size;

    private Aabb cachedAabb;
    private Vector3 cachedPosition;

    public bool IsNearCollider { get; set; } = false;
    public SpatialElement(Vector3 Position, Vector3 Size, bool IsNearCollider)
    {
        this.Position = Position;
        this.Size = Size;
        this.IsNearCollider = IsNearCollider;
    }

    public void UpdateAabb()
    {
        cachedPosition = Position;

        // Create AABB around the element with its size
        Vector3 extents = Size / 2f;
        cachedAabb = new Aabb(cachedPosition - extents, extents * 2);
    }

    public Aabb GetAabb()
    {
        // Check if position has changed
        if (Position != cachedPosition)
        {
            UpdateAabb();
        }
        return cachedAabb;
    }
}
