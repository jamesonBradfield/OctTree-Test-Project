using Godot;
[GlobalClass]
public partial class State : Node
{
    public StateMachine fsm;
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Ready() { }
    public virtual void Update(float delta) { }
    public virtual void PhysicsUpdate(float delta) { }
    public virtual void HandleInput(InputEvent @event) { }
    public override string ToString(){
        return this.Name;
    }
}
