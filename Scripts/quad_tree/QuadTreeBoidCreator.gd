extends Node3D
# QUADTREE_UPDATE_INTERVAL increased to reduce frequency
const QUADTREE_UPDATE_INTERVAL: float = 0.2  # Reduced from 0.1
const UPDATE_INTERVAL: float = 0.016
const QUADTREE_UPDATE_INTERVAL: float = 0.1
@export var world_size: Vector2 = Vector2(50, 50)
@export var leader: Node3D
@export var boid_scene: PackedScene
@export var num_boids: int = 200
var max_search_range: float = 12.0

var flock: Array[Boid] = []
var quad_tree: ThreadSafeQuadTree
var quad_tree_boundary: Rectangle
var _thread_results := []
var _batch_args := []


var mutex: Mutex
var threads: Array[Thread] = []
var thread_count: int = 4

var boid_data_cache: Array[ThreadSafeBoidData] = []
var id_to_boid = {}
var next_boid_id: int = 0

var _last_quadtree_update: float = 0.0
var _last_update: float = 0.0
var _cached_nearby_boids = {}
var _cached_results = []


func _ready():
	if not boid_scene or not leader:
		push_error("Required nodes not set!")
		return

	_initialize_systems()
	_initialize_boids()


func _initialize_systems():
	mutex = Mutex.new()
	threads.resize(thread_count)
	for i in range(thread_count):
		threads[i] = Thread.new()

	quad_tree_boundary = Rectangle.new(0, 0, world_size.x, world_size.y)
	quad_tree = ThreadSafeQuadTree.new()
	quad_tree.initialize(quad_tree_boundary, 4)

	_cached_results.resize(thread_count)


func _initialize_boids():
	# var colors = [Color.BLUE, Color.RED, Color.GREEN, Color.YELLOW, Color.PURPLE]
	var half_size = world_size / 2

	for i in range(num_boids):
		var boid = boid_scene.instantiate() as Boid
		if not boid:
			push_error("Failed to instantiate boid!")
			continue

		boid.position = Vector3(
			randf_range(-half_size.x, half_size.x), 0, randf_range(-half_size.y, half_size.y)
		)

		boid.debug_draw = i < 5
		# boid.set_color(colors[i % colors.size()])

		add_child(boid)
		flock.append(boid)

		var boid_data = ThreadSafeBoidData.from_boid(boid, next_boid_id)
		boid_data_cache.append(boid_data)
		id_to_boid[next_boid_id] = boid
		next_boid_id += 1

		quad_tree.insert(boid_data)

	print("Initialized ", flock.size(), " boids")


func _find_nearby_boid_data(boid_data: ThreadSafeBoidData) -> Array[ThreadSafeBoidData]:
	var search_rect = Rectangle.new(
		boid_data.position.x, boid_data.position.z, max_search_range * 2, max_search_range * 2  # Pass Z as Rectangle's Y-axis
	)
	# Debug print to verify search area
	print(
		"Search area for boid ",
		boid_data.id,
		": ",
		search_rect.x,
		",",
		search_rect.y,
		" size(",
		search_rect.w,
		",",
		search_rect.h,
		")"
	)
	return quad_tree.query_area(search_rect)

	var nearby_data = quad_tree.query_area(search_rect)
	_cached_nearby_boids[boid_data.id] = nearby_data
	return nearby_data


func _update_quad_tree():
	quad_tree.cleanup()
	quad_tree = ThreadSafeQuadTree.new()
	quad_tree.initialize(quad_tree_boundary, 4)

	for boid_data in boid_data_cache:
		var success = quad_tree.insert(boid_data)
		if not success:
			push_error("Boid ", boid_data.id, " at ", boid_data.position, " failed insertion!")



