using Godot;
[GlobalClass]
public partial class Effect : Node
{
    [Export] float duration;
    Timer durationTimer;
    protected Player player;
    public override void _EnterTree()
    {
        player = GetNode<Player>("/root/Main/Player");
        durationTimer = new Timer();
        this.AddChild(durationTimer);
        durationTimer.WaitTime = duration;
        durationTimer.Start();
        durationTimer.Connect("timeout", new Callable(this, "RemoveEffect"));
    }

    public void RemoveEffect()
    {
        QueueFree();
    }

    public override void _ExitTree()
    {

    }
}
