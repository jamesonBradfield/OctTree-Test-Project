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
    float Sensitivity = .006f;
    float ControllerSensitivity = .05f;
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

    Vector3 WishDir = Vector3.Zero;
    Vector2 CurrentControllerLook;

    public override void _Ready()
    {
        HealthBar.MaxValue = Damageable.CurrentHealth;
        HealthBar.Value = Damageable.CurrentHealth;
        StartingFov = Camera.Fov;
    }
    public override void _Process(double delta)
    {
        var target_look = Input.GetVector("look_right", "look_left", "look_down", "look_up");
        if (target_look.Length() < CurrentControllerLook.Length())
        {
            CurrentControllerLook = target_look;
        }
        else
        {
            CurrentControllerLook = CurrentControllerLook.Lerp(target_look, 5.0f * (float)delta);
        }

        this.RotateY(CurrentControllerLook.X * ControllerSensitivity);
        this.Camera.RotateX(CurrentControllerLook.Y * ControllerSensitivity);
        Vector3 MinCameraRotation = new(-180, -90, -180);
        Vector3 MaxCameraRotation = new(180, 90, 180);
        this.Camera.Rotation.Clamp(MinCameraRotation, MaxCameraRotation);
    }
    public override void _UnhandledInput(InputEvent Event)
    {
        base._UnhandledInput(Event);
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (Event is InputEventMouseMotion)
            {
                InputEventMouseMotion mouseMotion = (InputEventMouseMotion)Event;
                this.RotateY(-mouseMotion.Relative.X * Sensitivity);
                this.Camera.RotateX(-mouseMotion.Relative.Y * Sensitivity);
                Vector3 MinCameraClamp = new(-180, -90, -180);
                Vector3 MaxCameraClamp = new(180, 90, 180);
                this.Camera.Rotation.Clamp(MinCameraClamp, MaxCameraClamp);
            }
        }
    }
}
