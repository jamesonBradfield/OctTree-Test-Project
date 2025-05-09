using System.Collections.Generic;
using Godot;

public partial class DataOrientedOctTree : Node, IOctree
{
    // Pre-allocated lists with cached positions and sizes
    private List<Vector3> nodePositions = new List<Vector3>();
    private List<float> nodeSizes = new List<float>();
    private List<OctNode> nodes = new List<OctNode>();
    private List<int> elementIndices = new List<int>();

    // Reusable collections to minimize GC pressure
    private List<int> tempElementIndices = new List<int>();
    private List<int>[] octantElements = new List<int>[8]; // For RedistributeElements
    private Dictionary<int, int> indexMap = new Dictionary<int, int>(); // For RedistributeElements

    // Reusable collections for traversal
    private Stack<NodeSearchInfo> searchStack = new Stack<NodeSearchInfo>();
    private Stack<NodeDrawInfo> drawStack = new Stack<NodeDrawInfo>();
    private List<int> searchResults = new List<int>();

    // Configuration
    private System.Func<int, OctTreeElement> getBoid;
    private int capacity;
    private bool debug;

    // Cache color array to avoid creating new colors each frame
    // These are static colors that won't change
    Color[] levelColors = new Color[]
    {
                new Color(0, 0, 1, 0.2f),   // Blue
                new Color(0, 1, 0, 0.2f),   // Green
                new Color(1, 0, 0, 0.2f),   // Red
                new Color(1, 1, 0, 0.2f),   // Yellow
                new Color(0, 1, 1, 0.2f),   // Cyan
                new Color(1, 0, 1, 0.2f),   // Magenta
                new Color(1, 0.5f, 0, 0.2f), // Orange
                new Color(0.5f, 0, 0.5f, 0.2f) // Purple
    };
    // Add to DataOrientedOctTree.cs
    public override void _Process(double delta)
    {
        if (debug)
        {
            DrawOctree();
        }
    }

    private void DrawOctree()
    {
        using (var _w1 = DebugDraw3D.NewScopedConfig().SetThickness(0.25f))
        {
            // Reuse the draw stack
            drawStack.Clear();

            // Push root node to stack
            drawStack.Push(new NodeDrawInfo(0, nodePositions[0], nodeSizes[0]));

            // Iterative traversal
            while (drawStack.Count > 0)
            {
                // Get current node from stack
                NodeDrawInfo current = drawStack.Pop();
                int nodeIndex = current.nodeIndex;
                Vector3 nodePos = current.position;
                float nodeSize = current.size;
                int depth = current.depth;

                // Skip if node index is invalid
                if (nodeIndex >= nodes.Count)
                    continue;

                OctNode node = nodes[nodeIndex];

                // Draw the current node
                Color color = levelColors[depth % levelColors.Length];

                // If it's a leaf node with elements, make it more visible
                if (node.firstChildIndex == -1 && node.elementCount > 0)
                {
                    // Create a modified color without allocating a new Color
                    Color leafColor = color;
                    leafColor.A = 0.4f; // More opaque
                    color = leafColor;
                }

                // Draw the box for this node
                DebugDraw3D.DrawBox(
                    nodePos,                                   // Position
                    Quaternion.Identity,                       // Rotation
                    new Vector3(nodeSize, nodeSize, nodeSize), // Size
                    color,                                     // Color
                    true,                                      // Is box centered
                    0                                          // Duration (one frame)
                );

                // If internal node, push all children to stack
                if (node.firstChildIndex >= 0)
                {
                    float childSize = nodeSize / 2;
                    for (int octant = 0; octant < 8; octant++)
                    {
                        int childIndex = node.firstChildIndex + octant;

                        // Use cached position if available, otherwise calculate
                        Vector3 childPos;
                        if (childIndex < nodePositions.Count)
                        {
                            childPos = nodePositions[childIndex];
                        }
                        else
                        {
                            // Calculate child position if not cached
                            childPos = CalculateChildPosition(nodePos, nodeSize, octant);
                        }

                        drawStack.Push(new NodeDrawInfo(childIndex, childPos, childSize, depth + 1));
                    }
                }
            }
        }
    }

