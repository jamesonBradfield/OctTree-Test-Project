using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class BoidManagerTest
{
    private BoidManager _boidManager;
    private BoidResource _resource;

    [BeforeTest]
    public void Setup()
    {
        // Create a BoidResource for testing
        _resource = new BoidResource();
        _resource.MaxSpeed = 1.0f;
        _resource.MaxForce = 0.5f;
        _resource.AlignmentWeight = 1.0f;
        _resource.CohesionWeight = 0.8f;
        _resource.SeparationWeight = 1.2f;

        // Create a new BoidManager and add it to the scene tree
        _boidManager = AutoFree(new BoidManager());
        // AddChild(_boidManager);
        _boidManager.resource = _resource;
    }

    [AfterTest]
    public void Teardown()
    {
        // Clean up resources
        // RemoveChild(_boidManager);
        _boidManager.QueueFree();
    }

    [TestCase(1.0f, 0.0f, 0.0f, Description = "Moving along X axis")]
    [TestCase(0.0f, 1.0f, 0.0f, Description = "Moving along Y axis")]
    [TestCase(0.0f, 0.0f, 1.0f, Description = "Moving along Z axis")]
    [TestCase(1.0f, 1.0f, 1.0f, Description = "Moving diagonally")]
    public void TestVelocityNormalization(float vx, float vy, float vz)
    {
        // Add a boid with the specified velocity
        _boidManager.AddBoidData(vx, vy, vz);

        // Get the velocity
        Vector3 velocity = _boidManager.GetVelocity(0);

        // Verify velocity was normalized and scaled by MaxSpeed
        Vector3 expectedDirection = new Vector3(vx, vy, vz).Normalized();
        Vector3 expectedVelocity = expectedDirection * _resource.MaxSpeed;

        AssertThat(velocity.Length()).IsEqualApprox(_resource.MaxSpeed, 0.001f);
        AssertThat(velocity.Normalized()).IsEqualApprox(expectedDirection, new(0.001f, 0.001f, 0.001f));
    }

    [TestCase(new float[] { 1, 0, 0 }, new float[] { 0, 1, 0 }, Description = "Reflection from floor")]
    [TestCase(new float[] { 0, 1, 0 }, new float[] { 1, 0, 0 }, Description = "Reflection from wall")]
    [TestCase(new float[] { 0, 0, 1 }, new float[] { 0, 0, -1 }, Description = "Reflection from opposite direction")]
    public void TestHandleCollision(float[] velocityComponents, float[] normalComponents)
    {
        // Add a boid with velocity 
        Vector3 initialVelocity = new Vector3(velocityComponents[0], velocityComponents[1], velocityComponents[2]);
        _boidManager.AddBoidData(initialVelocity.X, initialVelocity.Y, initialVelocity.Z);

        // Handle collision with given normal
        Vector3 normal = new Vector3(normalComponents[0], normalComponents[1], normalComponents[2]);
        _boidManager.HandleCollision(0, normal);

        // Get velocity after collision
        Vector3 reflectedVelocity = _boidManager.GetVelocity(0);

        // Verify dot product changed sign
        float dotBefore = initialVelocity.Normalized().Dot(normal);
        float dotAfter = reflectedVelocity.Normalized().Dot(normal);

        // If initially moving toward normal, should now be moving away
        if (dotBefore > 0)
            AssertThat(dotAfter).IsLess(0);
        // If initially moving away from normal, direction shouldn't change much
        else
            AssertThat(dotAfter).IsLessEqual(0);

        // Verify magnitude is preserved (equal to MaxSpeed)
        AssertThat(reflectedVelocity.Length()).IsEqualApprox(_resource.MaxSpeed, 0.001f);
    }

    // [TestCase(new float[] { 2.0f, 5.0f, 10.0f }, 10.0f, Description = "Three rules with max 10")]
    // [TestCase(new float[] { 5.0f, 3.0f, 2.0f }, 5.0f, Description = "Three rules with max 5")]
    // [TestCase(new float[] { 1.0f }, 1.0f, Description = "Single rule")]
    // [TestCase(new float[] { }, 0.0f, Description = "No rules")]
    // public void TestGetMaxRuleRange(float[] ranges, float expectedMax)
    // {
    //     // Add test rules with the specified ranges
    //     for (int i = 0; i < ranges.Length; i++)
    //     {
    //         var rule = new TestRule { RangeValue = ranges[i] };
    //         _boidManager.AddChild(rule);
    //     }
    //
    //     // Force the Ready method to be called to collect child rules
    //     _boidManager._Ready();
    //
    //     // Verify max range is correct
    //     AssertThat(_boidManager.GetMaxRuleRange()).IsEqual(expectedMax);
    // }

    // // Helper class for testing
    // private class TestRule : BoidRule
    // {
    //     public float RangeValue { get; set; }
    //
    //     public override float GetRange()
    //     {
    //         return RangeValue;
    //     }
    // }
}