func _process(delta: float):
	if !is_instance_valid(leader):
		return  # Fail silently in release

	# Coarse early exit for performance
	if (_last_update += delta) < UPDATE_INTERVAL:
		return
	_last_update = 0.0

	# Less frequent quadtree updates
	if (_last_quadtree_update += delta) >= QUADTREE_UPDATE_INTERVAL:
		_update_quad_tree()
		_last_quadtree_update = 0.0
		_cached_nearby_boids.clear()

	# Direct data sync without per-element checks
	_update_thread_safe_data()
	_process_boids_multithreaded()

func _update_thread_safe_data():
	# Batch update using parallel arrays
	var count = boid_data_cache.size()
	for i in count:
		var boid = id_to_boid.get(boid_data_cache[i].id, null)
		if boid:
			boid_data_cache[i].position = boid.position
			boid_data_cache[i].velocity = boid.velocity

func _process_boids_multithreaded():
	# Pre-calculate shared values
	var leader_pos = leader.position
	var total_boids = boid_data_cache.size()
	var boids_per_thread = (total_boids + thread_count - 1) / thread_count

	# Reuse thread arguments
	_batch_args.resize(thread_count)
	for i in thread_count:
		var start = i * boids_per_thread
		_batch_args[i] = [start, min(start + boids_per_thread, total_boids), leader_pos]

	# Parallel processing with job stealing
	for i in thread_count:
		if threads[i].is_started():
			threads[i].wait_to_finish()
		threads[i].start(_process_batch_wrapper.bind(i))

	# Collect results
	_thread_results.clear()
	for i in thread_count:
		threads[i].wait_to_finish()
		_thread_results.append(_cached_results[i])
	
	_apply_thread_results()

func _process_batch_wrapper(thread_idx: int):
	var args = _batch_args[thread_idx]
	var batch_updates = []
	
	# Hot loop optimization
	var start_idx = args[0]
	var end_idx = args[1]
	var leader_pos = args[2]
	var delta = UPDATE_INTERVAL
	
	for i in range(start_idx, end_idx):
		var boid_data = boid_data_cache[i]
		var nearby = quad_tree.query_area(Rectangle.new(
			boid_data.position.x, 
			boid_data.position.z, 
			max_search_range * 2, 
			max_search_range * 2
		))
		
		var new_vel = ThreadSafeBoidData.compute_flocking_optimized(boid_data, nearby, leader_pos)
		var new_pos = boid_data.position + new_vel * delta
		
		# Fast position wrapping
		new_pos.x = wrap(new_pos.x, -25.0, 25.0)  # Precomputed half_size
		new_pos.z = wrap(new_pos.z, -25.0, 25.0)
		
		batch_updates.append({
			"id": boid_data.id,
			"vel": new_vel,
			"pos": new_pos
		})

	# Single mutex operation per thread
	mutex.lock()
	_cached_results[thread_idx] = batch_updates
	mutex.unlock()

static func wrap(val: float, minv: float, maxv: float) -> float:
	var range = maxv - minv
	return fmod(val - minv + range, range) + minv

func _apply_thread_results():
	var half_size = world_size / 2

	for batch_result in _cached_results:
		if not batch_result:
			continue

		for update in batch_result:
			if not id_to_boid.has(update.id):
				continue

			var boid = id_to_boid[update.id]
			if not is_instance_valid(boid):
				continue

			# Update velocity and position
			boid.velocity = update.velocity
			var new_position = update.position

			# Wrap position
			if new_position.x > half_size.x:
				new_position.x = -half_size.x
			elif new_position.x < -half_size.x:
				new_position.x = half_size.x

			if new_position.z > half_size.y:
				new_position.z = -half_size.y
			elif new_position.z < -half_size.y:
				new_position.z = half_size.y

			boid.position = new_position


func _exit_tree():
	for thread in threads:
		if thread.is_started():
			thread.wait_to_finish()

	for boid in flock:
		if is_instance_valid(boid):
			boid.queue_free()

	flock.clear()
	boid_data_cache.clear()
	id_to_boid.clear()
