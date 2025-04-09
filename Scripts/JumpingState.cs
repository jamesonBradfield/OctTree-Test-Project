using Godot;
using System;

public partial class JumpingState : State
{
    Player player;
    public override void Enter()
    {
        player = fsm.GetParent<Player>();
    }

    public override void Exit()
    {

    }
}
