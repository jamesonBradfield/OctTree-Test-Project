using Godot;
public partial class DecelEffect : Effect
{
    [Export] float Value;
    public override void _EnterTree()
    {
        base._EnterTree();
        player.WalkSpeed -= Value;
    }


    public override void _ExitTree()
    {
        base._ExitTree();
        player.WalkSpeed += Value;
    }
}
