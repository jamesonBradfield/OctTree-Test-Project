using Godot;
public struct Rectangle
{
    public Vector3 Position;
    public Vector3 Size;
    public Rectangle(Vector3 Position, Vector3 Size)
    {
        this.Position = Position;
        this.Size = Size;
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
