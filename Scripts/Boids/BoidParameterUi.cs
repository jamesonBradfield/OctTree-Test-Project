using Godot;

public partial class BoidParameterUi : Control
{
    private BoidManager boidManager;

    // Sliders
    private HSlider alignmentRangeSlider;
    private HSlider cohesionRangeSlider;
    private HSlider separationRangeSlider;
    private HSlider maxForceSlider;
    private HSlider maxSpeedSlider;
    private HSlider alignmentWeightSlider;
    private HSlider cohesionWeightSlider;
    private HSlider separationWeightSlider;
    private HSlider followWeightSlider;
    private HSlider followRangeSlider;
    private HSlider octTreeRefreshIntervalSlider;
    private HSlider octSizeSlider;
    private HSlider boidCountSlider;
    private Button showOctTree;
    private Button restartSimulation;
    // Value labels
    private Label alignmentRangeValue;
    private Label cohesionRangeValue;
    private Label separationRangeValue;
    private Label maxForceValue;
    private Label maxSpeedValue;
    private Label alignmentWeightValue;
    private Label cohesionWeightValue;
    private Label followWeightValue;
    private Label separationWeightValue;

    private Label octTreeRefreshIntervalValue;
    private Label octSizeValue;
    private Label boidCountValue;
    public override void _Ready()
    {
        // Get the boid manager reference
        boidManager = GetNode<BoidManager>("/root/OctTreeTest/BoidManager");

        // Get all slider and label references
        alignmentRangeSlider = GetNode<HSlider>("TabContainer/Boid Settings/AlignmentRange/HSlider");
        cohesionRangeSlider = GetNode<HSlider>("TabContainer/Boid Settings/CohesionRange/HSlider");
        separationRangeSlider = GetNode<HSlider>("TabContainer/Boid Settings/SeparationRange/HSlider");
        followRangeSlider = GetNode<HSlider>("TabContainer/Boid Settings/FollowRange/HSlider");
        maxForceSlider = GetNode<HSlider>("TabContainer/Boid Settings/MaxForce/HSlider");
        maxSpeedSlider = GetNode<HSlider>("TabContainer/Boid Settings/MaxSpeed/HSlider");
        alignmentWeightSlider = GetNode<HSlider>("TabContainer/Boid Settings/AlignmentWeight/HSlider");
        cohesionWeightSlider = GetNode<HSlider>("TabContainer/Boid Settings/CohesionWeight/HSlider");
        separationWeightSlider = GetNode<HSlider>("TabContainer/Boid Settings/SeparationWeight/HSlider");
        followWeightSlider = GetNode<HSlider>("TabContainer/Boid Settings/FollowWeight/HSlider");
        octSizeSlider = GetNode<HSlider>("TabContainer/OctTree Settings/OctSize/HSlider");

        octTreeRefreshIntervalSlider = GetNode<HSlider>("TabContainer/OctTree Settings/RefreshInterval/HSlider");
        boidCountSlider = GetNode<HSlider>("TabContainer/OctTree Settings/BoidCount/HSlider");

        alignmentRangeValue = GetNode<Label>("TabContainer/Boid Settings/AlignmentRange/Value");
        cohesionRangeValue = GetNode<Label>("TabContainer/Boid Settings/CohesionRange/Value");
        separationRangeValue = GetNode<Label>("TabContainer/Boid Settings/SeparationRange/Value");
        maxForceValue = GetNode<Label>("TabContainer/Boid Settings/MaxForce/Value");
        maxSpeedValue = GetNode<Label>("TabContainer/Boid Settings/MaxSpeed/Value");
        alignmentWeightValue = GetNode<Label>("TabContainer/Boid Settings/AlignmentWeight/Value");
        cohesionWeightValue = GetNode<Label>("TabContainer/Boid Settings/CohesionWeight/Value");
        separationWeightValue = GetNode<Label>("TabContainer/Boid Settings/SeparationWeight/Value");
        followWeightValue = GetNode<Label>("TabContainer/Boid Settings/FollowWeight/Value");
        octSizeValue = GetNode<Label>("TabContainer/OctTree Settings/OctSize/Value");
        boidCountValue = GetNode<Label>("TabContainer/OctTree Settings/BoidCount/Value");
        octTreeRefreshIntervalValue = GetNode<Label>("TabContainer/OctTree Settings/RefreshInterval/Value");
        showOctTree = GetNode<Button>("TabContainer/OctTree Settings/ShowOctTree/Button");
        restartSimulation = GetNode<Button>("TabContainer/OctTree Settings/RestartSimulation/Button");
        // Initialize slider values from BoidManager
        alignmentRangeSlider.Value = boidManager.resource.AlignmentRange;
        cohesionRangeSlider.Value = boidManager.resource.CohesionRange;
        separationRangeSlider.Value = boidManager.resource.SeparationRange;
        maxForceSlider.Value = boidManager.resource.MaxForce;
        maxSpeedSlider.Value = boidManager.resource.MaxSpeed;
        alignmentWeightSlider.Value = boidManager.resource.AlignmentWeight;
        cohesionWeightSlider.Value = boidManager.resource.CohesionWeight;
        separationWeightSlider.Value = boidManager.resource.SeparationWeight;
        followWeightSlider.Value = boidManager.resource.FollowWeight;
        octTreeRefreshIntervalSlider.Value = boidManager.resource.octreeRefreshInterval;
        // Connect signals
        alignmentRangeSlider.ValueChanged += OnAlignmentRangeChanged;
        cohesionRangeSlider.ValueChanged += OnCohesionRangeChanged;
        separationRangeSlider.ValueChanged += OnSeparationRangeChanged;
        maxForceSlider.ValueChanged += OnMaxForceChanged;
        maxSpeedSlider.ValueChanged += OnMaxSpeedChanged;
        alignmentWeightSlider.ValueChanged += OnAlignmentWeightChanged;
        cohesionWeightSlider.ValueChanged += OnCohesionWeightChanged;
        separationWeightSlider.ValueChanged += OnSeparationWeightChanged;
        followWeightSlider.ValueChanged += OnFollowWeightChanged;
        octSizeSlider.ValueChanged += OnOctSizeChanged;
        boidCountSlider.ValueChanged += OnBoidCountChanged;
        showOctTree.Pressed += ShowOctTree;
        restartSimulation.Pressed += RestartSimulation;
        octTreeRefreshIntervalSlider.ValueChanged += OnRefreshIntervalChanged;
        // Update labels
        UpdateAllLabels();
    }
    private void RestartSimulation()
    {
        GetParent<OctTreeTestSceneSetup>().RestartSimulation();
    }
    private void ShowOctTree()
    {
        boidManager.Octree.ToggleDebug();
    }

