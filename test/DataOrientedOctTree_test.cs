namespace GodotGame;
using GdUnit4;
using Godot;
using System;
using System.Collections.Generic;
using static GdUnit4.Assertions;

[TestSuite]
public class DataOrientedOctTreeTest
{
    // private DataOrientedOctTree? _sut;
    //
    // // Helper function for getting positions
    // private Vector3 GetPositionFunction(int index)
    // {
    //     var positions = new List<Vector3>
    //     {
    //         new Vector3(0, 0, 0),
    //         new Vector3(5, 5, 5),
    //         new Vector3(-5, 5, 5),
    //         new Vector3(5, -5, 5),
    //         new Vector3(5, 5, -5),
    //         new Vector3(-5, -5, 5),
    //         new Vector3(-5, 5, -5),
    //         new Vector3(5, -5, -5),
    //         new Vector3(-5, -5, -5)
    //     };
    //
    //     if (index >= 0 && index < positions.Count)
    //         return positions[index];
    //
    //     return Vector3.Zero;
    // }
    //
    // [Before]
    // public void Setup()
    // {
    //     // Create a new OctTree with standard parameters
    //     var position = new Vector3(0, 0, 0);
    //     var size = new Vector3(10, 10, 10);
    //     int capacity = 4;
    //
    //     _sut = AutoFree(new DataOrientedOctTree(position, size, capacity, GetPositionFunction));
    //     AssertThat(_sut).IsNotNull();
    // }
    //
    // [After]
    // public void TearDown()
    // {
    //     // Clean up is handled by AutoFree
    // }
    //
    // [TestCase]
    // public void Test_Insert()
    // {
    //     // Arrange
    //     AssertThat(_sut).IsNotNull();
    //     var position = new Vector3(1, 1, 1);
    //
    //     // Act
    //     _sut.Insert(0, position);
    //
    //     // Assert
    //     // The root node should contain the element
    //     AssertThat(_sut.NodeElementIndices[0].Count).IsEqual(1);
    //     AssertThat(_sut.NodeElementIndices[0][0]).IsEqual(0);
    // }
    //
    // [TestCase]
    // public void Test_Clear()
    // {
    //     // Arrange
    //     AssertThat(_sut).IsNotNull();
    //
    //     // Insert several elements
    //     _sut.Insert(0, new Vector3(1, 1, 1));
    //     _sut.Insert(1, new Vector3(2, 2, 2));
    //
    //     // Store the initial state 
    //     var initialPosition = _sut.NodePositions[0];
    //     var initialSize = _sut.NodeSizes[0];
    //
    //     // Act
    //     _sut.Clear();
    //
    //     // Assert
    //     // Tree should be reset to just the root
    //     AssertThat(_sut.NodePositions.Count).IsEqual(1);
    //     AssertThat(_sut.NodeChildrenIndices[0].Count).IsEqual(0);
    //     AssertThat(_sut.NodeElementIndices[0].Count).IsEqual(0);
    //     // Root position and size should remain the same
    //     AssertThat(_sut.NodePositions[0]).IsEqual(initialPosition);
    //     AssertThat(_sut.NodeSizes[0]).IsEqual(initialSize);
    // }
    //
    // [TestCase]
    // public void Test_Search()
    // {
    //     // Arrange
    //     AssertThat(_sut).IsNotNull();
    //
    //     // Insert elements for searching
    //     _sut.Insert(0, new Vector3(0, 0, 0)); // Center point
    //     _sut.Insert(1, new Vector3(3, 3, 3)); // Close to center
    //
    //     // Act
    //     var result = _sut.Search(new Vector3(0, 0, 0), 5.0f);
    //
    //     // Assert
    //     AssertThat(result).IsNotNull();
    //     AssertThat(result.Count).IsEqual(2); // Should find both elements
    //     AssertThat(result).Contains(new List<int> { 0, 1 });
    // }
    //
    // [TestCase]
    // public void Test__Process()
    // {
    //     // Arrange
    //     AssertThat(_sut).IsNotNull();
    // }
}
