using Godot;
using System.Collections.Generic;
using System.Linq;
public partial class OctTree : Node3D
{
    public LinkedList<Aabb> elements = new LinkedList<Aabb>();
    public LinkedList<OctNode> nodes = new LinkedList<OctNode>();
    int MaxCapacity;
    bool debug = false;

    public OctTree(Aabb RootAabb, int MaxCapacity)
    {
        this.MaxCapacity = MaxCapacity;
        this.Name = "OctTree";
        nodes.AddFirst(new OctNode(RootAabb, 0));
    }


    public void Insert(Aabb element)
    {
        InsertRecursive(nodes.First, element);
    }

    private void InsertRecursive(LinkedListNode<OctNode> currentNode, Aabb element)
    {
        // Base case: node doesn't exist or doesn't contain our element
        if (currentNode == null || !currentNode.Value.bounds.Encloses(element))
            return;

        Log($"Element {element} inserting into {currentNode.Value.bounds},{currentNode.Value.count}");

        // Case 1: Non-leaf node - use breadth-first search to find a suitable node
        if (currentNode.Value.count == -1)
        {
            // Get all node sizes in descending order (largest/root first)
            HashSet<float> uniqueSizes = new HashSet<float>();
            LinkedListNode<OctNode> tempNode = nodes.First;
            while (tempNode != null)
            {
                uniqueSizes.Add(tempNode.Value.bounds.Size.X);
                tempNode = tempNode.Next;
            }

            List<float> sortedSizes = uniqueSizes.ToList();
            sortedSizes.Sort((a, b) => b.CompareTo(a));

            // Start BFS from the current depth
            int currentSizeIndex = sortedSizes.IndexOf(currentNode.Value.bounds.Size.X);

            // Try each depth level, starting from the current depth
            for (int d = currentSizeIndex; d < sortedSizes.Count; d++)
            {
                float targetSize = sortedSizes[d];

                // Get all nodes at this depth
                List<LinkedListNode<OctNode>> nodesAtDepth = GetNodesAtDepth(targetSize);

                // Try each node at this depth
                foreach (var node in nodesAtDepth)
                {
                    // Skip non-leaf nodes
                    if (node.Value.count == -1)
                        continue;

                    // Check if this node can enclose our element
                    if (node.Value.bounds.Encloses(element))
                    {
                        // If node has space, insert here
                        if (node.Value.count < MaxCapacity)
                        {
                            if (elements.Count == 0)
                                elements.AddFirst(element);
                            else
                                elements.AddAfter(FindLastElementInOctNode(node), element);

                            UpdateCount(node, node.Value.count + 1);
                            Log($"Element {element} added to OctNode({node.Value.bounds},{node.Value.count}) in BFS");
                            return;
                        }
                        // If node is full, we'll split it
                        else if (node.Value.count == MaxCapacity)
                        {
                            // Handle splitting for this node
                            SplitNodeAndRedistribute(node, element);
                            return;
                        }
                    }
                }
            }

            // If we get here, no suitable node was found at any depth
            Log($"No suitable node found in BFS, adding to elements list");
            if (elements.Count == 0)
                elements.AddFirst(element);
            else
                elements.AddLast(element);
        }
        // Case 2: Leaf node at capacity - split and redistribute
        else if (currentNode.Value.count == MaxCapacity)
        {
            SplitNodeAndRedistribute(currentNode, element);
        }
        // Case 3: Leaf node with space - insert here
        else
        {
            if (elements.Count == 0)
                elements.AddFirst(element);
            else
                elements.AddAfter(FindLastElementInOctNode(currentNode), element);

            UpdateCount(currentNode, currentNode.Value.count + 1);
            Log($"Element {element} added to OctNode({currentNode.Value.bounds},{currentNode.Value.count})");
        }
    }
    // Get all nodes at a specific depth (identified by their size)
    private List<LinkedListNode<OctNode>> GetNodesAtDepth(float size)
    {
        List<LinkedListNode<OctNode>> nodesAtDepth = new List<LinkedListNode<OctNode>>();
        LinkedListNode<OctNode> currentNode = nodes.First;

        // Iterate through all nodes
        while (currentNode != null)
        {
            // If the node's size matches our target size, add it to the list
            if (Mathf.Abs(currentNode.Value.bounds.Size.X - size) < 0.001f) // Using a small epsilon for float comparison
            {
                nodesAtDepth.Add(currentNode);
            }

            currentNode = currentNode.Next;
        }

        return nodesAtDepth;
    }

