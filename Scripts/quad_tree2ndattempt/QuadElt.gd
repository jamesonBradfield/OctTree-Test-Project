class_name QuadElt
extends RefCounted

# Stores the ID for the element (can be used to refer to external data)
var id: int
# Stores the rectangle for the element using integer coordinates
var x1: int
var y1: int
var x2: int
var y2: int


func _init(p_id: int = 0, p_x1: int = 0, p_y1: int = 0, p_x2: int = 0, p_y2: int = 0) -> void:
	id = p_id
	x1 = p_x1
	y1 = p_y1
	x2 = p_x2
	y2 = p_y2


# Helper function to set from floating point coordinates
static func from_float(p_id: int, fx1: float, fy1: float, fx2: float, fy2: float) -> QuadElt:
	# Convert to fixed-point integers (multiply by 1000 to preserve 3 decimal places)
	var scale := 1000
	return QuadElt.new(p_id, int(fx1 * scale), int(fy1 * scale), int(fx2 * scale), int(fy2 * scale))
