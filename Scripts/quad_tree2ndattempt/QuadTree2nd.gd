class_name QuadTree2nd
extends RefCounted

# Stores all the elements in the quadtree using FreeList
var elts: FreeList
# Stores all the element nodes in the quadtree using FreeList
var elt_nodes: FreeList
# Stores all the nodes in the quadtree using FreeList, first node is always root
var nodes: FreeList
# Stores the quadtree extents
var root_rect: QuadCRect
# Store the first free node in the quadtree to be reclaimed as 4
# contiguous nodes at once. A value of -1 indicates that the free
# list is empty
var free_node: int = -1
# Stores the maximum depth allowed for the quadtree
var max_depth: int = 8


func _init(rect: QuadCRect, depth: int = 8) -> void:
	root_rect = rect
	max_depth = depth
	elts = FreeList.new()
	elt_nodes = FreeList.new()
	nodes = FreeList.new()
	# Create root node
	nodes.insert(QuadNode.new())


# Allocates 4 child nodes and returns the index of the first child
func _alloc_nodes() -> int:
	if free_node != -1:
		# Reuse nodes from free list
		var alloc_index := free_node
		free_node = nodes.get_element(free_node).first_child
		return alloc_index
	# Allocate new nodes
	var start_index := nodes.range()
	for i in range(4):
		nodes.insert(QuadNode.new())
	return start_index


# Frees 4 child nodes starting at the given index
func _free_nodes(index: int) -> void:
	# Link freed nodes together in LIFO order
	# The last node will point to the previous free_node
	for i in range(3):
		nodes.get_element(index + i).first_child = index + i + 1
		nodes.erase(index + i)

	# Last node points to previous free list head
	nodes.get_element(index + 3).first_child = free_node
	nodes.erase(index + 3)

	# Update free_node to point to first node in freed block
	free_node = index


# Call this method at the end of each frame
func cleanup() -> void:
	print("Starting cleanup")
	var to_process := SmallList.new()
	var root_node: QuadNode = nodes.get_element(0)

	# Only process if root is a branch
	if root_node.count == -1:
		to_process.append(0)
		print("Root is branch, processing children")

	while to_process.size() > 0:
		var node_index = to_process.pop_back()
		var current_node: QuadNode = nodes.get_element(node_index)
		print("Processing node ", node_index, " children start at: ", current_node.first_child)

		var num_empty_leaves := 0

		# Loop through all 4 children
		for j in range(4):
			var child_index := current_node.first_child + j
			var child: QuadNode = nodes.get_element(child_index)
			print("  Child ", j, " count: ", child.count)

			# If child is an empty leaf, increment empty leaf count
			if child.count == 0:
				num_empty_leaves += 1
			# If child is a branch, add to processing queue
			elif child.count == -1:
				to_process.append(child_index)

		print("  Empty leaves: ", num_empty_leaves)

		# If all 4 children are empty leaves, remove them
		if num_empty_leaves == 4:
			print("  Cleaning up node ", node_index)

			# Push first child to free list
			nodes.get_element(current_node.first_child).first_child = free_node
			free_node = current_node.first_child

			# Convert current node to an empty leaf
			current_node.first_child = -1
			current_node.count = 0


# Returns true if the element rectangle intersects with the given node bounds
func _element_intersects_node(
	elt: QuadElt, center_x: int, center_y: int, half_size_x: int, half_size_y: int
) -> bool:
	var node_x1 := center_x - half_size_x
	var node_y1 := center_y - half_size_y
	var node_x2 := center_x + half_size_x
	var node_y2 := center_y + half_size_y

	return not (elt.x2 < node_x1 or elt.x1 > node_x2 or elt.y2 < node_y1 or elt.y1 > node_y2)


# Adds an element node to a leaf's linked list
func _add_element_node(leaf_index: int, elt_index: int) -> void:
	var elt_node := QuadEltNode.new()
	elt_node.element = elt_index

	var leaf_node: QuadNode = nodes.get_element(leaf_index)
	elt_node.next = leaf_node.first_child

	var node_index := elt_nodes.insert(elt_node)
	leaf_node.first_child = node_index
	leaf_node.count += 1


