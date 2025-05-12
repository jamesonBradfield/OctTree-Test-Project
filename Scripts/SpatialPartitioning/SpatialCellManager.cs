using Godot;
using System.Collections.Generic;

public partial class SpatialCellManager : Node3D, ISpatialPartitioning
{
    // Cell data structure
    private struct CellData
    {
        public Vector3I cellCoord;
        public List<int> boids;
    }

    // Reusable collections to minimize GC pressure
    private CellData[] activeCells = new CellData[128]; // Pre-allocate reasonable size
    private int activeCellCount = 0;
    private Dictionary<Vector3I, int> cellLookup = new Dictionary<Vector3I, int>();
    private List<int> tempNeighborList = new List<int>(256);
    private HashSet<int> tempNeighborSet = new HashSet<int>();
    private HashSet<Vector3I> cellsWithColliders = new HashSet<Vector3I>();

    // Thresholds for optimization decisions
    private const int MIN_BOIDS_FOR_CELL_SYSTEM = 150;
    private const int MIN_BOIDS_PER_CELL = 20;

    // Cell configuration
    private float cellSize;
    private Vector3 rootPosition;
    private float rootSize;
    private bool debugVisible = false;

    // Store a reference to get boid elements
    private System.Func<int, OctTreeElement> getElement;
    private int capacity;

    public SpatialCellManager()
    {
        // Default initialization - will be properly initialized in Initialize()

        // Pre-allocate lists for cells
        for (int i = 0; i < activeCells.Length; i++)
        {
            activeCells[i].boids = new List<int>(32);
        }
    }

    // Standardized initialization matching ISpatialPartitioning
    public void Initialize(Vector3 rootPosition, float rootSize, int capacity, System.Func<int, OctTreeElement> getElement)
    {
        this.rootPosition = rootPosition;
        this.rootSize = rootSize;
        this.capacity = capacity;
        this.getElement = getElement;

        // Set initial cell size based on root size and capacity
        // Similar to how octree divides space
        cellSize = Mathf.Max(rootSize / Mathf.Sqrt(capacity), 1.0f);
    }

    // ISpatialPartitioning implementation
    public void Clear()
    {
        // Clear existing cell data but reuse collections
        for (int i = 0; i < activeCellCount; i++)
        {
            activeCells[i].boids.Clear();
        }
        cellLookup.Clear();
        activeCellCount = 0;
        cellsWithColliders.Clear();
    }

    public void Insert(List<int> elementIndices)
    {
        // Update cells based on the indices
        UpdateCells(elementIndices);
    }