    // Get all nodes at specific depths, ordered by depth (root first, then depth 1, etc)
    private List<LinkedListNode<OctNode>> GetNodesOrderedByDepth()
    {
        List<LinkedListNode<OctNode>> orderedNodes = new List<LinkedListNode<OctNode>>();
        HashSet<float> uniqueSizes = new HashSet<float>();

        // First, collect all unique sizes
        LinkedListNode<OctNode> currentNode = nodes.First;
        while (currentNode != null)
        {
            uniqueSizes.Add(currentNode.Value.bounds.Size.X);
            currentNode = currentNode.Next;
        }

        // Convert to a sorted list (descending order - largest/root first)
        List<float> sortedSizes = uniqueSizes.ToList();
        sortedSizes.Sort((a, b) => b.CompareTo(a));

        // Get nodes for each size, in order
        foreach (float size in sortedSizes)
        {
            orderedNodes.AddRange(GetNodesAtDepth(size));
        }

        return orderedNodes;
    }
    // Separate method for splitting a node and redistributing elements
    private void SplitNodeAndRedistribute(LinkedListNode<OctNode> nodeToSplit, Aabb newElement)
    {
        Log($"Splitting node at {nodeToSplit.Value.bounds.Position}, size {nodeToSplit.Value.bounds.Size}");

        // Gather all elements before splitting
        LinkedListNode<Aabb> elementNode = FindFirstElementInOctNode(nodeToSplit);
        int originalCount = nodeToSplit.Value.count;
        List<Aabb> elementsToRedistribute = new List<Aabb>();

        // Collect elements without removing them yet
        for (int i = 0; i < originalCount; i++)
        {
            elementsToRedistribute.Add(elementNode.Value);
            elementNode = elementNode.Next;
        }

        // Convert to non-leaf
        var splitNode = UpdateCount(nodeToSplit, -1);
        CreateChildOctNodes(splitNode);

        // Add the new element to the redistribution list
        elementsToRedistribute.Add(newElement);

        // Remove the original elements from the list
        elementNode = FindFirstElementInOctNode(splitNode);
        for (int i = 0; i < originalCount; i++)
        {
            LinkedListNode<Aabb> nextElement = elementNode.Next;
            elements.Remove(elementNode);
            elementNode = nextElement;
        }

        // Redistribute all elements using the breadth-first approach
        foreach (Aabb elementToRedist in elementsToRedistribute)
        {
            // Start a new insertion process from the root
            Insert(elementToRedist);
        }
    }


    private LinkedListNode<OctNode> UpdateCount(LinkedListNode<OctNode> oldNode, int count)
    {
        LinkedListNode<OctNode> newChildNode = new(new(oldNode.Value.bounds, count));
        nodes.AddAfter(oldNode, newChildNode);
        nodes.Remove(oldNode);
        return newChildNode;
    }

    private LinkedListNode<Aabb> FindFirstElementInOctNode(LinkedListNode<OctNode> searchNode)
    {
        int sum = 0;
        LinkedListNode<OctNode> currentNode = nodes.First;
        LinkedListNode<Aabb> foundElement = elements.First;
        while (currentNode != searchNode)
        {
            if (currentNode.Value.count != -1)
                sum += currentNode.Value.count;
            currentNode = currentNode.Next;
        }
        for (int i = 0; i < sum; i++)
        {
            foundElement = foundElement.Next;
        }
        return foundElement;
    }

    private LinkedListNode<Aabb> FindLastElementInOctNode(LinkedListNode<OctNode> childNode)
    {
        LinkedListNode<Aabb> lastElementNodeInOctNode = elements.First;
        for (int j = 0; j < childNode.Value.count - 1; j++)
        {
            if (lastElementNodeInOctNode.Next != null)
                lastElementNodeInOctNode = lastElementNodeInOctNode.Next;
        }

        return lastElementNodeInOctNode;
    }

    private void CreateChildOctNodes(LinkedListNode<OctNode> parentNode)
    {
        Vector3 new_size = parentNode.Value.bounds.Size / 2;
        Vector3 new_pos = parentNode.Value.bounds.Position;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    Vector3 childPos = new Vector3(
                        new_pos.X + x * new_size.X,
                        new_pos.Y + y * new_size.Y,
                        new_pos.Z + z * new_size.Z
                    );

                    LinkedListNode<OctNode> childNode = new(new(childPos, new_size, 0));
                    nodes.AddAfter(parentNode, childNode);
                }
            }
        }
    }
    public void Regenerate()
    {
        Log("Regenerating OctTree");

        // Save reference to the root bounds
        Aabb rootBounds = nodes.First.Value.bounds;

        // Store all elements before clearing the tree
        List<Aabb> allElements = new List<Aabb>();
        foreach (Aabb element in elements)
        {
            allElements.Add(element);
        }

        // Clear existing nodes and elements
        nodes.Clear();
        elements.Clear();

        // Recreate the root node
        nodes.AddFirst(new OctNode(rootBounds, 0));

        // Reinsert all elements
        Log($"Reinserting {allElements.Count} elements into regenerated tree");
        foreach (Aabb element in allElements)
        {
            Insert(element);
        }

        Log($"OctTree regenerated with {nodes.Count} nodes and {elements.Count} elements");
    }
    public override void _Process(double delta)
    {
        foreach (OctNode node in nodes)
        {
            DebugDraw3D.DrawAabb(node.bounds);
        }
        DebugDraw3D.DrawGizmo(Transform);
    }

    public void Log(string log_message)
    {
        if (!debug)
            return;
        GD.Print("[OctTree] " + log_message);
    }
}
