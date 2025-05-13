

using Godot;
using System.Collections.Generic;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
[TestSuite]
public class IntegrationTest
{
    [TestCase(50, Description = "Small number of boids")]
    [TestCase(100, Description = "Medium number of boids")]
    [TestCase(1000, Description = "Medium number of boids")]
    public void TestBoidManagerWithSpatialSystem(int boidCount)
    {
        // Create test components
        BoidManager boidManager = new BoidManager();
        DataOrientedOctTree spatialSystem = new DataOrientedOctTree();
        List<OctTreeElement> elements = new List<OctTreeElement>();

        try
        {
            // Configure BoidManager
            BoidResource resource = new BoidResource();
            resource.MaxSpeed = 1.0f;
            resource.MaxForce = 0.5f;
            boidManager.resource = resource;

            // Create test elements
            for (int i = 0; i < boidCount; i++)
            {
                // Create elements at evenly spaced positions
                Vector3 position = new Vector3(i * 2, 0, 0);
                elements.Add(new OctTreeElement(position, 1.0f));

                // Add corresponding boid data
                boidManager.AddBoidData(1.0f, 0.0f, 0.0f);
            }

            // Initialize spatial system
            spatialSystem.Initialize(Vector3.Zero, 20.0f, 2, (index) => elements[index]);

            // Add elements to spatial system
            List<int> indices = new List<int>();
            for (int i = 0; i < elements.Count; i++)
            {
                indices.Add(i);
            }
            spatialSystem.Insert(indices);

            // Update boid behaviors
            boidManager.UpdateBoidBehaviors(elements, spatialSystem);

            // Verify that all velocities were updated and are within proper bounds
            for (int i = 0; i < elements.Count; i++)
            {
                Vector3 velocity = boidManager.GetVelocity(i);
                AssertThat(velocity.Length()).IsLessEqual(resource.MaxSpeed * 1.01f);
            }
        }
        finally
        {
            // Clean up
            boidManager.QueueFree();
            spatialSystem.QueueFree();
        }
    }

    [TestCase(
        new float[] { 1.0f, 1.0f, 1.0f }, // AlignmentWeight, CohesionWeight, SeparationWeight
        new float[] { 5.0f, 5.0f, 2.0f }, // AlignmentRange, CohesionRange, SeparationRange
        Description = "Default weights and ranges"
    )]
    [TestCase(
        new float[] { 0.0f, 1.0f, 1.0f }, // AlignmentWeight = 0 (disabled)
        new float[] { 5.0f, 5.0f, 2.0f },
        Description = "Alignment disabled"
    )]
    [TestCase(
        new float[] { 1.0f, 0.0f, 1.0f }, // CohesionWeight = 0 (disabled)
        new float[] { 5.0f, 5.0f, 2.0f },
        Description = "Cohesion disabled"
    )]
    [TestCase(
        new float[] { 1.0f, 1.0f, 0.0f }, // SeparationWeight = 0 (disabled)
        new float[] { 5.0f, 5.0f, 2.0f },
        Description = "Separation disabled"
    )]
    public void TestRuleWeightsAndRanges(float[] weights, float[] ranges)
    {
        // Create components
        BoidManager boidManager = new BoidManager();


        try
        {
            // Configure BoidManager
            BoidResource resource = new BoidResource();
            resource.MaxSpeed = 1.0f;
            resource.MaxForce = 0.5f;

            // Set weights and ranges
            resource.AlignmentWeight = weights[0];
            resource.CohesionWeight = weights[1];
            resource.SeparationWeight = weights[2];

            resource.AlignmentRange = ranges[0];
            resource.CohesionRange = ranges[1];
            resource.SeparationRange = ranges[2];

            boidManager.resource = resource;

            // Create rules
            AlignmentRule alignmentRule = new AlignmentRule();
            CohesionRule cohesionRule = new CohesionRule();
            SeparationRule separationRule = new SeparationRule();

            alignmentRule.resource = resource;
            cohesionRule.resource = resource;
            separationRule.resource = resource;

            // Add rules to manager
            boidManager.AddChild(alignmentRule);
            boidManager.AddChild(cohesionRule);
            boidManager.AddChild(separationRule);

            // Call _Ready to collect rules
            boidManager._Ready();

            // Create test scenario with two boids
            List<OctTreeElement> elements = new List<OctTreeElement>();
            elements.Add(new OctTreeElement(new Vector3(0, 0, 0), 1.0f));
            elements.Add(new OctTreeElement(new Vector3(1, 0, 0), 1.0f));

            // Add boid data
            boidManager.AddBoidData(1.0f, 0.0f, 0.0f);
            boidManager.AddBoidData(-1.0f, 0.0f, 0.0f);

            // Create simple list of neighbors
            List<int> neighbors = new List<int> { 0, 1 };

            // Calculate forces for each rule
            Vector3 alignmentForce = alignmentRule.CalculateForce(0, neighbors, elements,
                new List<Vector3> { boidManager.GetVelocity(0), boidManager.GetVelocity(1) });

            Vector3 cohesionForce = cohesionRule.CalculateForce(0, neighbors, elements,
                new List<Vector3> { boidManager.GetVelocity(0), boidManager.GetVelocity(1) });

            Vector3 separationForce = separationRule.CalculateForce(0, neighbors, elements,
                new List<Vector3> { boidManager.GetVelocity(0), boidManager.GetVelocity(1) });

            // Verify weights affect forces
            if (weights[0] == 0)
                AssertThat(alignmentForce.Length()).IsEqual(0);

            if (weights[1] == 0)
                AssertThat(cohesionForce.Length()).IsEqual(0);

            if (weights[2] == 0)
                AssertThat(separationForce.Length()).IsEqual(0);
        }
        finally
        {
            // Clean up
            boidManager.QueueFree();
        }
    }
}
