class_name ParticleQueue
extends Node3D

@export var particle : PackedScene
@export var queue_count : int

var index = 0

func _ready():
    for _i in range(queue_count):
        add_child(particle.instantiate())


func get_next_particle():
    return get_child(index)

func trigger():
    get_next_particle().restart()
    index = (index + 1) % queue_count