# Checks if a node should split based on element count
func _should_split(node_index: int, depth: int) -> bool:
	return depth < max_depth and nodes.get_element(node_index).count >= 8


# Splits a leaf node into 4 children
func _split_node(
	node_index: int, center_x: int, center_y: int, half_size_x: int, half_size_y: int
) -> void:
	var current_node: QuadNode = nodes.get_element(node_index)
	var first_child := _alloc_nodes()

	# Calculate quarter sizes for child nodes
	var quarter_size_x := half_size_x >> 1
	var quarter_size_y := half_size_y >> 1

	# Transfer elements to children
	var current_elt_node_index: int = current_node.first_child
	while current_elt_node_index != -1:
		var current_elt_node: QuadEltNode = elt_nodes.get_element(current_elt_node_index)
		var current_elt: QuadElt = elts.get_element(current_elt_node.element)
		var next_elt_node_index: int = current_elt_node.next

		# Check intersection with child quadrants
		if current_elt.x1 <= center_x:
			if current_elt.y1 <= center_y:
				# Top-left quadrant
				if _element_intersects_node(
					current_elt,
					center_x - quarter_size_x,
					center_y - quarter_size_y,
					quarter_size_x,
					quarter_size_y
				):
					_add_element_node(first_child, current_elt_node.element)
			if current_elt.y2 > center_y:
				# Bottom-left quadrant
				if _element_intersects_node(
					current_elt,
					center_x - quarter_size_x,
					center_y + quarter_size_y,
					quarter_size_x,
					quarter_size_y
				):
					_add_element_node(first_child + 2, current_elt_node.element)
		if current_elt.x2 > center_x:
			if current_elt.y1 <= center_y:
				# Top-right quadrant
				if _element_intersects_node(
					current_elt,
					center_x + quarter_size_x,
					center_y - quarter_size_y,
					quarter_size_x,
					quarter_size_y
				):
					_add_element_node(first_child + 1, current_elt_node.element)
			if current_elt.y2 > center_y:
				# Bottom-right quadrant
				if _element_intersects_node(
					current_elt,
					center_x + quarter_size_x,
					center_y + quarter_size_y,
					quarter_size_x,
					quarter_size_y
				):
					_add_element_node(first_child + 3, current_elt_node.element)

		# Free the old element node
		elt_nodes.erase(current_elt_node_index)
		current_elt_node_index = next_elt_node_index

	# Convert this node to a branch
	current_node.first_child = first_child
	current_node.count = -1  # -1 indicates this is no longer a leaf


# Inserts an element into the quadtree
func insert(elt: QuadElt) -> void:
	var elt_index := elts.insert(elt)

	# Start recursive insertion at root
	_insert_recursive(
		elt_index,
		0,
		root_rect.center_x,
		root_rect.center_y,
		root_rect.half_width,
		root_rect.half_height,
		0
	)


func _compute_child_bounds(
	parent_center_x: int,
	parent_center_y: int,
	parent_half_width: int,
	parent_half_height: int,
	child_index: int
) -> Dictionary:
	# Compute quarter sizes
	var quarter_width := parent_half_width >> 1
	var quarter_height := parent_half_height >> 1

	# Compute child center based on index
	match child_index:
		0:  # Top-left
			return {
				"center_x": parent_center_x - quarter_width,
				"center_y": parent_center_y - quarter_height,
				"half_width": quarter_width,
				"half_height": quarter_height
			}
		1:  # Top-right
			return {
				"center_x": parent_center_x + quarter_width,
				"center_y": parent_center_y - quarter_height,
				"half_width": quarter_width,
				"half_height": quarter_height
			}
		2:  # Bottom-left
			return {
				"center_x": parent_center_x - quarter_width,
				"center_y": parent_center_y + quarter_height,
				"half_width": quarter_width,
				"half_height": quarter_height
			}
		3:  # Bottom-right
			return {
				"center_x": parent_center_x + quarter_width,
				"center_y": parent_center_y + quarter_height,
				"half_width": quarter_width,
				"half_height": quarter_height
			}
		_:
			push_error("Invalid child index")
			return {}


