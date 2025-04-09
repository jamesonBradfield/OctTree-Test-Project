using Godot;

public partial class Damageable : Node
{

    [Export] public int MaxHealth = 100;
    [Export] public int CurrentHealth;
    enum HealthState { ALIVE, DEAD };
    HealthState Health = HealthState.ALIVE;

    public override void _EnterTree()
    {
        CurrentHealth = MaxHealth;
        Health = HealthState.ALIVE;
    }

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        Health = HealthState.ALIVE;
    }
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            Destroy();
        }
    }

    private void Heal(int HealValue)
    {
        if (CurrentHealth + HealValue <= MaxHealth)
        {
            CurrentHealth += HealValue;
        }
        else
        {
            CurrentHealth = MaxHealth;
        }
    }

    private void Destroy()
    {
        Health = HealthState.DEAD;
    }
}
