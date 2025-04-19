using Godot;

public partial class WalkState : State
{
    float WalkSpeed = 8.5f;
    float SprintSpeed = 8.5f;
    float GroundAccel = 14.0f;
    float GroundDecel = 10f;
    float GroundFriction = 6.0f;
    float HeadbobAmplitude = 0.06f;
    float HeadbobFrequency = 2.4f;
    float HeadbobTime = 0.0f;
    Player player;
    Vector2 inputDir;

    public override void Enter()
    {
        player = fsm.GetParent<Player>();
    }

    public override void Exit()
    {

    }

    public override void Update(float delta)
    {
        inputDir = Input.GetVector("left", "right", "forward", "backward").Normalized();
        if (inputDir == Vector2.Zero)
            fsm.TransitionTo("Idle");
        if (player.IsOnFloor() && (Input.IsActionJustPressed("jump") || (player.AutoBhop && Input.IsActionJustPressed("jump"))))
            fsm.TransitionTo("Jumping");
    }

    public override void PhysicsUpdate(float delta)
    {
        base.PhysicsUpdate(delta);
        player.WishDir = player.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y);

        var CurrentSpeedInWishDir = player.Velocity.Dot(player.WishDir);
        var SpeedLeftTillCap = WalkSpeed - CurrentSpeedInWishDir;
        if (SpeedLeftTillCap > 0)
        {
            float AccelSpeed = (float)(GroundAccel * delta * WalkSpeed);
            AccelSpeed = Mathf.Min(AccelSpeed, SpeedLeftTillCap);
            player.Velocity += AccelSpeed * player.WishDir;

            var Control = Mathf.Max(player.Velocity.Length(), GroundDecel);
            var Drop = Control * GroundFriction * delta;
            var new_speed = Mathf.Max(player.Velocity.Length() - Drop, 0.0);
            if (player.Velocity.Length() > 0)
                new_speed /= player.Velocity.Length();
            player.Velocity *= (float)new_speed;

            HeadbobEffect(delta);
        }
    }

    private void HeadbobEffect(double delta)
    {
        HeadbobTime += (float)delta * player.Velocity.Length();
        Transform3D CameraTransform = player.Camera.Transform;
        CameraTransform.Origin = new Vector3(
        Mathf.Cos(HeadbobTime * HeadbobFrequency * 0.5f) * HeadbobAmplitude,
        Mathf.Sin(HeadbobTime * HeadbobFrequency) * HeadbobAmplitude,
        0
    );
    }
}
