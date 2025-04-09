class_name QuadCRect
extends RefCounted

# Center coordinates
var center_x: int
var center_y: int
# Half dimensions
var half_width: int
var half_height: int

func _init(p_cx: int = 0, p_cy: int = 0, p_hw: int = 0, p_hh: int = 0):
	center_x = p_cx
	center_y = p_cy
	half_width = p_hw
	half_height = p_hh
