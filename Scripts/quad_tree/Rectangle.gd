class_name Rectangle
extends RefCounted

var x: float  # Center X
var y: float  # Center Y
var w: float  # Width
var h: float  # Height


func _init(_x: float, _y: float, _w: float, _h: float):
	x = _x
	y = _y
	w = _w
	h = _h


func contains_vector3(point: Vector3) -> bool:
	var half_w = w * 0.5
	var half_h = h * 0.5
	return (
		point.x >= (x - half_w)
		and point.x <= (x + half_w)
		and point.z >= (y - half_h)  # Use Z-axis for Y-coordinate of rectangle
		and point.z <= (y + half_h)
	)


func intersects(other: Rectangle) -> bool:
	var half_w = w * 0.5
	var half_h = h * 0.5
	var other_half_w = other.w * 0.5
	var other_half_h = other.h * 0.5

	return not (
		x + half_w < other.x - other_half_w
		or x - half_w > other.x + other_half_w
		or y + half_h < other.y - other_half_h
		or y - half_h > other.y + other_half_h
	)
