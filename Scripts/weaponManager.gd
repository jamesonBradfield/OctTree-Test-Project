extends Node3D

# store a array of the unlocked weapons
@export var equipped_weapons : Array[WeaponResource]
@export var current_weapon : WeaponResource
@onready var weapon : Weapon = $Weapon
@onready var camera_raycast : RayCast3D = $"../RayCast3D"
# TODO: need a function to parse a resource for our weapons. (weapon resource should define shared weapon values; fire_rate,damage,muzzle_point,max_ammo,current_ammo,range)
func _ready():
    parse_resource_file()

func parse_resource_file():
    weapon.set_values(current_weapon)

func _process(_delta):
    weapon.set_target_to_camera_hit_point(raycast_from_camera())

func _unhandled_input(event: InputEvent) -> void:
    if event.is_action_pressed("shoot"):
        if weapon.can_shoot():
            weapon.raycast_from_muzzle()
    

func raycast_from_camera() -> Vector3:
    return camera_raycast.get_collision_point()