    public List<int> FindNearby(Vector3 position, float range)
    {
        List<int> result = new List<int>();

        // Find the cell containing this position
        Vector3I baseCell = PositionToCell(position);

        // Determine how many cells to check in each direction based on range
        int cellRadius = Mathf.CeilToInt(range / cellSize);

        // Clear temp set for reuse
        tempNeighborSet.Clear();

        // Check all cells within the range
        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dy = -cellRadius; dy <= cellRadius; dy++)
            {
                for (int dz = -cellRadius; dz <= cellRadius; dz++)
                {
                    Vector3I checkCell = new Vector3I(
                        baseCell.X + dx,
                        baseCell.Y + dy,
                        baseCell.Z + dz
                    );

                    if (cellLookup.TryGetValue(checkCell, out int cellIdx))
                    {
                        // Add all boids from this cell
                        foreach (int boidIdx in activeCells[cellIdx].boids)
                        {
                            // Double-check distance if we have an element callback
                            if (getElement != null)
                            {
                                Vector3 boidPos = getElement(boidIdx).Position;
                                if (boidPos.DistanceSquaredTo(position) <= range * range)
                                {
                                    tempNeighborSet.Add(boidIdx);
                                }
                            }
                            else
                            {
                                // Without element callback, just add all boids in range cells
                                tempNeighborSet.Add(boidIdx);
                            }
                        }
                    }
                }
            }
        }

        // Convert set to list
        result.AddRange(tempNeighborSet);
        return result;
    }

    public bool IsNearCollider(Vector3 position)
    {
        Vector3I cellCoord = PositionToCell(position);

        // Check this cell and adjacent cells for colliders
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    Vector3I checkCell = new Vector3I(
                        cellCoord.X + dx,
                        cellCoord.Y + dy,
                        cellCoord.Z + dz
                    );

                    if (cellsWithColliders.Contains(checkCell))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public Vector3 WrapPosition(Vector3 position)
    {
        // Calculate min and max boundaries based on centered boxes
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
        var spaceState = GetWorld3D().DirectSpaceState;
        MarkCellsWithColliders(spaceState);
    }

    // Cell-specific methods (preserved functionality from original)

    // Determine if cell optimization should be used
    public bool ShouldUseCellOptimization(int boidCount)
    {
        return boidCount >= MIN_BOIDS_FOR_CELL_SYSTEM;
    }

    // Update cell assignments based on current positions
    public void UpdateCells(List<int> elementIndices)
    {
        // Skip cell processing for small simulations
        if (elementIndices.Count < MIN_BOIDS_FOR_CELL_SYSTEM)
            return;

        // Clear existing cell data but reuse collections
        for (int i = 0; i < activeCellCount; i++)
        {
            activeCells[i].boids.Clear();
        }
        cellLookup.Clear();
        activeCellCount = 0;

        // Assign boids to cells
        for (int i = 0; i < elementIndices.Count; i++)
        {
            int boidIdx = elementIndices[i];

            if (getElement == null)
                continue; // Can't assign without element access

            Vector3 position = getElement(boidIdx).Position;
            Vector3I cell = PositionToCell(position);

            // Find or create cell
            int cellIdx;
            if (cellLookup.TryGetValue(cell, out cellIdx))
            {
                // Add to existing cell
                activeCells[cellIdx].boids.Add(boidIdx);
            }
            else
            {
                // Create new cell
                if (activeCellCount >= activeCells.Length)
                {
                    // Expand capacity if needed
                    System.Array.Resize(ref activeCells, activeCells.Length * 2);

                    // Initialize new lists
                    for (int j = activeCellCount; j < activeCells.Length; j++)
                    {
                        if (activeCells[j].boids == null)
                            activeCells[j].boids = new List<int>(32);
                    }
                }

                cellIdx = activeCellCount++;
                activeCells[cellIdx].cellCoord = cell;
                activeCells[cellIdx].boids.Add(boidIdx);
                cellLookup[cell] = cellIdx;
            }
        }
    }

    // Process all cells with provided action (original method signature maintained)
    public void ProcessCells(List<OctTreeElement> elements, ISpatialPartitioning octree,
                           System.Func<int, List<int>, List<OctTreeElement>, List<int>> filterFunc,
                           System.Action<int, List<int>> processBoidFunc)
    {
        for (int cellIdx = 0; cellIdx < activeCellCount; cellIdx++)
        {
            var cell = activeCells[cellIdx];

            // Skip empty cells
            if (cell.boids.Count == 0)
                continue;

            // Choose processing strategy based on cell population
            if (cell.boids.Count >= MIN_BOIDS_PER_CELL)
            {
                // For populated cells, batch process all boids
                ProcessCellAsBatch(cell, elements, filterFunc, processBoidFunc);
            }
            else
            {
                // For sparse cells, use octree directly
                ProcessCellWithOctree(cell, elements, octree, filterFunc, processBoidFunc);
            }
        }
    }

    // Process a densely populated cell using cell-based neighbor finding
    private void ProcessCellAsBatch(CellData cell, List<OctTreeElement> elements,
                                  System.Func<int, List<int>, List<OctTreeElement>, List<int>> filterFunc,
                                  System.Action<int, List<int>> processBoidFunc)
    {
        // Get all potential neighbors from this and adjacent cells
        tempNeighborList.Clear();
        GetPotentialNeighbors(cell.cellCoord, tempNeighborList);

        // Process each boid in the cell
        foreach (int boidIdx in cell.boids)
        {
            // Apply visibility and distance filtering
            List<int> filteredNeighbors = filterFunc(boidIdx, tempNeighborList, elements);

            // Process the boid with filtered neighbors
            processBoidFunc(boidIdx, filteredNeighbors);
        }
    }

    // Process a sparsely populated cell using direct octree queries
    private void ProcessCellWithOctree(CellData cell, List<OctTreeElement> elements,
                                     ISpatialPartitioning octree,
                                     System.Func<int, List<int>, List<OctTreeElement>, List<int>> filterFunc,
                                     System.Action<int, List<int>> processBoidFunc)
    {
        // For each boid, use octree to find neighbors
        foreach (int boidIdx in cell.boids)
        {
            // Use octree for spatial query
            List<int> octreeNeighbors = octree.FindNearby(elements[boidIdx].Position, cellSize / 1.5f);

            // Apply filtering
            List<int> filteredNeighbors = filterFunc(boidIdx, octreeNeighbors, elements);

            // Process the boid
            processBoidFunc(boidIdx, filteredNeighbors);
        }
    }

    // Collect boids from a cell and its adjacent cells
    private void GetPotentialNeighbors(Vector3I cellCoord, List<int> result)
    {
        // First collect in HashSet to avoid duplicates
        tempNeighborSet.Clear();

        // Add boids from this cell and adjacent cells
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    Vector3I neighborCell = new Vector3I(
                        cellCoord.X + dx,
                        cellCoord.Y + dy,
                        cellCoord.Z + dz
                    );

                    if (cellLookup.TryGetValue(neighborCell, out int neighborCellIdx))
                    {
                        foreach (int boidIdx in activeCells[neighborCellIdx].boids)
                        {
                            tempNeighborSet.Add(boidIdx);
                        }
                    }
                }
            }
        }

        // Convert to list
        foreach (int boidIdx in tempNeighborSet)
        {
            result.Add(boidIdx);
        }
    }

    // Convert world position to cell coordinates
    private Vector3I PositionToCell(Vector3 position)
    {
        return new Vector3I(
            Mathf.FloorToInt(position.X / cellSize),
            Mathf.FloorToInt(position.Y / cellSize),
            Mathf.FloorToInt(position.Z / cellSize)
        );
    }

    // Change the cell size (recalculates on next update)
    public void SetCellSize(float newSize)
    {
        cellSize = newSize;
    }

    // Get current cell count for debugging
    public int GetActiveCellCount()
    {
        return activeCellCount;
    }

    // Method to check for colliders in cells
    private void MarkCellsWithColliders(PhysicsDirectSpaceState3D spaceState)
    {
        cellsWithColliders.Clear();

        foreach (var cellPair in cellLookup)
        {
            Vector3I cellCoord = cellPair.Key;

            // Calculate world position of this cell
            Vector3 cellPosition = new Vector3(
                (cellCoord.X + 0.5f) * cellSize,
                (cellCoord.Y + 0.5f) * cellSize,
                (cellCoord.Z + 0.5f) * cellSize
            );

            // Check for colliders
            var shape = new BoxShape3D();
            shape.Size = new Vector3(cellSize, cellSize, cellSize);

            var parameters = new PhysicsShapeQueryParameters3D();
            parameters.Shape = shape;
            parameters.Transform = new Transform3D(Basis.Identity, cellPosition);
            parameters.CollideWithBodies = true;
            parameters.CollisionMask = 1; // Adjust based on your collision layers

            var results = spaceState.IntersectShape(parameters);
            if (results.Count > 0)
            {
                cellsWithColliders.Add(cellCoord);
            }
        }
    }

    public List<(Vector3 position, Vector3 size, bool isLeaf, bool hasCollider)> GetVisualizationData()
    {
        var result = new List<(Vector3, Vector3, bool, bool)>();

        // Iterate through all active cells
        foreach (var cellPair in cellLookup)
        {
            Vector3I cellCoord = cellPair.Key;
            int cellIdx = cellPair.Value;

            // Calculate world position of this cell
            Vector3 cellPosition = new Vector3(
                (cellCoord.X + 0.5f) * cellSize,
                (cellCoord.Y + 0.5f) * cellSize,
                (cellCoord.Z + 0.5f) * cellSize
            );

            // Create box size
            Vector3 cellSizeVector = new Vector3(cellSize, cellSize, cellSize);

            // Check if cell has colliders
            bool hasCollider = cellsWithColliders.Contains(cellCoord);

            // All cells are considered "leaf" nodes in the cell manager
            bool isLeaf = true;

            // Add to result
            result.Add((cellPosition, cellSizeVector, isLeaf, hasCollider));
        }

        return result;
    }
}
