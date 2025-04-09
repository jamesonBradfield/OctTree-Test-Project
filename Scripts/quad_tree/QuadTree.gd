class_name QuadTree
const MAX_TOTAL_NODES = 1000
const MAX_DEPTH = 8
var boundary: Rectangle
var capacity: int
var boids: Array[Boid] = []
var divided: bool = false
var northwest: QuadTree = null
var northeast: QuadTree = null
var southwest: QuadTree = null
var southeast: QuadTree = null
var current_depth: int = 0


func initialize(bound: Rectangle, n: int):
	boundary = bound
	capacity = n
	current_depth = 0  # Reset depth for new trees
	boids.clear()


func subdivide():
	if current_depth >= MAX_DEPTH or get_total_nodes() >= MAX_TOTAL_NODES:
		return

	var x = boundary.x
	var y = boundary.y
	var w = boundary.w / 2
	var h = boundary.h / 2

	# Initialize all four quadrants
	northeast = QuadTree.new()
	northeast.initialize(Rectangle.new(x + w / 2, y + h / 2, w, h), capacity)
	northeast.current_depth = current_depth + 1

	northwest = QuadTree.new()
	northwest.initialize(Rectangle.new(x - w / 2, y + h / 2, w, h), capacity)
	northwest.current_depth = current_depth + 1

	southeast = QuadTree.new()
	southeast.initialize(Rectangle.new(x + w / 2, y - h / 2, w, h), capacity)
	southeast.current_depth = current_depth + 1

	southwest = QuadTree.new()
	southwest.initialize(Rectangle.new(x - w / 2, y - h / 2, w, h), capacity)
	southwest.current_depth = current_depth + 1

	divided = true


func get_total_nodes() -> int:
	var count = 1  # Count this node
	if divided:
		count += northwest.get_total_nodes() if northwest else 0
		count += northeast.get_total_nodes() if northeast else 0
		count += southwest.get_total_nodes() if southwest else 0
		count += southeast.get_total_nodes() if southeast else 0
	return count


func try_insert_here(boid: Boid) -> bool:
	if not boid in boids:
		boids.append(boid)
		return true
	return false


func try_insert_in_children(boid: Boid) -> bool:
	var center_x = boundary.x
	var center_y = boundary.y
	var success = false

	if boid.position.x > center_x:
		if boid.position.z > center_y:
			success = (
				northeast.insert(boid)
				or northwest.insert(boid)
				or southeast.insert(boid)
				or southwest.insert(boid)
			)
		else:
			success = (
				southeast.insert(boid)
				or southwest.insert(boid)
				or northeast.insert(boid)
				or northwest.insert(boid)
			)
	else:
		if boid.position.z > center_y:
			success = (
				northwest.insert(boid)
				or northeast.insert(boid)
				or southwest.insert(boid)
				or southeast.insert(boid)
			)
		else:
			success = (
				southwest.insert(boid)
				or southeast.insert(boid)
				or northwest.insert(boid)
				or northeast.insert(boid)
			)

	return success


func insert(boid: Boid) -> bool:
	if not is_instance_valid(boid) or not boundary.contains(boid.position):
		return false

	if boids.size() < capacity or current_depth >= MAX_DEPTH:
		return try_insert_here(boid)

	if get_total_nodes() >= MAX_TOTAL_NODES:
		return try_insert_here(boid)

	if not divided and current_depth < MAX_DEPTH:
		subdivide()

	if divided and try_insert_in_children(boid):
		return true

	return try_insert_here(boid)


func query(query_range: Rectangle) -> Array[Boid]:
	var local_results: Array[Boid] = []
	if !boundary.fast_intersects(query_range):
		return local_results

	for boid in boids:
		if is_instance_valid(boid) and query_range.fast_contains(boid.position):
			local_results.append(boid)

	if divided:
		for child in [northeast, northwest, southeast, southwest]:
			if child and child.boundary.fast_intersects(query_range):
				_append_child_results(child, query_range, local_results)
	return local_results


func _append_child_results(child: QuadTree, query_range: Rectangle, results: Array[Boid]):
	var stack = [child]
	while not stack.is_empty():
		var current = stack.pop_back()
		var is_fully_contained = query_range.fast_contains_rect(current.boundary)
		for boid in current.boids:
			if (
				is_instance_valid(boid)
				and (is_fully_contained or query_range.fast_contains(boid.position))
			):
				results.append(boid)
		if current.divided:
			for subchild in [
				current.northeast, current.northwest, current.southeast, current.southwest
			]:
				if subchild and subchild.boundary.fast_intersects(query_range):
					stack.append(subchild)


func cleanup_boids() -> void:
	var i = boids.size() - 1
	while i >= 0:
		if not is_instance_valid(boids[i]):
			boids.remove_at(i)
		i -= 1

	if divided:
		if is_instance_valid(northeast):
			northeast.cleanup_boids()
		if is_instance_valid(northwest):
			northwest.cleanup_boids()
		if is_instance_valid(southeast):
			southeast.cleanup_boids()
		if is_instance_valid(southwest):
			southwest.cleanup_boids()
