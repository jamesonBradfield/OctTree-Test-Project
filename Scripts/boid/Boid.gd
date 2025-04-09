class_name Boid
extends Node3D

@export_group("Core Parameters")
@export var max_speed: float = 20.0
@export var world_size: Vector2 = Vector2(50, 50)
@export var mesh_scale: float = 1.0  # New scaling parameter
@export var debug_draw: bool

# Properties
var velocity: Vector3 = Vector3.ZERO


func _ready():
	# Initial velocity (full random direction)
	velocity = Vector3(randf_range(-1, 1), 0, randf_range(-1, 1)).normalized() * max_speed * 0.5
	position.y = 0.1  # Slight elevation to avoid Z-fighting




func wrap_position():
	var half_size = world_size / 2
	position.x = wrap(position.x, -half_size.x, half_size.x)
	position.z = wrap(position.z, -half_size.y, half_size.y)


static func wrap(value: float, min_val: float, max_val: float) -> float:
	return fposmod(value - min_val, max_val - min_val) + min_val