    private struct NodeDrawInfo
    {
        public int nodeIndex;
        public Vector3 position;
        public float size;
        public int depth;

        public NodeDrawInfo(int nodeIndex, Vector3 position, float size, int depth = 0)
        {
            this.nodeIndex = nodeIndex;
            this.position = position;
            this.size = size;
            this.depth = depth;
        }
    }

    public void ToggleDebug()
    {
        debug = !debug;
    }

    // Constructor with pre-allocation
    public DataOrientedOctTree(Vector3 rootPosition, float rootSize, int capacity, System.Func<int, OctTreeElement> getBoid, int estimatedNodes = 1000)
    {
        this.capacity = capacity;
        this.getBoid = getBoid;

        // Pre-allocate collections based on estimated size
        nodes.Capacity = estimatedNodes;
        nodePositions.Capacity = estimatedNodes;
        nodeSizes.Capacity = estimatedNodes;
        elementIndices.Capacity = estimatedNodes * capacity;
        tempElementIndices.Capacity = estimatedNodes * capacity;
        searchResults.Capacity = capacity * 10;

        // Initialize octant element lists
        for (int i = 0; i < 8; i++)
        {
            octantElements[i] = new List<int>(capacity);
        }

        // Create the root node
        nodes.Add(new OctNode(-1, -1, 0, false));
        nodePositions.Add(rootPosition);
        nodeSizes.Add(rootSize);
    }

    public Vector3 WrapPosition(Vector3 position)
    {
        // Calculate min and max boundaries based on centered boxes
        float halfSize = nodeSizes[0] / 2;
        float minX = nodePositions[0].X - halfSize;
        float maxX = nodePositions[0].X + halfSize;
        float minY = nodePositions[0].Y - halfSize;
        float maxY = nodePositions[0].Y + halfSize;
        float minZ = nodePositions[0].Z - halfSize;
        float maxZ = nodePositions[0].Z + halfSize;

        // Wrap X
        if (position.X > maxX)
            position.X = minX + (position.X - maxX);
        else if (position.X < minX)
            position.X = maxX - (minX - position.X);

        // Wrap Y
        if (position.Y > maxY)
            position.Y = minY + (position.Y - maxY);
        else if (position.Y < minY)
            position.Y = maxY - (minY - position.Y);

        // Wrap Z
        if (position.Z > maxZ)
            position.Z = minZ + (position.Z - maxZ);
        else if (position.Z < minZ)
            position.Z = maxZ - (minZ - position.Z);

        return position;
    }

