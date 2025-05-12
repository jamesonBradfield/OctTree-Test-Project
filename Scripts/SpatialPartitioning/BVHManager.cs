using Godot;
using System.Collections.Generic;

public partial class BVHManager : Node3D, ISpatialPartitioning
{
    // Data-oriented flattened BVH structure - much smaller now
    private struct BVHNode
    {
        public int firstChildIndex;    // -1 for leaf nodes
        public int elementCount;       // Number of elements in a leaf
        public int firstElementIndex;  // Start index in elementIndices list
        public bool hasCollider;       // For collision detection

        public BVHNode(int firstChildIndex, int elementCount, int firstElementIndex, bool hasCollider)
        {
            this.firstChildIndex = firstChildIndex;
            this.elementCount = elementCount;
            this.firstElementIndex = firstElementIndex;
            this.hasCollider = hasCollider;
        }
    }

    // Separate lists for positions and sizes
    private List<BVHNode> nodes = new();
    private List<Vector3> nodePositions = new();
    private List<Vector3> nodeSizes = new();
    private List<int> elementIndices = new();

    // Temp collections to avoid allocations
    private List<int> tempResults = new();
    private List<int> leftElements = new();
    private List<int> rightElements = new();

    // Configuration
    private Vector3 rootPosition;
    private float rootSize;
    private int maxElementsPerNode = 8;
    private System.Func<int, OctTreeElement> getElement;

    // Properties for debugging
    public int NodeCount => nodes.Count;
    public int ElementCount => elementIndices.Count;

    public BVHManager()
    {
        // Default constructor for Node3D
    }

    // ISpatialPartitioning implementation
    public void Initialize(Vector3 rootPosition, float rootSize, int capacity, System.Func<int, OctTreeElement> getElement)
    {
        this.rootPosition = rootPosition;
        this.rootSize = rootSize;
        this.maxElementsPerNode = capacity;
        this.getElement = getElement;

        Clear();
    }
    public List<(Vector3 position, Vector3 size, bool isLeaf, bool hasCollider)> GetVisualizationData()
    {
        var result = new List<(Vector3, Vector3, bool, bool)>();

        // Collect visualization data from all nodes
        for (int i = 0; i < nodes.Count; i++)
        {
            BVHNode node = nodes[i];
            Vector3 position = nodePositions[i];
            Vector3 size = nodeSizes[i];
            bool isLeaf = node.firstChildIndex < 0;

            result.Add((position, size, isLeaf, node.hasCollider));
        }

        return result;
    }
    public void Clear()
    {
        nodes.Clear();
        nodePositions.Clear();
        nodeSizes.Clear();
        elementIndices.Clear();

        // Create root node
        nodes.Add(new BVHNode(-1, 0, -1, false));
        nodePositions.Add(rootPosition);
        nodeSizes.Add(new Vector3(rootSize, rootSize, rootSize));
    }

    public void Insert(List<int> elements)
    {
        // Skip if no elements
        if (elements == null || elements.Count == 0)
            return;

        // Insert each element
        foreach (int elementIndex in elements)
        {
            InsertElement(elementIndex);
        }

        // Optimize the tree (optional)
        OptimizeTree();
    }

    private void InsertElement(int elementIndex)
    {
        // Start at root
        int currentNodeIndex = 0;

        while (true)
        {
            // Get current node
            BVHNode node = nodes[currentNodeIndex];

            // If this is a leaf node
            if (node.firstChildIndex == -1)
            {
                // If there's room, add the element
                if (node.elementCount < maxElementsPerNode)
                {
                    AddElementToNode(currentNodeIndex, elementIndex);
                    break;
                }
                // Otherwise, split the node
                else
                {
                    SplitNode(currentNodeIndex);

                    // Continue traversal to place element in one of the new children
                    OctTreeElement elem = getElement(elementIndex);
                    int childIndex = SelectChildForElement(currentNodeIndex, elem.Position);
                    currentNodeIndex = childIndex;
                }
            }
            // If internal node, traverse to appropriate child
            else
            {
                OctTreeElement elem = getElement(elementIndex);
                int childIndex = SelectChildForElement(currentNodeIndex, elem.Position);
                currentNodeIndex = childIndex;
            }
        }

        // Update bounding boxes after insertion
        UpdateBounds();
    }

