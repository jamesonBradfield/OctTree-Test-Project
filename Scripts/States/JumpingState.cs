using Godot;
public partial class JumpingState : State
{
    Player player;
    float JumpVelocity = 6.0f;
    public override void Enter()
    {
        player = fsm.GetParent<Player>();
        player.Velocity = new(player.Velocity.X, JumpVelocity, player.Velocity.Z);
        fsm.TransitionTo("InAir");
    }
    public override void PhysicsUpdate(float delta)
    {
        base.PhysicsUpdate(delta);
    }

    public override void Exit()
    {

    }
}