    public void Insert(List<int> elements)
    {
        // Pre-estimate capacity for better performance
        int potentialNewNodes = elements.Count / capacity;
        nodes.EnsureCapacity(nodes.Count + potentialNewNodes);
        nodePositions.EnsureCapacity(nodePositions.Count + potentialNewNodes);
        nodeSizes.EnsureCapacity(nodeSizes.Count + potentialNewNodes);
        elementIndices.EnsureCapacity(elementIndices.Count + elements.Count);

        for (int i = 0; i < elements.Count; i++)
        {
            int elementIndex = elements[i];

            // Cache position to avoid multiple bridge crossings
            Vector3 position = getBoid(elementIndex).Position;

            // Start at root node
            int currentNodeIndex = 0;
            Vector3 nodePos = nodePositions[0]; // Root position
            float nodeSize = nodeSizes[0];      // Root size
            int depth = 0;
            const int MAX_DEPTH = 10;

            // Traverse tree to find insert location
            bool inserted = false;
            while (!inserted)
            {
                OctNode node = nodes[currentNodeIndex];

                // If node has children, drill down
                if (node.firstChildIndex >= 0)
                {
                    // Calculate which octant for this position
                    int octant = GetOctant(position, nodePos);

                    // Move to child node
                    currentNodeIndex = node.firstChildIndex + octant;

                    // Use cached position and size if available
                    if (currentNodeIndex < nodePositions.Count)
                    {
                        nodePos = nodePositions[currentNodeIndex];
                        nodeSize = nodeSizes[currentNodeIndex];
                    }
                    else
                    {
                        // Calculate if not cached (should rarely happen)
                        nodePos = CalculateChildPosition(nodePos, nodeSize, octant);
                        nodeSize /= 2;
                    }
                    depth++;
                }
                // Leaf node reached
                else
                {
                    // If node has space, add element
                    if (node.elementCount < capacity)
                    {
                        // Add element to this node
                        AddElementIndexToNode(currentNodeIndex, elementIndex);
                        inserted = true;
                    }
                    else if (nodeSize < 0.1f || depth >= MAX_DEPTH - 1)
                    {
                        // If we're at a small node size or near max depth, just add here
                        AddElementIndexToNode(currentNodeIndex, elementIndex);
                        inserted = true;
                    }
                    else
                    {
                        // Need to subdivide
                        SubdivideNode(currentNodeIndex, nodePos, nodeSize);

                        // Redistribute existing elements
                        RedistributeElements(currentNodeIndex, nodePos, nodeSize);

                        // Continue traversal in next iteration
                    }
                }
                // Safety check - prevent infinite loops
                if (depth > MAX_DEPTH)
                {
                    AddElementIndexToNode(currentNodeIndex, elementIndex);
                    inserted = true;
                }
            }
        }
    }

    private int GetOctant(Vector3 position, Vector3 nodePos)
    {
        int octant = 0;
        if (position.X >= nodePos.X) octant |= 1; // x-axis bit
        if (position.Y >= nodePos.Y) octant |= 2; // y-axis bit
        if (position.Z >= nodePos.Z) octant |= 4; // z-axis bit
        return octant;
    }

    private Vector3 CalculateChildPosition(Vector3 parentPos, float parentSize, int octant)
    {
        // For centered boxes, offset is 1/4 of parent size
        float offset = parentSize / 4;

        float offsetX = ((octant & 1) != 0) ? offset : -offset;
        float offsetY = ((octant & 2) != 0) ? offset : -offset;
        float offsetZ = ((octant & 4) != 0) ? offset : -offset;

        return new Vector3(
            parentPos.X + offsetX,
            parentPos.Y + offsetY,
            parentPos.Z + offsetZ
        );
    }