    private void AddElementToNode(int nodeIndex, int elementIndex)
    {
        BVHNode node = nodes[nodeIndex];

        // If this is the first element
        if (node.elementCount == 0)
        {
            node.firstElementIndex = elementIndices.Count;
        }

        // Add the element
        elementIndices.Add(elementIndex);
        node.elementCount++;

        // Update the node
        nodes[nodeIndex] = node;

        // Expand bounds to include the element
        ExpandNodeBounds(nodeIndex, elementIndex);
    }

    private void ExpandNodeBounds(int nodeIndex, int elementIndex)
    {
        Vector3 nodePos = nodePositions[nodeIndex];
        Vector3 nodeSize = nodeSizes[nodeIndex];
        OctTreeElement element = getElement(elementIndex);

        // Calculate element's bounds (treating as a sphere)
        Vector3 elementMin = element.Position - new Vector3(element.Size / 2, element.Size / 2, element.Size / 2);
        Vector3 elementMax = element.Position + new Vector3(element.Size / 2, element.Size / 2, element.Size / 2);

        // Calculate node's current bounds
        Vector3 nodeMin = nodePos - nodeSize / 2;
        Vector3 nodeMax = nodePos + nodeSize / 2;

        // Expand bounds
        Vector3 newMin = new Vector3(
            Mathf.Min(nodeMin.X, elementMin.X),
            Mathf.Min(nodeMin.Y, elementMin.Y),
            Mathf.Min(nodeMin.Z, elementMin.Z)
        );

        Vector3 newMax = new Vector3(
            Mathf.Max(nodeMax.X, elementMax.X),
            Mathf.Max(nodeMax.Y, elementMax.Y),
            Mathf.Max(nodeMax.Z, elementMax.Z)
        );

        // Update node size and position
        Vector3 newSize = newMax - newMin;
        Vector3 newPos = newMin + newSize / 2;

        // Store back to the lists
        nodePositions[nodeIndex] = newPos;
        nodeSizes[nodeIndex] = newSize;
    }

