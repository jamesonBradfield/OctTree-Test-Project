extends Node3D

@onready var terrain: Node3D = $Terrain
@onready var leader: Node3D = $Leader
@onready var camera: Camera3D = $Camera3D
@onready var boid_creator = $QuadTreeBoidCreator


func _ready():
	# Set up camera
	camera.position = Vector3(0, 50, 50)
	camera.rotation_degrees = Vector3(-45, 0, 0)

	# Set up terrain
	# _create_terrain()

	# Connect leader to boid creator
	boid_creator.leader = leader


func _create_terrain():
	# Create some elevated platforms
	var platforms = [
		{"pos": Vector3(10, 5, 10), "size": Vector3(10, 1, 10)},
		{"pos": Vector3(-15, 3, -15), "size": Vector3(8, 1, 8)},
		{"pos": Vector3(0, 7, -20), "size": Vector3(12, 1, 12)}
	]

	for plat in platforms:
		var platform = CSGBox3D.new()
		platform.position = plat.pos
		platform.size = plat.size

		# Add collision
		var static_body = StaticBody3D.new()
		var collision = CollisionShape3D.new()
		var box_shape = BoxShape3D.new()
		box_shape.size = plat.size
		collision.shape = box_shape
		static_body.add_child(collision)
		platform.add_child(static_body)

		terrain.add_child(platform)

	# Create base plane
	var ground = CSGBox3D.new()
	ground.position = Vector3(0, -0.5, 0)
	ground.size = Vector3(100, 1, 100)

	# Add collision to ground
	var ground_body = StaticBody3D.new()
	var ground_collision = CollisionShape3D.new()
	var ground_shape = BoxShape3D.new()
	ground_shape.size = Vector3(100, 1, 100)
	ground_collision.shape = ground_shape
	ground_body.add_child(ground_collision)
	ground.add_child(ground_body)

	terrain.add_child(ground)


func _process(_delta):
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		var mouse_pos = get_viewport().get_mouse_position()
		var from = camera.project_ray_origin(mouse_pos)
		var to = from + camera.project_ray_normal(mouse_pos) * 1000

		var space_state = get_world_3d().direct_space_state
		var query = PhysicsRayQueryParameters3D.create(from, to)
		var result = space_state.intersect_ray(query)

		if result:
			# Update leader target position
			leader.target_position = result.position