    private void RedistributeElements(int nodeIndex, Vector3 nodePos, float nodeSize)
    {
        OctNode node = nodes[nodeIndex];
        int firstChildIndex = node.firstChildIndex;

        // Skip if no children or no elements
        if (node.firstChildIndex < 0 || node.elementCount <= 0 || node.firstElementIndex < 0)
            return;

        // Clear reused collections
        for (int i = 0; i < 8; i++)
            octantElements[i].Clear();
        indexMap.Clear();
        tempElementIndices.Clear();

        // Make sure we don't go out of bounds
        int endElementIndex = Mathf.Min(node.firstElementIndex + node.elementCount, elementIndices.Count);

        // Sort elements into child octants
        for (int i = 0; i < node.elementCount; i++)
        {
            int elementPos = node.firstElementIndex + i;
            if (elementPos >= elementIndices.Count)
                break; // Prevent out of bounds access

            int elementIdx = elementIndices[elementPos];
            // Cache position to reduce bridge crossings
            Vector3 pos = getBoid(elementIdx).Position;
            int octant = GetOctant(pos, nodePos);
            octantElements[octant].Add(elementIdx);
        }

        // Build new element indices list without allocating a new list
        // First, copy elements before this node
        for (int i = 0; i < node.firstElementIndex && i < elementIndices.Count; i++)
        {
            tempElementIndices.Add(elementIndices[i]);
        }

        // Clear parent node's elements
        node.elementCount = 0;
        node.firstElementIndex = -1;
        nodes[nodeIndex] = node;

        // Add elements to child nodes
        for (int octant = 0; octant < 8; octant++)
        {
            List<int> octantElements = this.octantElements[octant];
            if (octantElements.Count > 0)
            {
                // Update child node
                int childIdx = firstChildIndex + octant;
                if (childIdx < nodes.Count) // Safety check
                {
                    OctNode childNode = nodes[childIdx];
                    childNode.firstElementIndex = tempElementIndices.Count;
                    childNode.elementCount = octantElements.Count;
                    nodes[childIdx] = childNode;

                    // Add elements
                    tempElementIndices.AddRange(octantElements);
                }
            }
        }

        // Copy elements after this node's original elements
        int afterIndex = endElementIndex;
        for (int i = afterIndex; i < elementIndices.Count; i++)
        {
            tempElementIndices.Add(elementIndices[i]);
        }

        // Update other nodes' firstElementIndex values 
        // This is safer than the previous approach which could lead to out-of-bounds accesses
        for (int i = 0; i < nodes.Count; i++)
        {
            OctNode otherNode = nodes[i];

            // Only update nodes with valid element indices
            if (otherNode.firstElementIndex >= 0)
            {
                // If the node's firstElementIndex is from before the redistributed elements
                if (otherNode.firstElementIndex < node.firstElementIndex)
                {
                    // It keeps the same index
                }
                // If it's from after the redistributed elements
                else if (otherNode.firstElementIndex >= endElementIndex)
                {
                    // Apply offset based on elements count change
                    int oldCount = node.elementCount;
                    int newCount = 0;
                    for (int j = 0; j < 8; j++)
                        newCount += octantElements[j].Count;

                    int offset = newCount - oldCount;
                    otherNode.firstElementIndex += offset;
                    nodes[i] = otherNode;
                }
                // If it's one of the child nodes we just updated, it already has the correct index
            }
        }

        // Swap lists instead of allocating a new one
        List<int> temp = elementIndices;
        elementIndices = tempElementIndices;
        tempElementIndices = temp;
        tempElementIndices.Clear(); // Ready for next use
    }

    private void SubdivideNode(int nodeIndex, Vector3 nodePos, float nodeSize)
    {
        OctNode node = nodes[nodeIndex];

        // Create 8 child nodes
        int firstChildIndex = nodes.Count;

        // Update parent node
        node.firstChildIndex = firstChildIndex;
        nodes[nodeIndex] = node;

        // Create 8 empty child nodes and cache their positions
        float childSize = nodeSize / 2;
        for (int octant = 0; octant < 8; octant++)
        {
            // Add new node
            nodes.Add(new OctNode(-1, -1, 0, false));

            // Calculate and cache position and size
            Vector3 childPos = CalculateChildPosition(nodePos, nodeSize, octant);
            nodePositions.Add(childPos);
            nodeSizes.Add(childSize);
        }
    }

    private void AddElementIndexToNode(int currentNodeIndex, int elementIndex)
    {
        OctNode node = nodes[currentNodeIndex];

        // If this is the first element for this node, set firstElementIndex
        if (node.elementCount == 0)
        {
            // Set the starting index to be the current end of our elements list
            node.firstElementIndex = elementIndices.Count;
        }

        // Just add the element to the end of our list
        elementIndices.Add(elementIndex);

        // Increment the element count
        node.elementCount++;

        // Update the node in the list
        nodes[currentNodeIndex] = node;
    }

