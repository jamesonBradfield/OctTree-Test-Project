using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// BVHManager implements the ISpatialPartitioning interface using an AABBTree
/// for efficient spatial queries in a boid simulation.
/// </summary>
public partial class BVHManager : Node3D, ISpatialPartitioning, ICollider
{
    // The underlying BVH structure
    private AABBTree<ElementWrapper> tree;

    // Root position and size (for bounds checking and wrapping)
    private Vector3 rootPosition;
    private float rootSize;
    private float halfSize;

    // Function to get elements from the simulation
    private Func<int, OctTreeElement> getElement;

    // Dictionary to map element indices to their wrappers
    private Dictionary<int, ElementWrapper> wrappers = new Dictionary<int, ElementWrapper>();

    // Collider tracking
    private List<int> elementsNearColliders = new List<int>();

    // Wrapper class to store element index and implement ICollider
    private class ElementWrapper : ICollider
    {
        public int Index { get; private set; }
        public OctTreeElement Element => getElementFunc(Index);
        private Func<int, OctTreeElement> getElementFunc;

        // Cache the AABB to avoid recalculations
        private Aabb cachedAabb;
        private Vector3 cachedPosition;

        public bool IsNearCollider { get; set; } = false;

        public ElementWrapper(int index, Func<int, OctTreeElement> getElementFunc)
        {
            Index = index;
            this.getElementFunc = getElementFunc;
            UpdateAabb();
        }

        public void UpdateAabb()
        {
            var element = Element;
            cachedPosition = element.Position;

            // Create AABB around the element with its size
            float halfSize = element.Size / 2f;
            Vector3 extents = new Vector3(halfSize, halfSize, halfSize);
            cachedAabb = new Aabb(cachedPosition - extents, extents * 2);
        }

        public Aabb GetAabb()
        {
            // Check if position has changed
            if (Element.Position != cachedPosition)
            {
                UpdateAabb();
            }
            return cachedAabb;
        }
    }

    /// <summary>
    /// Initializes the BVH Manager with simulation parameters.
    /// </summary>
    public void Initialize(Vector3 rootPosition, float rootSize, int capacity, Func<int, OctTreeElement> getElement)
    {
        this.rootPosition = rootPosition;
        this.rootSize = rootSize;
        this.halfSize = rootSize / 2f;
        this.getElement = getElement;

        // Initialize the BVH tree with our modified version
        InitializeAabbTree();

        // Set the margin and multiplier appropriate for boid simulation
        // These could be exposed as properties if needed
        SetAabbTreeProperties(0.2f, 1.5f);
    }

    // Helper method to initialize the tree with appropriate error handling
    private void InitializeAabbTree()
    {
        try
        {
            tree = new AABBTree<ElementWrapper>();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error initializing AABBTree: {ex.Message}");
            // Create fallback if initialization fails
            tree = new AABBTree<ElementWrapper>();
        }
    }

    // Helper method to set tree properties with error handling
    private void SetAabbTreeProperties(float margin, float multiplier)
    {
        try
        {
            tree.AabbMargin = margin;
            tree.AabbMultiplier = multiplier;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Could not set AABB tree properties: {ex.Message}");
            // Continue without setting properties
        }
    }

    /// <summary>
    /// Clears all elements from the BVH.
    /// </summary>
    public void Clear()
    {
        try
        {
            tree.Reset();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error resetting tree: {ex.Message}");
            // Try to recreate the tree
            InitializeAabbTree();
        }

        wrappers.Clear();
        elementsNearColliders.Clear();
    }

