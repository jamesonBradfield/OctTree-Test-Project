extends Node3D

@export var boid_creator_node: NodePath
var debug_material = StandardMaterial3D.new()
@onready var quadtree: ThreadSafeQuadTree = get_node(boid_creator_node).quad_tree


func _ready():
	debug_material.shading_mode = StandardMaterial3D.SHADING_MODE_UNSHADED
	debug_material.albedo_color = Color(1, 0, 0, 0.5)  # Red wireframe


func _process(_delta):
	_draw_quadtree()


func _draw_quadtree():
	if !quadtree:
		return

	DebugDraw3D.clear_all()

	var boundaries = quadtree.get_all_boundaries()
	for rect in boundaries:
		var center = Vector3(rect.x, 0, rect.y)
		var size = Vector2(rect.w, rect.h)
		var half_size = size / 2

		var points = [
			Vector3(center.x - half_size.x, 0, center.z - half_size.y),
			Vector3(center.x + half_size.x, 0, center.z - half_size.y),
			Vector3(center.x + half_size.x, 0, center.z + half_size.y),
			Vector3(center.x - half_size.x, 0, center.z + half_size.y)
		]

		for i in 4:
			var from = points[i]
			var to = points[(i + 1) % 4]
			DebugDraw3D.draw_line(from, to, debug_material.albedo_color, 0.1)
