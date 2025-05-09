public struct OctNode
{
    // we can assume every subdivided node has 8 children. -1 if a leaf.
    public int firstChildIndex;
    public int elementCount;
    public int firstElementIndex;
    // NOTE: this will be used later on to optimize boid-collision raycasts (IE only having boids in the octNode with this set to true will raycast for collisions)
    public bool hasCollider;
    public OctNode(int firstChildIndex, int firstElementIndex, int elementCount, bool hasCollider)
    {
        this.firstChildIndex = firstChildIndex;
        this.firstElementIndex = firstElementIndex;
        this.elementCount = elementCount;
        this.hasCollider = hasCollider;
    }
}
