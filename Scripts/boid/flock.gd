extends Node3D
var flock: Array[Boid] = []
var num_boids: int = 400
var world_size: Vector2 = Vector2(50, 50)


func _ready():
	for b in range(0, num_boids):
		var boid = Boid.new(world_size)
		boid.position = Vector3(
			randf_range(-world_size.x / 2 + position.x, world_size.x / 2 + position.x),
			0,
			randf_range(-world_size.y / 2 + position.y, world_size.y / 2 + position.y)
		)
		flock.append(boid)
		add_child(boid)


func _process(delta):
	for boid in flock:
		boid.wrap_position()
		boid.flock(flock)
		boid.update(delta)
		boid.debug()
