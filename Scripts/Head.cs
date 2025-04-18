using Godot;

public partial class Head : Node3D
{
    [Export] public float Sensitivity = .006f;
    [Export] public float ControllerSensitivity = .05f;
    Vector2 CurrentControllerLook;

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

        GetParent<Node3D>().RotateY(CurrentControllerLook.X * ControllerSensitivity);
        GetNode<Camera3D>("Camera3D").RotateX(CurrentControllerLook.Y * ControllerSensitivity);
        Vector3 MinCameraRotation = new(-85, -90, -180);
        Vector3 MaxCameraRotation = new(85, 90, 180);
        GetNode<Camera3D>("Camera3D").Rotation.Clamp(MinCameraRotation, MaxCameraRotation);
    }

    public override void _UnhandledInput(InputEvent Event)
    {
        base._UnhandledInput(Event);
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (Event is InputEventMouseMotion)
            {
                InputEventMouseMotion mouseMotion = (InputEventMouseMotion)Event;
                GetParent<Node3D>().RotateY(-mouseMotion.Relative.X * Sensitivity);
                GetNode<Camera3D>("Camera3D").RotateX(-mouseMotion.Relative.Y * Sensitivity);
                // NOTE: Camera.Rotation is returned in radians, so we have to convert our euler angles to radians.
                Vector3 MinCameraClamp = new(Mathf.DegToRad(-85), Mathf.DegToRad(-360), Mathf.DegToRad(-360));
                Vector3 MaxCameraClamp = new(Mathf.DegToRad(85), Mathf.DegToRad(360), Mathf.DegToRad(360));
                GetNode<Camera3D>("Camera3D").Rotation = GetNode<Camera3D>("Camera3D").Rotation.Clamp(MinCameraClamp, MaxCameraClamp);
            }
        }
    }
}
