extends Node3D
var camera_speed = 20.0
var zoom_speed = 2.0

@onready var camera = $QuadTreeTest/Camera3D


func _process(delta):
	if Input.is_key_pressed(KEY_W):
		camera.position.z -= camera_speed * delta
	if Input.is_key_pressed(KEY_S):
		camera.position.z += camera_speed * delta
	if Input.is_key_pressed(KEY_A):
		camera.position.x -= camera_speed * delta
	if Input.is_key_pressed(KEY_D):
		camera.position.x += camera_speed * delta

	# Zoom with Q/E
	if Input.is_key_pressed(KEY_Q):
		camera.position.y -= zoom_speed * delta * camera.position.y
	if Input.is_key_pressed(KEY_E):
		camera.position.y += zoom_speed * delta * camera.position.y