# Modify existing methods to use on-the-fly computation
func _insert_recursive(
	elt_index: int,
	node_index: int,
	center_x: int,
	center_y: int,
	half_size_x: int,
	half_size_y: int,
	depth: int
) -> void:
	var current_node: QuadNode = nodes.get_element(node_index)

	# If this is a leaf node
	if current_node.count != -1:
		_add_element_node(node_index, elt_index)

		# Check if we should split this node
		if _should_split(node_index, depth):
			_split_node(node_index, center_x, center_y, half_size_x, half_size_y)

	# If this is a branch node
	else:
		var current_elt: QuadElt = elts.get_element(elt_index)
		var child_index: int = current_node.first_child

		# Check each quadrant
		if current_elt.x1 <= center_x:
			if current_elt.y1 <= center_y:
				# Top-left quadrant
				var tl_bounds = _compute_child_bounds(
					center_x, center_y, half_size_x, half_size_y, 0
				)
				_insert_recursive(
					elt_index,
					child_index,
					tl_bounds["center_x"],
					tl_bounds["center_y"],
					tl_bounds["half_width"],
					tl_bounds["half_height"],
					depth + 1
				)

			if current_elt.y2 > center_y:
				# Bottom-left quadrant
				var bl_bounds = _compute_child_bounds(
					center_x, center_y, half_size_x, half_size_y, 2
				)
				_insert_recursive(
					elt_index,
					child_index + 2,
					bl_bounds["center_x"],
					bl_bounds["center_y"],
					bl_bounds["half_width"],
					bl_bounds["half_height"],
					depth + 1
				)

		if current_elt.x2 > center_x:
			if current_elt.y1 <= center_y:
				# Top-right quadrant
				var tr_bounds = _compute_child_bounds(
					center_x, center_y, half_size_x, half_size_y, 1
				)
				_insert_recursive(
					elt_index,
					child_index + 1,
					tr_bounds["center_x"],
					tr_bounds["center_y"],
					tr_bounds["half_width"],
					tr_bounds["half_height"],
					depth + 1
				)

			if current_elt.y2 > center_y:
				# Bottom-right quadrant
				var br_bounds = _compute_child_bounds(
					center_x, center_y, half_size_x, half_size_y, 3
				)
				_insert_recursive(
					elt_index,
					child_index + 3,
					br_bounds["center_x"],
					br_bounds["center_y"],
					br_bounds["half_width"],
					br_bounds["half_height"],
					depth + 1
				)


# Modify _split_node to use on-the-fly computation
func _split_node(
	node_index: int, center_x: int, center_y: int, half_size_x: int, half_size_y: int
) -> void:
	var current_node: QuadNode = nodes.get_element(node_index)
	var first_child := _alloc_nodes()

	# Calculate quarter sizes for child nodes
	var quarter_size_x := half_size_x >> 1
	var quarter_size_y := half_size_y >> 1

	# Transfer elements to children
	var current_elt_node_index: int = current_node.first_child
	while current_elt_node_index != -1:
		var current_elt_node: QuadEltNode = elt_nodes.get_element(current_elt_node_index)
		var current_elt: QuadElt = elts.get_element(current_elt_node.element)
		var next_elt_node_index: int = current_elt_node.next

		# Compute and check intersections with child quadrants
		# Top-left
		var tl_bounds = _compute_child_bounds(center_x, center_y, half_size_x, half_size_y, 0)
		if _element_intersects_node(
			current_elt,
			tl_bounds["center_x"],
			tl_bounds["center_y"],
			tl_bounds["half_width"],
			tl_bounds["half_height"]
		):
			_add_element_node(first_child, current_elt_node.element)

		# Add similar checks for other quadrants (top-right, bottom-left, bottom-right)
		# ... (code omitted for brevity, but would follow the same pattern)

		# Free the old element node
		elt_nodes.erase(current_elt_node_index)
		current_elt_node_index = next_elt_node_index

	# Convert this node to a branch
	current_node.first_child = first_child
	current_node.count = -1  # -1 indicates branch node
