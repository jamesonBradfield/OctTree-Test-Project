class_name FreeList
extends RefCounted

# Since we can't use unions in GDScript, we'll use a dictionary to store either
# the element or next pointer
var _data: Array[Dictionary]
var _first_free: int


func _init() -> void:
	_data = []
	_first_free = -1


# Inserts an element to the free list and returns an index to it
func insert(element) -> int:
	if _first_free != -1:
		var index := _first_free
		_first_free = _data[_first_free]["next"]
		_data[index] = {"element": element}
		return index
	_data.append({"element": element})
	return _data.size() - 1


# Removes the nth element from the free list
func erase(n: int) -> void:
	_data[n] = {"next": _first_free}
	_first_free = n


# Removes all elements from the free list
func clear() -> void:
	_data.clear()
	_first_free = -1


# Returns the range of valid indices
func range() -> int:
	return _data.size()


# Returns the nth element
func get_element(n: int):
	return _data[n]["element"]


# Sets the nth element
func set_element(n: int, element) -> void:
	_data[n]["element"] = element


# Returns whether the given index is a free slot
func is_free(n: int) -> bool:
	return "next" in _data[n]


# Returns whether the given index is valid
func is_valid_index(n: int) -> bool:
	return n >= 0 and n < _data.size()
