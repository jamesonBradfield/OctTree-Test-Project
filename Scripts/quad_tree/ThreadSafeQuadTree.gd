class_name ThreadSafeQuadTree
extends RefCounted

const MAX_TOTAL_NODES = 1000
const MAX_DEPTH = 6  # Was 8 (reduce to prevent over-subdivision)

var boundary: Rectangle
var capacity: int
var boid_data: Array[ThreadSafeBoidData] = []
var divided: bool = false
var northwest: ThreadSafeQuadTree = null
var northeast: ThreadSafeQuadTree = null
var southwest: ThreadSafeQuadTree = null
var southeast: ThreadSafeQuadTree = null
var current_depth: int = 0


func initialize(bound: Rectangle, n: int):
	boundary = bound
	capacity = n
	current_depth = 0
	boid_data.clear()

func get_all_boundaries() -> Array[Rectangle]:
	var boundaries: Array[Rectangle] = [boundary]
	if divided:
		boundaries += northeast.get_all_boundaries()
		boundaries += northwest.get_all_boundaries()
		boundaries += southeast.get_all_boundaries()
		boundaries += southwest.get_all_boundaries()
	return boundaries


func subdivide():
	if current_depth >= MAX_DEPTH or get_total_nodes() >= MAX_TOTAL_NODES:
		return

	var w = boundary.w * 0.5
	var h = boundary.h * 0.5
	var x = boundary.x
	var y = boundary.y
	northeast = ThreadSafeQuadTree.new()
	northeast.initialize(Rectangle.new(x + w * 0.5, y + h * 0.5, w, h), capacity)
	northeast.current_depth = current_depth + 1

	northwest = ThreadSafeQuadTree.new()
	northwest.initialize(Rectangle.new(x - w * 0.5, y + h * 0.5, w, h), capacity)
	northwest.current_depth = current_depth + 1

	southeast = ThreadSafeQuadTree.new()
	southeast.initialize(Rectangle.new(x + w * 0.5, y - h * 0.5, w, h), capacity)
	southeast.current_depth = current_depth + 1

	southwest = ThreadSafeQuadTree.new()
	southwest.initialize(Rectangle.new(x - w * 0.5, y - h * 0.5, w, h), capacity)
	southwest.current_depth = current_depth + 1

	divided = true


func insert(data: ThreadSafeBoidData) -> bool:
	if not boundary.contains_vector3(data.position):
		print(
			"Boid ",
			data.id,
			" at ",
			data.position,
			" outside boundary: X[",
			boundary.x - boundary.w / 2,
			",",
			boundary.x + boundary.w / 2,
			"] Z[",
			boundary.y - boundary.h / 2,
			",",
			boundary.y + boundary.h / 2,
			"]"
		)
		return false
	if data.position.x == 0 and data.position.z == 0:
		boid_data.append(data)
		return true
	# If there's space in current node, add here
	if boid_data.size() < capacity:
		if not data in boid_data:
			boid_data.append(data)
			return true
		return false

	# If we haven't subdivided and can, do so
	if not divided and current_depth < MAX_DEPTH:
		subdivide()

	# Try to insert into appropriate quadrant
	if divided:
		var inserted = false
		var x = boundary.x
		var y = boundary.y

		# Determine which quadrant the point belongs in
		if data.position.x >= x:  # Changed from ">" to ">="
			if data.position.z >= y:  # Changed from ">" to ">="
				inserted = northeast.insert(data)
			else:
				inserted = southeast.insert(data)
		else:
			if data.position.z >= y:  # Changed from ">" to ">="
				inserted = northwest.insert(data)
			else:
				inserted = southwest.insert(data)

		if inserted:
			return true

	# If we couldn't insert in children, store in current node
	if not data in boid_data:
		boid_data.append(data)
		return true

	return false


func query_area(search_area: Rectangle) -> Array[ThreadSafeBoidData]:
	var found: Array[ThreadSafeBoidData] = []

	# First check if search area intersects this node
	if not boundary.intersects(search_area):
		return found

	# Check points in current node
	for data in boid_data:
		if search_area.contains_vector3(data.position):
			found.append(data)

	# If subdivided, check children
	if divided:
		found.append_array(northeast.query_area(search_area))
		found.append_array(northwest.query_area(search_area))
		found.append_array(southeast.query_area(search_area))
		found.append_array(southwest.query_area(search_area))

	return found


func get_total_nodes() -> int:
	var count = 1
	if divided:
		count += northwest.get_total_nodes() if northwest else 0
		count += northeast.get_total_nodes() if northeast else 0
		count += southwest.get_total_nodes() if southwest else 0
		count += southeast.get_total_nodes() if southeast else 0
	return count


func cleanup():
	boid_data.clear()
	if divided:
		if northeast:
			northeast.cleanup()
		if northwest:
			northwest.cleanup()
		if southeast:
			southeast.cleanup()
		if southwest:
			southwest.cleanup()
		northeast = null
		northwest = null
		southeast = null
		southwest = null
		divided = false