    public void UpdateAllLabels()
    {
        alignmentRangeValue.Text = "Alignment Range : " + alignmentRangeSlider.Value.ToString("0.0");
        cohesionRangeValue.Text = "Cohesion Range : " + cohesionRangeSlider.Value.ToString("0.0");
        separationRangeValue.Text = "Separation Range : " + separationRangeSlider.Value.ToString("0.0");
        maxForceValue.Text = "Max Force : " + maxForceSlider.Value.ToString("0.0");
        maxSpeedValue.Text = "Max Speed : " + maxSpeedSlider.Value.ToString("0.0");
        alignmentWeightValue.Text = "Alignment Weight : " + alignmentWeightSlider.Value.ToString("0.0");
        cohesionWeightValue.Text = "Cohesion Weight : " + cohesionWeightSlider.Value.ToString("0.0");
        separationWeightValue.Text = "Separation Weight : " + separationWeightSlider.Value.ToString("0.0");
        followWeightValue.Text = "Follow Weight : " + followWeightSlider.Value.ToString("0.0");
        octSizeValue.Text = "Oct Size(needs restart) : " + octSizeSlider.Value.ToString("0.0");
        boidCountValue.Text = "Boid Count(needs restart) : " + boidCountSlider.Value.ToString("0.0");
        octTreeRefreshIntervalValue.Text = "Refresh Interval : " + octTreeRefreshIntervalSlider.Value.ToString("0.0");
    }

    // Event handlers
    private void OnAlignmentRangeChanged(double value)
    {
        boidManager.resource.AlignmentRange = (float)value;
        alignmentRangeValue.Text = "Alignment Range : " + value.ToString("0.0");
    }

    private void OnCohesionRangeChanged(double value)
    {
        boidManager.resource.CohesionRange = (float)value;
        cohesionRangeValue.Text = "Cohesion Range : " + value.ToString("0.0");
    }

    private void OnSeparationRangeChanged(double value)
    {
        boidManager.resource.SeparationRange = (float)value;
        separationRangeValue.Text = "Separation Range : " + value.ToString("0.0");
    }

    private void OnMaxForceChanged(double value)
    {
        boidManager.resource.MaxForce = (float)value;
        maxForceValue.Text = "Max Force : " + value.ToString("0.0");
    }

    private void OnMaxSpeedChanged(double value)
    {
        boidManager.resource.MaxSpeed = (float)value;
        maxSpeedValue.Text = "Max Speed : " + value.ToString("0.0");
    }

    private void OnAlignmentWeightChanged(double value)
    {
        boidManager.resource.AlignmentWeight = (float)value;
        alignmentWeightValue.Text = "Alignment Weight : " + value.ToString("0.0");
    }

    private void OnCohesionWeightChanged(double value)
    {
        boidManager.resource.CohesionWeight = (float)value;
        cohesionWeightValue.Text = "Cohesion Weight : " + value.ToString("0.0");
    }

    private void OnSeparationWeightChanged(double value)
    {
        boidManager.resource.SeparationWeight = (float)value;
        separationWeightValue.Text = "Separation Weight : " + value.ToString("0.0");
    }

    private void OnFollowWeightChanged(double value)
    {
        boidManager.resource.FollowWeight = (float)value;
        followWeightValue.Text = "follow Weight : " + value.ToString("0.0");
    }

    private void OnOctSizeChanged(double value)
    {
        boidManager.resource.rootOctSize = (float)value;
        octSizeValue.Text = "Oct Size(needs restart) : " + value.ToString("0.0");
    }


    private void OnBoidCountChanged(double value)
    {
        boidManager.resource.count = (int)value;
        boidCountValue.Text = "Boid Count(needs restart) : " + value.ToString("0.0");
    }

    private void OnRefreshIntervalChanged(double value)
    {
        boidManager.resource.octreeRefreshInterval = (int)value;
        octTreeRefreshIntervalValue.Text = "Refresh Interval : " + value.ToString("0.0");
    }
}
