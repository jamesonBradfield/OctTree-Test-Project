extends Node3D

@export var move_speed: float = 10.0
@export var turning_speed: float = 5.0

var target_position: Vector3
var velocity: Vector3
var debug_draw: bool = true


func _ready():
	# Initialize target at current position
	target_position = position

	# Create debug mesh
	var mesh = MeshInstance3D.new()
	var sphere = SphereMesh.new()
	sphere.radius = 1.0
	sphere.height = 2.0
	mesh.mesh = sphere

	# Add material
	var material = StandardMaterial3D.new()
	material.albedo_color = Color.RED
	material.emission_enabled = true
	material.emission = Color.RED
	material.emission_energy_multiplier = 2.0
	mesh.material_override = material

	add_child(mesh)


func _process(delta):
	# Move towards target
	var direction = target_position - position

	if direction.length() > 0.1:
		direction = direction.normalized()
		velocity = velocity.lerp(direction * move_speed, turning_speed * delta)
		position += velocity * delta
	else:
		velocity = velocity.lerp(Vector3.ZERO, turning_speed * delta)

	if debug_draw:
		# Draw path to target
		DebugDraw3D.draw_line(position, target_position, Color.RED)
		DebugDraw3D.draw_sphere(target_position, 0.5, Color.YELLOW)
