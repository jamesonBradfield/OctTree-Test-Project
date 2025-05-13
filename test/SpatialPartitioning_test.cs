using Godot;
using System.Collections.Generic;
using System;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SpatialPartitioningTest
{
    private List<OctTreeElement> _elements;
    private System.Func<int, OctTreeElement> _getElement;

    [BeforeTest]
    public void Setup()
    {
        // Create test elements
        _elements = new List<OctTreeElement>();

        // Add elements at specific positions for testing
        _elements.Add(new OctTreeElement(new Vector3(0, 0, 0), 1.0f));
        _elements.Add(new OctTreeElement(new Vector3(5, 0, 0), 1.0f));
        _elements.Add(new OctTreeElement(new Vector3(0, 5, 0), 1.0f));
        _elements.Add(new OctTreeElement(new Vector3(0, 0, 5), 1.0f));
        _elements.Add(new OctTreeElement(new Vector3(5, 5, 5), 1.0f));

        // Create a function to retrieve elements by index
        _getElement = (int index) => _elements[index];
    }

    [TestCase(typeof(DataOrientedOctTree), Description = "Testing OctTree implementation")]
    [TestCase(typeof(SpatialCellManager), Description = "Testing Cell Manager implementation")]
    [TestCase(typeof(BVHManager), Description = "Testing BVH implementation")]
    public void TestSpatialPartitioningImplementation(Type implementationType)
    {
        // Create the specified implementation
        ISpatialPartitioning spatialSystem = (ISpatialPartitioning)Activator.CreateInstance(implementationType);
        // AddChild((Node)spatialSystem);

        try
        {
            // Initialize with test parameters
            Vector3 rootPosition = Vector3.Zero;
            float rootSize = 20.0f;
            int capacity = 2;

            spatialSystem.Initialize(rootPosition, rootSize, capacity, _getElement);

            // Test Insert and FindNearby
            TestInsertAndFindNearby(spatialSystem);

            // Test position wrapping
            TestPositionWrapping(spatialSystem, rootSize);
        }
        finally
        {
            // Clean up
            // RemoveChild((Node)spatialSystem);
            ((Node)spatialSystem).QueueFree();
        }
    }

    private void TestInsertAndFindNearby(ISpatialPartitioning spatialSystem)
    {
        // Test Clear method
        spatialSystem.Clear();

        // Test Insert method with all element indices
        List<int> indices = new List<int> { 0, 1, 2, 3, 4 };
        spatialSystem.Insert(indices);

        // Test FindNearby method
        List<int> nearCenter = spatialSystem.FindNearby(Vector3.Zero, 2.0f);
        AssertThat(nearCenter).Contains(0); // Element at (0,0,0) should be found

        List<int> nearFar = spatialSystem.FindNearby(new Vector3(5, 5, 5), 2.0f);
        AssertThat(nearFar).Contains(4); // Element at (5,5,5) should be found
    }

    private void TestPositionWrapping(ISpatialPartitioning spatialSystem, float rootSize)
    {
        float halfSize = rootSize / 2;

        // Test various wrapping cases
        TestWrappingCase(spatialSystem, new Vector3(halfSize + 5, 0, 0), "X positive overflow");
        TestWrappingCase(spatialSystem, new Vector3(-halfSize - 5, 0, 0), "X negative overflow");
        TestWrappingCase(spatialSystem, new Vector3(0, halfSize + 5, 0), "Y positive overflow");
        TestWrappingCase(spatialSystem, new Vector3(0, -halfSize - 5, 0), "Y negative overflow");
        TestWrappingCase(spatialSystem, new Vector3(0, 0, halfSize + 5), "Z positive overflow");
        TestWrappingCase(spatialSystem, new Vector3(0, 0, -halfSize - 5), "Z negative overflow");
    }

    private void TestWrappingCase(ISpatialPartitioning spatialSystem, Vector3 outsidePos, string description)
    {
        Vector3 wrappedPos = spatialSystem.WrapPosition(outsidePos);

        // The wrapped position should be different from the outside position
        AssertThat(wrappedPos).IsNotEqual(outsidePos);

        // If we went out of bounds in one dimension, we should come back in from the opposite side
        if (outsidePos.X > 0 && Mathf.Abs(outsidePos.X) > Mathf.Abs(outsidePos.Y) && Mathf.Abs(outsidePos.X) > Mathf.Abs(outsidePos.Z))
            AssertThat(wrappedPos.X).IsLess(0);
        else if (outsidePos.X < 0 && Mathf.Abs(outsidePos.X) > Mathf.Abs(outsidePos.Y) && Mathf.Abs(outsidePos.X) > Mathf.Abs(outsidePos.Z))
            AssertThat(wrappedPos.X).IsGreater(0);

        if (outsidePos.Y > 0 && Mathf.Abs(outsidePos.Y) > Mathf.Abs(outsidePos.X) && Mathf.Abs(outsidePos.Y) > Mathf.Abs(outsidePos.Z))
            AssertThat(wrappedPos.Y).IsLess(0);
        else if (outsidePos.Y < 0 && Mathf.Abs(outsidePos.Y) > Mathf.Abs(outsidePos.X) && Mathf.Abs(outsidePos.Y) > Mathf.Abs(outsidePos.Z))
            AssertThat(wrappedPos.Y).IsGreater(0);

        if (outsidePos.Z > 0 && Mathf.Abs(outsidePos.Z) > Mathf.Abs(outsidePos.X) && Mathf.Abs(outsidePos.Z) > Mathf.Abs(outsidePos.Y))
            AssertThat(wrappedPos.Z).IsLess(0);
        else if (outsidePos.Z < 0 && Mathf.Abs(outsidePos.Z) > Mathf.Abs(outsidePos.X) && Mathf.Abs(outsidePos.Z) > Mathf.Abs(outsidePos.Y))
            AssertThat(wrappedPos.Z).IsGreater(0);
    }
}