    private void SplitNode(int nodeIndex)
    {
        BVHNode node = nodes[nodeIndex];
        Vector3 nodePos = nodePositions[nodeIndex];
        Vector3 nodeSize = nodeSizes[nodeIndex];

        // Find the longest axis
        int axis = 0; // 0 = X, 1 = Y, 2 = Z
        if (nodeSize.Y > nodeSize.X && nodeSize.Y > nodeSize.Z)
            axis = 1;
        else if (nodeSize.Z > nodeSize.X && nodeSize.Z > nodeSize.Y)
            axis = 2;

        // Collect elements from this node
        leftElements.Clear();
        rightElements.Clear();

        for (int i = 0; i < node.elementCount; i++)
        {
            int elemIdx = elementIndices[node.firstElementIndex + i];
            OctTreeElement elem = getElement(elemIdx);

            // Sort by position along the chosen axis
            float pos = 0;
            switch (axis)
            {
                case 0: pos = elem.Position.X; break;
                case 1: pos = elem.Position.Y; break;
                case 2: pos = elem.Position.Z; break;
            }

            // Split at median (using node position)
            float splitPos = nodePos[axis];
            if (pos <= splitPos)
                leftElements.Add(elemIdx);
            else
                rightElements.Add(elemIdx);
        }

        // Handle edge case: all elements on one side
        if (leftElements.Count == 0 || rightElements.Count == 0)
        {
            // Force split in half
            leftElements.Clear();
            rightElements.Clear();

            int half = node.elementCount / 2;
            for (int i = 0; i < node.elementCount; i++)
            {
                int elemIdx = elementIndices[node.firstElementIndex + i];
                if (i < half)
                    leftElements.Add(elemIdx);
                else
                    rightElements.Add(elemIdx);
            }
        }

        // Create two child nodes
        int leftChildIndex = nodes.Count;
        int rightChildIndex = leftChildIndex + 1;

        // Create empty nodes initially
        nodes.Add(new BVHNode(-1, 0, -1, false));
        nodes.Add(new BVHNode(-1, 0, -1, false));

        // Add initial positions and sizes 
        nodePositions.Add(nodePos); // Will update later
        nodePositions.Add(nodePos); // Will update later
        nodeSizes.Add(nodeSize / 2);
        nodeSizes.Add(nodeSize / 2);

        // Update parent to point to children
        node.firstChildIndex = leftChildIndex;
        node.elementCount = 0;
        node.firstElementIndex = -1;
        nodes[nodeIndex] = node;

        // Move elements to new lists
        int oldFirstElementIndex = node.firstElementIndex;
        int oldElementCount = node.elementCount;

        // Add elements to left child
        if (leftElements.Count > 0)
        {
            BVHNode leftChild = nodes[leftChildIndex];
            leftChild.firstElementIndex = elementIndices.Count;
            leftChild.elementCount = leftElements.Count;
            nodes[leftChildIndex] = leftChild;

            foreach (int elemIdx in leftElements)
            {
                elementIndices.Add(elemIdx);
                ExpandNodeBounds(leftChildIndex, elemIdx);
            }
        }

        // Add elements to right child
        if (rightElements.Count > 0)
        {
            BVHNode rightChild = nodes[rightChildIndex];
            rightChild.firstElementIndex = elementIndices.Count;
            rightChild.elementCount = rightElements.Count;
            nodes[rightChildIndex] = rightChild;

            foreach (int elemIdx in rightElements)
            {
                elementIndices.Add(elemIdx);
                ExpandNodeBounds(rightChildIndex, elemIdx);
            }
        }

        // Remove old elements by marking them as deleted
        // (We'll compact the list later)
        for (int i = 0; i < oldElementCount; i++)
        {
            elementIndices[oldFirstElementIndex + i] = -1;
        }
    }

    private int SelectChildForElement(int nodeIndex, Vector3 position)
    {
        BVHNode node = nodes[nodeIndex];

        // If not split yet, return this node
        if (node.firstChildIndex == -1)
            return nodeIndex;

        // Select child with closer centroid
        int leftChildIndex = node.firstChildIndex;
        int rightChildIndex = leftChildIndex + 1;

        Vector3 leftPos = nodePositions[leftChildIndex];
        Vector3 rightPos = nodePositions[rightChildIndex];

        float leftDistSq = position.DistanceSquaredTo(leftPos);
        float rightDistSq = position.DistanceSquaredTo(rightPos);

        return leftDistSq <= rightDistSq ? leftChildIndex : rightChildIndex;
    }

