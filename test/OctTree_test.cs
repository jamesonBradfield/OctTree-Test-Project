namespace GodotGame;
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class OctTreeTest
{
    private OctTree _sut;


    [Before]
    public void Setup()
    {
        _sut = AutoFree(new OctTree(new(new(0, 0, 0), new(100f, 100f, 100f)), 2));
        AssertThat(_sut).IsNotNull();
    }

    [After]
    public void TearDown()
    {
        // Clean up resources if needed
    }

    [TestCase(99f, 99f, 99f, 1f, 1f, 1f)]
    [TestCase(0f, 0f, 0f, 1f, 1f, 1f)]
    [TestCase(0f, 99f, 0f, 1f, 1f, 1f)]
    [TestCase(99f, 0f, 0f, 1f, 1f, 1f)]
    [TestCase(99f, 99f, 0f, 1f, 1f, 1f)]
    [TestCase(0f, 99f, 99f, 1f, 1f, 1f)]
    public void Test_Insert_single_element_at_edges(float x, float y, float z, float sx, float sy, float sz)
    {
        // Arrange
        AssertThat(_sut).IsNotNull();
        Aabb inserted_boid = new(new(x, y, z), new(sx, sy, sz));
        // Act
        _sut.Insert(inserted_boid);

        // Assert
        // TODO: Add assertions for Insert
        AssertThat(_sut.elements.Find(inserted_boid)).IsNotNull();
        _sut = AutoFree(new OctTree(new(new(0, 0, 0), new(100f, 100f, 100f)), 2));
    }

    [TestCase]
    public void Test_Insert_split()
    {
        GD.Print("Starting Test_Insert_split");
        // Arrange
        AssertThat(_sut).IsNotNull();
        Aabb inserted_boid1 = new(new(60, 50, 60), new(1, 1, 1));
        Aabb inserted_boid2 = new(new(70, 60, 70), new(1, 1, 1));
        Aabb inserted_boid3 = new(new(96, 96, 96), new(1, 1, 1));
        Aabb inserted_boid4 = new(new(35, 35, 49), new(1, 1, 1));
        Aabb inserted_boid5 = new(new(20, 30, 30), new(1, 1, 1));
        Aabb inserted_boid6 = new(new(40, 40, 20), new(1, 1, 1));
        // Act
        _sut.Insert(inserted_boid1);
        _sut.Insert(inserted_boid2);
        _sut.Insert(inserted_boid3);
        _sut.Insert(inserted_boid4);
        _sut.Insert(inserted_boid5);
        _sut.Insert(inserted_boid6);
        // Assert
        // TODO: Add assertions for Insert
        AssertThat(_sut.elements.Find(inserted_boid1)).IsNotNull();
        AssertThat(_sut.elements.Find(inserted_boid2)).IsNotNull();
        AssertThat(_sut.elements.Find(inserted_boid3)).IsNotNull();
        AssertThat(_sut.elements.Find(inserted_boid4)).IsNotNull();
        AssertThat(_sut.elements.Find(inserted_boid5)).IsNotNull();
        AssertThat(_sut.elements.Find(inserted_boid6)).IsNotNull();
        AssertThat(_sut.elements.Count).IsEqual(6);
        AssertThat(_sut.nodes.Count).IsGreater(1);
        _sut = AutoFree(new OctTree(new(new(0, 0, 0), new(100f, 100f, 100f)), 2));
    }
    [TestCase]
    public void Test__Process()
    {
        // Arrange
        AssertThat(_sut).IsNotNull();

        // Act
        _sut._Process(0.0);

        // Assert
        // TODO: Add assertions for _Process
    }

    [TestCase]
    public void Test_Log()
    {
        // Arrange
        AssertThat(_sut).IsNotNull();

        // Act
        _sut.Log("");

        // Assert
        // TODO: Add assertions for Log
    }
}
