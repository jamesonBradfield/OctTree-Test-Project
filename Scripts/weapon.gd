class_name Weapon
extends Node3D

var damage : int
var max_ammo : int
var current_ammo : int
var ammo_consumed_at_once : int
var shooting_range : Vector3
var gun_node
@onready var shot_timer : Timer = $ShotTimer
@onready var reload_timer : Timer = $ReloadTimer
@onready var raycast : RayCast3D = $RayCast3D
@onready var shooting_player : AudioStreamPlayer3D = $ShootPlayer3D
var ammo_indicator : Label
const WEAPON_SWAY_MOVE_AMOUNT = 0.035
const WEAPON_SWAY_FREQUENCY = 1.4
var sway_time := 0.0

func _ready() -> void:
    ammo_indicator = Globals.ui.ammo

func set_values(weapon_resource : WeaponResource):
    shot_timer.one_shot = true
    reload_timer.one_shot = true
    reload_timer.timeout.connect(func(): 
        current_ammo = max_ammo
        ammo_indicator.text = str(current_ammo,"/",max_ammo)
    )
    shot_timer.wait_time = weapon_resource.fire_rate
    reload_timer.wait_time = weapon_resource.reload_time
    raycast.position = weapon_resource.muzzle_point
    shooting_player.stream = weapon_resource.shoot_audio_stream
    self.shooting_range = Vector3.FORWARD * weapon_resource.shooting_range
    self.ammo_consumed_at_once = weapon_resource.ammo_consumed_at_once
    self.damage = weapon_resource.damage
    self.max_ammo = weapon_resource.max_ammo
    self.current_ammo = max_ammo
    gun_node = weapon_resource.gun_scene.instantiate()
    gun_node.position = weapon_resource.scene_position
    add_child(gun_node)
    ammo_indicator.text = str(current_ammo,"/",max_ammo)


func _process(delta):
    sway_time += delta
    self.transform.origin = Vector3(
        sin(sway_time * WEAPON_SWAY_FREQUENCY * 0.5) * WEAPON_SWAY_MOVE_AMOUNT,
        cos(sway_time * WEAPON_SWAY_FREQUENCY) * WEAPON_SWAY_MOVE_AMOUNT,
        0
    )

func set_target_to_camera_hit_point(hit_point : Vector3):
    raycast.target_position = to_local(hit_point)
    
func raycast_from_muzzle():
    # succeed shooting
    if current_ammo > 0:
        shot_timer.start()
        shooting_player.play()
        if raycast.get_collider():
           raycast.get_collider().take_damage(damage) 
        current_ammo -= ammo_consumed_at_once
        ammo_indicator.text = str(current_ammo,"/",max_ammo)
    else:
        # fail shooting
        reload_timer.start()
        pass

func can_shoot() -> bool:
    if reload_timer.is_stopped() and shot_timer.is_stopped():
        return true
    else:
        return false
