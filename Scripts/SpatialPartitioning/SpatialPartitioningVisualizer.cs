using Godot;
using System.Collections.Generic;

public partial class SpatialPartitioningVisualizer : Node3D
{
    [Export] public bool Visible { get; set; } = false;
    public ISpatialPartitioning Target { get; set; }

    // Visualization options
    [Export] public Color LeafNodeColor = new Color(0, 1, 0, 0.2f);
    [Export] public Color InternalNodeColor = new Color(0, 0, 1, 0.1f);
    [Export] public Color ColliderNodeColor = new Color(1, 0, 0, 0.3f);

    public override void _Process(double delta)
    {
        if (Visible && Target != null)
        {
            // Get visualization data from spatial system
            var visualizationData = GetVisualizationData();
            DrawSpatialSystem(visualizationData);
        }
    }

    public void Toggle()
    {
        Visible = !Visible;
    }

    private List<(Vector3 position, Vector3 size, bool isLeaf, bool hasCollider)> GetVisualizationData()
    {
        // Request visualization data from the spatial system
        if (Target is BVHManager bvh)
        {
            return bvh.GetVisualizationData();
        }
        else if (Target is DataOrientedOctTree octree)
        {
            return octree.GetVisualizationData();
        }
        else if (Target is SpatialCellManager cellManager)
        {
            return cellManager.GetVisualizationData();
        }

        return new List<(Vector3, Vector3, bool, bool)>();
    }

    private void DrawSpatialSystem(List<(Vector3 position, Vector3 size, bool isLeaf, bool hasCollider)> nodes)
    {
        foreach (var node in nodes)
        {
            // Select color based on node type
            Color color;
            if (node.isLeaf)
            {
                color = node.hasCollider ? ColliderNodeColor : LeafNodeColor;
            }
            else
            {
                color = node.hasCollider ?
                    new Color(ColliderNodeColor.R * 0.5f, ColliderNodeColor.G * 0.5f, ColliderNodeColor.B * 0.5f, ColliderNodeColor.A * 0.5f) :
                    InternalNodeColor;
            }

            // Draw the node
            DebugDraw3D.DrawBox(
                node.position,
                Quaternion.Identity,
                node.size,
                color,
                true,
                0
            );
        }
    }
}
