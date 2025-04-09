class_name QuadTreePool
extends Node

var pool: WCSpawnPool
var active_trees: Array[QuadTree] = []
var pooled_trees: Dictionary = {}  # Track which trees came from pool


func _init():
	pool = WCSpawnPool.new()
	add_child(pool)
	pool.scene_file = "res://Scenes/QuadTree.tscn"
	pool.thres_in_tree = 16
	pool.thres_in_memory = 32


func get_quad_tree(bound: Rectangle, capacity: int) -> QuadTree:
	var tree = await pool.spawn()
	if not tree:
		push_error("Failed to spawn QuadTree")
		return null

	# Mark this tree as pool-spawned
	pooled_trees[tree] = true

	# Initialize first, then set properties
	tree.initialize(bound, capacity)
	tree.divided = false
	tree.boids.clear()
	active_trees.append(tree)
	return tree


func return_tree(tree: QuadTree):
	if not is_instance_valid(tree):
		return

	# First return all children recursively
	if tree.divided:
		if is_instance_valid(tree.northwest):
			await _safe_return_subtree(tree.northwest)
		if is_instance_valid(tree.northeast):
			await _safe_return_subtree(tree.northeast)
		if is_instance_valid(tree.southwest):
			await _safe_return_subtree(tree.southwest)
		if is_instance_valid(tree.southeast):
			await _safe_return_subtree(tree.southeast)

		tree.northwest = null
		tree.northeast = null
		tree.southwest = null
		tree.southeast = null
		tree.divided = false

	# Remove from active trees
	var idx = active_trees.find(tree)
	if idx != -1:
		active_trees.remove_at(idx)

	# Clear the tree's data
	tree.boids.clear()

	# Only despawn if it was created by the pool
	if pooled_trees.has(tree):
		pool.despawn(tree)
		pooled_trees.erase(tree)
	else:
		# If it wasn't from the pool, just free it
		if tree.get_parent():
			tree.get_parent().remove_child(tree)
		tree.queue_free()


func _safe_return_subtree(subtree: QuadTree) -> void:
	if not is_instance_valid(subtree):
		return

	if subtree.get_parent():
		subtree.get_parent().remove_child(subtree)
	await return_tree(subtree)


func subdivide_pooled(tree: QuadTree):
	if not is_instance_valid(tree):
		push_error("Invalid tree passed to subdivide_pooled")
		return

	var x = tree.boundary.x
	var y = tree.boundary.y
	var w = tree.boundary.w / 2
	var h = tree.boundary.h / 2

	# Create all quadrants
	tree.northeast = await get_quad_tree(Rectangle.new(x + w / 2, y + h / 2, w, h), tree.capacity)
	tree.northwest = await get_quad_tree(Rectangle.new(x - w / 2, y + h / 2, w, h), tree.capacity)
	tree.southeast = await get_quad_tree(Rectangle.new(x + w / 2, y - h / 2, w, h), tree.capacity)
	tree.southwest = await get_quad_tree(Rectangle.new(x - w / 2, y - h / 2, w, h), tree.capacity)

	# Only proceed if all quadrants were created successfully
	if not tree.northeast or not tree.northwest or not tree.southeast or not tree.southwest:
		push_error("Failed to create all quadrants")
		if tree.northeast:
			await return_tree(tree.northeast)
		if tree.northwest:
			await return_tree(tree.northwest)
		if tree.southeast:
			await return_tree(tree.southeast)
		if tree.southwest:
			await return_tree(tree.southwest)
		return

	tree.add_child(tree.northeast)
	tree.add_child(tree.northwest)
	tree.add_child(tree.southeast)
	tree.add_child(tree.southwest)

	tree.boids.clear()
	tree.divided = true


func cleanup():
	for tree in active_trees.duplicate():
		await return_tree(tree)
	active_trees.clear()
	pooled_trees.clear()
	pool.cleanup()
