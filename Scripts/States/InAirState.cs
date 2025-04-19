using Godot;

public partial class InAirState : State
{
    float AirMoveSpeed = 500.0f;
    float AirCap = 0.85f;
    float AirAccel = 800.0f;
    Player player;
    Vector2 inputDir;
    AudioStreamPlayer3D landPlayer;

    public override void Ready()
    {
        base.Ready();
        fsm = GetParent<StateMachine>();
        player = fsm.GetParent<Player>();
        landPlayer = player.GetNode<AudioStreamPlayer3D>("LandPlayer");
    }

    public override void Enter()
    {
        player = fsm.GetParent<Player>();
        landPlayer = player.GetNode<AudioStreamPlayer3D>("LandPlayer");
        player.GetNode<Node>("CameraShake3DNode").Call("_init_camera_shake");
    }

    public override void Exit()
    {
        landPlayer.PitchScale = (float)GD.RandRange(0.9, 1.1);
        landPlayer.Play();
        player.GetNode<Node>("CameraShake3DNode").Call("_shake");
    }

    public override void Update(float delta)
    {
        if (player.IsOnFloor())
        {
            if (Input.IsActionPressed("jump") && player.AutoBhop == true)
                fsm.TransitionTo("Jumping");
            else if (inputDir != Vector2.Zero)
                fsm.TransitionTo("Walk");
            else
                fsm.TransitionTo("Idle");
        }
    }

    public override void PhysicsUpdate(float delta)
    {
        base.PhysicsUpdate(delta);
        inputDir = Input.GetVector("left", "right", "forward", "backward").Normalized();
        player.WishDir = player.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y);
        player.Velocity -= (new Vector3(0f, 12.0f, 0f) * delta);
        var CurrentSpeedInWishDirection = player.Velocity.Dot(player.WishDir);
        var CappedSpeed = Mathf.Min((AirMoveSpeed * player.WishDir).Length(), AirCap);
        var SpeedLeftTillCap = CappedSpeed - CurrentSpeedInWishDirection;
        if (SpeedLeftTillCap > 0)
        {
            var AccelSpeed = AirAccel * AirMoveSpeed * delta;
            AccelSpeed = Mathf.Min(AccelSpeed, SpeedLeftTillCap);
            player.Velocity += (float)AccelSpeed * player.WishDir;
        }
    }
}
