using Godot;
using System.Collections.Generic;

public interface ISpatialPartitioning
{
    // Initialization - all spatial systems need these parameters
    void Initialize(Vector3 rootPosition, float rootSize, int capacity, System.Func<int, OctTreeElement> getElement);

    // Clear all spatial data
    void Clear();

    // Insert elements into the spatial system
    void Insert(List<int> elementIndices);

    // Find elements within a certain range of a position
    List<int> FindNearby(Vector3 position, float range);

    // Check if a position is near a collider
    bool IsNearCollider(Vector3 position);

    // Wrap a position to stay within bounds (if applicable)
    Vector3 WrapPosition(Vector3 position);

    // Update/mark collider information
    void UpdateColliderInfo();
    List<(Vector3 position, Vector3 size, bool isLeaf, bool hasCollider)> GetVisualizationData();
}
