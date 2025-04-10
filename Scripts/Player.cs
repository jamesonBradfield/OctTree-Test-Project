using Godot;

public partial class Player : CharacterBody3D
{
    [Export] Node3D Head;
    [Export] public Camera3D Camera;
    [Export] Damageable Damageable;
    [Export] Label VelocityIndicator;
    [Export] ProgressBar HealthBar;
    [Export] PanelContainer PausePanel;
    [Export] PanelContainer DeathPanel;
    [Export] AudioStreamPlayer3D BhopPlayer;
    [Export] AudioStreamPlayer3D FootstepPlayer;
    float StartingFov;
    float MovingFastFov = 80f;
    float JumpVelocity = 6.0f;
    const float Speed = 5.0f;
    public bool AutoBhop = true;
    float HeadbobAmplitude = 0.06f;
    float HeadbobFrequency = 2.4f;
    float HeadbobTime = 0.0f;

    // air movement settings
    float AirCap = 0.85f;
    float AirAccel = 800.0f;
    float AirMoveSpeed = 500.0f;

    // ground movement settings
    float WalkSpeed = 8.5f;
    float SprintSpeed = 8.5f;
    float GroundAccel = 14.0f;
    float GroundDecel = 10f;
    float GroundFriction = 6.0f;
    StateMachine fsm;
    Vector3 WishDir = Vector3.Zero;

    public override void _Ready()
    {
        HealthBar.MaxValue = Damageable.CurrentHealth;
        HealthBar.Value = Damageable.CurrentHealth;
        StartingFov = Camera.Fov;
        fsm = GetNode<StateMachine>("FSM");
    }
}