    /// <summary>
    /// Inserts elements into the BVH using their indices.
    /// </summary>
    public void Insert(List<int> elementIndices)
    {
        foreach (int index in elementIndices)
        {
            try
            {
                // Create or get existing wrapper
                if (!wrappers.TryGetValue(index, out ElementWrapper wrapper))
                {
                    wrapper = new ElementWrapper(index, getElement);
                    wrappers[index] = wrapper;

                    // Add to tree
                    tree.CreateNode(wrapper, wrapper.GetAabb());
                }
                else
                {
                    // Update existing node
                    wrapper.UpdateAabb();

                    // Use the safer MoveNode approach
                    SafeMoveNode(wrapper);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error inserting element {index}: {ex.Message}");
                // Continue with other elements
            }
        }
    }

    // Helper method to safely move nodes, working around the __.Throw calls
    private void SafeMoveNode(ElementWrapper wrapper)
    {
        try
        {
            tree.MoveNode(wrapper, wrapper.GetAabb());
        }
        catch
        {
            // If MoveNode fails, try recreating the node
            try
            {
                // Remove and recreate the node as a fallback
                tree.RemoveNode(wrapper);
                tree.CreateNode(wrapper, wrapper.GetAabb());
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error recreating node: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Finds elements within a specified range of a position.
    /// </summary>
    public List<int> FindNearby(Vector3 position, float range, int maxNeighbors = int.MaxValue)
    {
        var result = new List<int>();
        float rangeSqr = range * range;

        try
        {
            // Create query AABB around the position
            Vector3 queryExtents = new Vector3(range, range, range);
            Aabb queryAabb = new Aabb(position - queryExtents, queryExtents * 2);

            // Query the tree with error handling
            try
            {
                tree.Query(queryAabb, (wrapper) =>
                {
                    try
                    {
                        // Do distance check to refine results
                        float distSqr = position.DistanceSquaredTo(wrapper.Element.Position);
                        if (distSqr <= rangeSqr)
                        {
                            result.Add(wrapper.Index);

                            // Stop if we reached max neighbors
                            if (result.Count >= maxNeighbors)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    catch
                    {
                        // Skip elements that cause errors
                        return true;
                    }
                });
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error in tree.Query: {ex.Message}");
                // Fall through to the fallback below
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error in FindNearby: {ex.Message}");
        }

        // If we found no results or an error occurred, use a fallback approach
        if (result.Count == 0)
        {
            // Fallback: Brute force scan
            foreach (var pair in wrappers)
            {
                try
                {
                    float distSqr = position.DistanceSquaredTo(pair.Value.Element.Position);
                    if (distSqr <= rangeSqr)
                    {
                        result.Add(pair.Key);

                        if (result.Count >= maxNeighbors)
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    // Skip elements that cause errors
                    continue;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a position is near a collider.
    /// </summary>
    public bool IsNearCollider(Vector3 position)
    {
        // For simple implementation, just check bounds
        // In a more sophisticated version, this would check against actual colliders

        float boundaryCheck = halfSize * 0.95f;
        Vector3 distFromCenter = (position - rootPosition).Abs();

        return distFromCenter.X > boundaryCheck ||
               distFromCenter.Y > boundaryCheck ||
               distFromCenter.Z > boundaryCheck;
    }

    /// <summary>
    /// Wraps a position to stay within bounds.
    /// </summary>
    public Vector3 WrapPosition(Vector3 position)
    {
        Vector3 result = position;
        Vector3 size = new Vector3(rootSize, rootSize, rootSize);
        Vector3 min = rootPosition - (size / 2);
        Vector3 max = rootPosition + (size / 2);

        // Wrap along each axis
        if (result.X < min.X) result.X = max.X - (min.X - result.X);
        else if (result.X > max.X) result.X = min.X + (result.X - max.X);

        if (result.Y < min.Y) result.Y = max.Y - (min.Y - result.Y);
        else if (result.Y > max.Y) result.Y = min.Y + (result.Y - max.Y);

        if (result.Z < min.Z) result.Z = max.Z - (min.Z - result.Z);
        else if (result.Z > max.Z) result.Z = min.Z + (result.Z - max.Z);

        return result;
    }

    /// <summary>
    /// Updates collider information for visualization and queries.
    /// </summary>
    public void UpdateColliderInfo()
    {
        elementsNearColliders.Clear();

        // Mark elements near the boundaries as near colliders
        foreach (var pair in wrappers)
        {
            bool isNearCollider = IsNearCollider(pair.Value.Element.Position);
            pair.Value.IsNearCollider = isNearCollider;

            if (isNearCollider)
            {
                elementsNearColliders.Add(pair.Key);
            }
        }
    }

    /// <summary>
    /// Gets AABB data for the BVH tree to visualize it.
    /// </summary>
    public List<(Vector3 position, Vector3 size, bool isLeaf, bool hasCollider)> GetVisualizationData()
    {
        var result = new List<(Vector3, Vector3, bool, bool)>();

        // Create a hash set of wrapper indices that are near colliders for quick lookup
        HashSet<ElementWrapper> collidingWrappers = new HashSet<ElementWrapper>();
        foreach (var index in elementsNearColliders)
        {
            if (wrappers.TryGetValue(index, out var wrapper))
            {
                collidingWrappers.Add(wrapper);
            }
        }

        try
        {
            // Iterate through BVH nodes if available
            foreach (var node in tree.EnumerateNodes())
            {
                try
                {
                    Aabb aabb = node.Aabb;
                    Vector3 position = aabb.Position + (aabb.Size / 2);
                    Vector3 size = aabb.Size;
                    bool isLeaf = node.IsLeaf;
                    bool hasCollider = isLeaf && node.Data != null && collidingWrappers.Contains(node.Data);

                    result.Add((position, size, isLeaf, hasCollider));
                }
                catch
                {
                    // Skip nodes that cause errors
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error getting visualization data: {ex.Message}");

            // Fallback: Just show the root bounds
            Aabb rootAabb = GetAabb();
            result.Add((rootPosition, new Vector3(rootSize, rootSize, rootSize), false, false));
        }

        return result;
    }

    /// <summary>
    /// Implements ICollider for the BVHManager itself.
    /// </summary>
    public Aabb GetAabb()
    {
        return new Aabb(
            rootPosition - new Vector3(halfSize, halfSize, halfSize),
            new Vector3(rootSize, rootSize, rootSize)
        );
    }
}