    public List<int> Search(Vector3 position, float range)
    {
        // Clear and reuse our collections
        searchResults.Clear();
        searchStack.Clear();

        // Safety check - make sure we have nodes and positions
        if (nodes.Count == 0 || nodePositions.Count == 0 || nodeSizes.Count == 0)
            return searchResults;

        // Push root node to stack
        searchStack.Push(new NodeSearchInfo(0, nodePositions[0], nodeSizes[0]));

        // Iterative traversal
        while (searchStack.Count > 0)
        {
            // Get current node from stack
            NodeSearchInfo current = searchStack.Pop();
            int nodeIndex = current.nodeIndex;
            Vector3 nodePos = current.position;
            float nodeSize = current.size;

            // Skip if node index is invalid
            if (nodeIndex >= nodes.Count)
                continue;

            OctNode node = nodes[nodeIndex];

            // Skip if node doesn't intersect search sphere
            if (!IntersectsSphere(nodePos, nodeSize, position, range))
                continue;

            // If leaf node, check all elements
            if (node.firstChildIndex == -1)
            {
                // Check if node has valid elements
                if (node.elementCount > 0 && node.firstElementIndex >= 0)
                {
                    // Make sure we don't go out of bounds
                    int endElementIndex = Mathf.Min(node.firstElementIndex + node.elementCount,
                                                  elementIndices.Count);

                    // Add elements that are within range
                    for (int i = node.firstElementIndex; i < endElementIndex; i++)
                    {
                        int elementIndex = elementIndices[i];

                        // Cache position to reduce bridge crossings
                        Vector3 elemPos = getBoid(elementIndex).Position;

                        if (elemPos.DistanceTo(position) <= range)
                        {
                            searchResults.Add(elementIndex);
                        }
                    }
                }
            }
            // If internal node, push all children to stack
            else if (node.firstChildIndex >= 0 && node.firstChildIndex < nodes.Count)
            {
                float childSize = nodeSize / 2;

                // Calculate the maximum child index we could have
                int maxChildIndex = node.firstChildIndex + 7;

                // Make sure we don't go out of bounds
                if (maxChildIndex >= nodes.Count)
                    continue;

                for (int octant = 0; octant < 8; octant++)
                {
                    int childIndex = node.firstChildIndex + octant;

                    // Use cached position if available
                    Vector3 childPos;
                    if (childIndex < nodePositions.Count)
                    {
                        childPos = nodePositions[childIndex];
                    }
                    else
                    {
                        childPos = CalculateChildPosition(nodePos, nodeSize, octant);
                    }

                    searchStack.Push(new NodeSearchInfo(childIndex, childPos, childSize));
                }
            }
        }

        return searchResults;
    }

    // Helper struct for iterative search
    private struct NodeSearchInfo
    {
        public int nodeIndex;
        public Vector3 position;
        public float size;

        public NodeSearchInfo(int nodeIndex, Vector3 position, float size)
        {
            this.nodeIndex = nodeIndex;
            this.position = position;
            this.size = size;
        }
    }

    private bool IntersectsSphere(Vector3 boxCenter, float boxSize, Vector3 sphereCenter, float sphereRadius)
    {
        // Calculate box half extents
        float halfSize = boxSize / 2;

        // Calculate distance from sphere center to closest point on box
        float dx = Mathf.Max(0, Mathf.Abs(sphereCenter.X - boxCenter.X) - halfSize);
        float dy = Mathf.Max(0, Mathf.Abs(sphereCenter.Y - boxCenter.Y) - halfSize);
        float dz = Mathf.Max(0, Mathf.Abs(sphereCenter.Z - boxCenter.Z) - halfSize);

        // If the closest distance is within the sphere radius, they intersect
        return (dx * dx + dy * dy + dz * dz) <= (sphereRadius * sphereRadius);
    }

    public void Clear()
    {
        // Reset to just the root node but keep allocated memory
        nodes.Clear();
        elementIndices.Clear();

        // Keep first position/size entry for the root
        if (nodePositions.Count > 1)
        {
            nodePositions.RemoveRange(1, nodePositions.Count - 1);
            nodeSizes.RemoveRange(1, nodeSizes.Count - 1);
        }

        // Create the root node (this is a leaf with no elements)
        nodes.Add(new OctNode(-1, -1, 0, false));
    }
}
