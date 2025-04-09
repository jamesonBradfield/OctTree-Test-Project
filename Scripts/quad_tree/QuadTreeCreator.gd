extends Node
var world_size = Vector2(50, 50)
var points: Array = []
var search_range = Rectangle.new(
	randf_range(-world_size.x / 2, world_size.x / 2),
	randf_range(-world_size.y / 2, world_size.y / 2),
	world_size.x / 2,
	world_size.y / 2
)


func _ready():
	var quad_tree_size = world_size
	# Create boundary with the full width/height
	var quad_tree_boundary = Rectangle.new(0, 0, quad_tree_size.x, quad_tree_size.y)
	var quad_tree = QuadTree.new(quad_tree_boundary, 4)
	add_child(quad_tree)

	for i in range(1000):
		var p = Vector2(
			randf_range(-quad_tree_size.x / 2, quad_tree_size.x / 2),
			randf_range(-quad_tree_size.y / 2, quad_tree_size.y / 2)
		)
		quad_tree.insert(p)
	points = quad_tree.query(search_range)


func _process(_delta):
	DebugDraw3D.draw_box(
		Vector3(search_range.x, 0, search_range.y),
		Quaternion.IDENTITY,
		Vector3(search_range.w, 10, search_range.h),
		Color(0, 1, 0),
		true
	)
	if not points.is_empty():
		for p in points:
			DebugDraw3D.draw_sphere(Vector3(p.x, 0, p.y), .13, Color(0, 1, 0))
