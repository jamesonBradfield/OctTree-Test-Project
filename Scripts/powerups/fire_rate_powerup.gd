extends Powerup

@export var flat_decrease_amount : float = 0.1

func on_powerup_add():
    Globals.player.set_fire_rate(-flat_decrease_amount)


func on_powerup_remove():
    Globals.player.set_fire_rate(flat_decrease_amount)

func on_powerup_stack():
    pass

func _process(_delta):
    pass

func _physics_process(_delta):
    pass

