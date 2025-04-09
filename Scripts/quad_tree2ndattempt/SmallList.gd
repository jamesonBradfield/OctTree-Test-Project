class_name SmallList
extends RefCounted

const SMALL_BUFFER_SIZE = 128

var _small_buffer: Array[Variant]  # Stack-allocated buffer
var _heap_buffer: Array[Variant]  # Heap-allocated buffer when needed
var _size: int = 0
var _using_heap: bool = false


func _init():
	_small_buffer.resize(SMALL_BUFFER_SIZE)


func append(value: Variant) -> void:
	if _size < SMALL_BUFFER_SIZE:
		_small_buffer[_size] = value
	else:
		if not _using_heap:
			# First time exceeding small buffer - initialize heap buffer
			_using_heap = true
			_heap_buffer = _small_buffer.duplicate()
		_heap_buffer.append(value)
	_size += 1


func pop_back() -> Variant:
	if _size == 0:
		push_error("Cannot pop from empty SmallList")
		return null

	_size -= 1
	if _using_heap:
		if _size < SMALL_BUFFER_SIZE:
			# Switch back to small buffer
			var value = _heap_buffer.pop_back()
			_using_heap = false
			# Copy remaining elements back to small buffer
			for i in range(_size):
				_small_buffer[i] = _heap_buffer[i]
			_heap_buffer.clear()
			return value
		return _heap_buffer.pop_back()
	return _small_buffer[_size]


func clear() -> void:
	_size = 0
	if _using_heap:
		_heap_buffer.clear()
		_using_heap = false
	for i in range(SMALL_BUFFER_SIZE):
		_small_buffer[i] = null


func size() -> int:
	return _size


func is_empty() -> bool:
	return _size == 0


func get_buffer() -> Array[Variant]:
	return _heap_buffer if _using_heap else _small_buffer


# Array access methods
func get_value_at_index(index: int) -> Variant:
	if index < 0 or index >= _size:
		push_error("Index out of bounds")
		return null
	return _heap_buffer[index] if _using_heap else _small_buffer[index]


func set_value_at_index(index: int, value: Variant) -> void:
	if index < 0 or index >= _size:
		push_error("Index out of bounds")
		return
	if _using_heap:
		_heap_buffer[index] = value
	else:
		_small_buffer[index] = value


# Iterator support
var _iter_current: int = 0


func _iter_init(_arg) -> bool:
	_iter_current = 0
	return _iter_current < _size


func _iter_next(_arg) -> bool:
	_iter_current += 1
	return _iter_current < _size


func _iter_get(_arg) -> Variant:
	return get_value_at_index(_iter_current)
