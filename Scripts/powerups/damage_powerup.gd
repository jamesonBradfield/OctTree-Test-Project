extends Powerup

@export var flat_increase_amount : float = 2

func on_powerup_add():
    print("damage powerup added")
    Globals.weapon.set_damage(flat_increase_amount)
    print("damage set to ",Globals.weapon.damage )

func on_powerup_remove():
    print("damage powerup removed")
    Globals.weapons.set_damage(-flat_increase_amount)
    print("damage set to ",Globals.weapon.damage )

func on_powerup_stack():
    pass

func _process(_delta):
    pass

func _physics_process(_delta):
    pass

