class_name QuadEltNode
extends RefCounted

# Points to the next element in the leaf node. A value of -1 indicates the end of the list
var next: int
# Stores the element index
var element: int

func _init(p_next: int = -1, p_element: int = 0):
	next = p_next
	element = p_element