    private void UpdateBounds()
    {
        // Start from leaf nodes and work up to root
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            BVHNode node = nodes[i];

            // Skip leaf nodes - their bounds are already updated
            if (node.firstChildIndex == -1)
                continue;

            // For internal nodes, calculate bounds from children
            int leftChildIndex = node.firstChildIndex;
            int rightChildIndex = leftChildIndex + 1;

            if (leftChildIndex < nodes.Count && rightChildIndex < nodes.Count)
            {
                Vector3 leftPos = nodePositions[leftChildIndex];
                Vector3 leftSize = nodeSizes[leftChildIndex];
                Vector3 rightPos = nodePositions[rightChildIndex];
                Vector3 rightSize = nodeSizes[rightChildIndex];

                // Calculate min/max of both children
                Vector3 leftMin = leftPos - leftSize / 2;
                Vector3 leftMax = leftPos + leftSize / 2;
                Vector3 rightMin = rightPos - rightSize / 2;
                Vector3 rightMax = rightPos + rightSize / 2;

                // Combine bounds
                Vector3 newMin = new Vector3(
                    Mathf.Min(leftMin.X, rightMin.X),
                    Mathf.Min(leftMin.Y, rightMin.Y),
                    Mathf.Min(leftMin.Z, rightMin.Z)
                );

                Vector3 newMax = new Vector3(
                    Mathf.Max(leftMax.X, rightMax.X),
                    Mathf.Max(leftMax.Y, rightMax.Y),
                    Mathf.Max(leftMax.Z, rightMax.Z)
                );

                // Update node
                Vector3 newSize = newMax - newMin;
                Vector3 newPos = newMin + newSize / 2;

                nodePositions[i] = newPos;
                nodeSizes[i] = newSize;
            }
        }
    }

    private void OptimizeTree()
    {
        // 1. Remove deleted elements
        CompactElementList();

        // 2. Update all bounds
        UpdateBounds();
    }

    private void CompactElementList()
    {
        // Create a new list without the deleted elements
        List<int> newElementIndices = new List<int>();
        Dictionary<int, int> indexMapping = new Dictionary<int, int>();

        // Go through old list and copy non-deleted elements
        for (int i = 0; i < elementIndices.Count; i++)
        {
            int elemIdx = elementIndices[i];
            if (elemIdx != -1)
            {
                indexMapping[i] = newElementIndices.Count;
                newElementIndices.Add(elemIdx);
            }
        }

        // Update node references
        for (int i = 0; i < nodes.Count; i++)
        {
            BVHNode node = nodes[i];
            if (node.elementCount > 0 && node.firstElementIndex >= 0)
            {
                if (indexMapping.TryGetValue(node.firstElementIndex, out int newIndex))
                {
                    node.firstElementIndex = newIndex;
                    nodes[i] = node;
                }
            }
        }

        // Replace old list with compacted list
        elementIndices = newElementIndices;
    }

    public List<int> FindNearby(Vector3 position, float range)
    {
        tempResults.Clear();

        // Queue for iterative traversal
        Queue<int> nodesToCheck = new Queue<int>();
        nodesToCheck.Enqueue(0); // Start with root

        float rangeSq = range * range;

        while (nodesToCheck.Count > 0)
        {
            int nodeIndex = nodesToCheck.Dequeue();
            BVHNode node = nodes[nodeIndex];
            Vector3 nodePos = nodePositions[nodeIndex];
            Vector3 nodeSize = nodeSizes[nodeIndex];

            // Check if node's bounding box intersects with search sphere
            if (!IntersectsSphere(nodePos, nodeSize, position, range))
                continue;

            // If leaf, check elements
            if (node.firstChildIndex == -1)
            {
                for (int i = 0; i < node.elementCount; i++)
                {
                    // Check for valid index
                    int elementPos = node.firstElementIndex + i;
                    if (elementPos >= elementIndices.Count)
                        continue;

                    int elemIdx = elementIndices[elementPos];

                    // Skip deleted elements
                    if (elemIdx == -1)
                        continue;

                    OctTreeElement elem = getElement(elemIdx);
                    if (elem.Position.DistanceSquaredTo(position) <= rangeSq)
                    {
                        tempResults.Add(elemIdx);
                    }
                }
            }
            // If internal, add children to queue
            else
            {
                nodesToCheck.Enqueue(node.firstChildIndex);
                nodesToCheck.Enqueue(node.firstChildIndex + 1);
            }
        }

        return tempResults;
    }

    private bool IntersectsSphere(Vector3 boxCenter, Vector3 boxSize, Vector3 sphereCenter, float sphereRadius)
    {
        // Calculate box half extents
        Vector3 halfSize = boxSize / 2;

        // Calculate distance from sphere center to closest point on box
        float dx = Mathf.Max(0, Mathf.Abs(sphereCenter.X - boxCenter.X) - halfSize.X);
        float dy = Mathf.Max(0, Mathf.Abs(sphereCenter.Y - boxCenter.Y) - halfSize.Y);
        float dz = Mathf.Max(0, Mathf.Abs(sphereCenter.Z - boxCenter.Z) - halfSize.Z);

        // If the closest distance is within the sphere radius, they intersect
        return (dx * dx + dy * dy + dz * dz) <= (sphereRadius * sphereRadius);
    }

    public bool IsNearCollider(Vector3 position)
    {
        Queue<int> nodesToCheck = new Queue<int>();
        nodesToCheck.Enqueue(0); // Start with root

        while (nodesToCheck.Count > 0)
        {
            int nodeIndex = nodesToCheck.Dequeue();
            BVHNode node = nodes[nodeIndex];
            Vector3 nodePos = nodePositions[nodeIndex];
            Vector3 nodeSize = nodeSizes[nodeIndex];

            // Skip if node doesn't have colliders
            if (!node.hasCollider)
                continue;

            // Calculate distance to node (simple AABB check)
            Vector3 nodeMin = nodePos - nodeSize / 2;
            Vector3 nodeMax = nodePos + nodeSize / 2;

            bool insideBox =
                position.X >= nodeMin.X && position.X <= nodeMax.X &&
                position.Y >= nodeMin.Y && position.Y <= nodeMax.Y &&
                position.Z >= nodeMin.Z && position.Z <= nodeMax.Z;

            // If inside leaf with collider, we're near a collider
            if (insideBox && node.firstChildIndex == -1)
                return true;

            // If inside internal node, check children
            if (insideBox && node.firstChildIndex != -1)
            {
                nodesToCheck.Enqueue(node.firstChildIndex);
                nodesToCheck.Enqueue(node.firstChildIndex + 1);
            }
        }

        return false;
    }

    public Vector3 WrapPosition(Vector3 position)
    {
        // Calculate min and max boundaries based on root size
        float halfSize = rootSize / 2;
        float minX = rootPosition.X - halfSize;
        float maxX = rootPosition.X + halfSize;
        float minY = rootPosition.Y - halfSize;
        float maxY = rootPosition.Y + halfSize;
        float minZ = rootPosition.Z - halfSize;
        float maxZ = rootPosition.Z + halfSize;

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

    public void UpdateColliderInfo()
    {
        // Use physics queries to update collider flags
        var spaceState = GetWorld3D().DirectSpaceState;
        MarkNodesWithColliders(spaceState);
    }

    private void MarkNodesWithColliders(PhysicsDirectSpaceState3D spaceState)
    {
        // Iterate through all nodes
        for (int i = 0; i < nodes.Count; i++)
        {
            BVHNode node = nodes[i];
            Vector3 nodePos = nodePositions[i];
            Vector3 nodeSize = nodeSizes[i];

            // Skip tiny nodes for performance
            if (nodeSize.X < 0.1f || nodeSize.Y < 0.1f || nodeSize.Z < 0.1f)
                continue;

            // Test for colliders
            var shape = new BoxShape3D();
            shape.Size = nodeSize;

            var parameters = new PhysicsShapeQueryParameters3D();
            parameters.Shape = shape;
            parameters.Transform = new Transform3D(Basis.Identity, nodePos);
            parameters.CollideWithBodies = true;
            parameters.CollisionMask = 1; // Adjust based on your collision layers

            var results = spaceState.IntersectShape(parameters);

            // Mark node
            node.hasCollider = results.Count > 0;
            nodes[i] = node;
        }

        // Propagate collider flags up the tree
        PropagateColliderFlags();
    }

    private void PropagateColliderFlags()
    {
        // Start from leaves and go up
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            BVHNode node = nodes[i];

            // Skip leaf nodes
            if (node.firstChildIndex == -1)
                continue;

            // Get children
            int leftIndex = node.firstChildIndex;
            int rightIndex = leftIndex + 1;

            // Check if children exist
            if (leftIndex < nodes.Count && rightIndex < nodes.Count)
            {
                // Inherit collider flags from children
                bool leftHasCollider = nodes[leftIndex].hasCollider;
                bool rightHasCollider = nodes[rightIndex].hasCollider;

                node.hasCollider = leftHasCollider || rightHasCollider;
                nodes[i] = node;
            }
        }
    }
}
