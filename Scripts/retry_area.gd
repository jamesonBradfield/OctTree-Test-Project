extends Area3D

func Retry(value : Node3D) ->void:
	get_tree().reload_current_scene();
