public struct OctNode
{
    // we can assume every subdivided node has 8 children. -1 if a leaf.
    public int firstChildIndex;
    public int elementCount;
    public int firstElementIndex;
    // NOTE: this will be used now to optimize boid-collision raycasts
    public bool hasCollider;

    public OctNode(int firstChildIndex, int firstElementIndex, int elementCount, bool hasCollider)
    {
        this.firstChildIndex = firstChildIndex;
        this.firstElementIndex = firstElementIndex;
        this.elementCount = elementCount;
        this.hasCollider = hasCollider;
    }
}
