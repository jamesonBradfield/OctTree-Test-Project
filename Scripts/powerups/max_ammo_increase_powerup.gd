extends Powerup

@export var flat_increase_amount : float = 2

func on_powerup_add():
    Globals.player.get_node("Head/Camera3D/WeaponManager").max_ammo += flat_increase_amount


func on_powerup_remove():
    Globals.player.get_node("Head/Camera3D/WeaponManager").max_ammo -= flat_increase_amount

func on_powerup_stack():
    pass

func _process(_delta):
    pass

func _physics_process(_delta):
    pass

