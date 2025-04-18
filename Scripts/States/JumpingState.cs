using Godot;
public partial class JumpingState : State
{
    Player player;
    float JumpVelocity = 6.0f;
    public override void Enter()
    {
        player = fsm.GetParent<Player>();
        player.Velocity = new(player.Velocity.X, JumpVelocity, player.Velocity.Z);
        // player.GetNode<AudioStreamPlayer3D>("FootstepPlayer").PitchScale = (float)GD.RandRange(0.90, 1.10);
        // player.GetNode<AudioStreamPlayer3D>("FootstepPlayer").Play();
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
