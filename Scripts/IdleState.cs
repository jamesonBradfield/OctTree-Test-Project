using Godot;

public partial class IdleState : State
{
    float Sensitivity = .006f;
    Player player;
    Vector3 WishDir = Vector3.Zero;
    Vector2 CurrentControllerLook;
    float ControllerSensitivity = .05f;

    public override void Enter()
    {
        player = fsm.GetParent<Player>();
    }

    public override void Exit()
    {

    }

    public override void Update(float delta)
    {
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        if (inputDir != Vector2.Zero)
            fsm.TransitionTo("Walk");
        if (!player.IsOnFloor())
        {
            fsm.TransitionTo("InAir");
        }
        else if (player.IsOnFloor() && (Input.IsActionJustPressed("jump") || (player.AutoBhop && Input.IsActionJustPressed("jump"))))
        {
            fsm.TransitionTo("Jumping");
        }
    }

    public override void PhysicsUpdate(float delta)
    {
        player.MoveAndSlide();
    }
}
