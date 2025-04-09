class_name QuadNode
extends RefCounted

# Points to the first child if this node is a branch or the first element if this node is a leaf
var first_child: int
# Stores the number of elements in the leaf or -1 if it this node is not a leaf
var count: int

func _init(p_first_child: int = -1, p_count: int = 0):
	first_child = p_first_child
	count = p_count
