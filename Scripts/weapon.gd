class_name Weapon
extends Node3D

var damage : int
var max_ammo : int
var current_ammo : int
var ammo_consumed_at_once : int
var shooting_range : Vector3
@onready var shot_timer : Timer = $ShotTimer
@onready var reload_timer : Timer = $ReloadTimer
@onready var raycast : RayCast3D = $RayCast3D
@onready var ammo_indicator: Label = $/root/Main/UI/PlayerViewUI/PanelContainer/MarginContainer/VBoxContainer/AmmoIndicator
@onready var mesh_instance : MeshInstance3D = $MeshInstance3D
@onready var shooting_player : AudioStreamPlayer3D = $ShootPlayer3D

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
    self.mesh_instance.mesh = weapon_resource.mesh
    ammo_indicator.text = str(current_ammo,"/",max_ammo)

func raycast_from_muzzle(hit_point : Vector3):
    # succeed shooting
    if current_ammo > 0:
        shot_timer.start()
        shooting_player.play()
        if to_local(hit_point) <= shooting_range:
            raycast.target_position = to_local(hit_point)
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
