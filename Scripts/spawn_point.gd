class_name SpawnPoint
extends Node3D

@export var count : int = 0
var currentX : int = 0
var currentZ : int = 0
var maxX : int = 10
var maxZ : int = 10
var flock : Flock3D
var real_enemy : Enemy
func spawn_enemies_on_point(node) -> void:
    if node is Enemy:
        add_child(node)
        print("adding real enemy to ",self.name)
        flock = Flock3D.new()
        flock.target = node
        flock.properties = load("res://addons/boids/defaults/3d_flock_properties.tres")
        self.add_child(flock)
        real_enemy = node
        count = 1
    elif node is Boid3D:
        if flock:
            flock.add_child(node)
            node.global_position = Vector3(flock.global_position.x + currentX,flock.global_position.y + 1.5,flock.global_position.z + currentZ)
            count += 1

    if currentX < maxX:
        currentX += 1
    else:
        currentZ += 1
        currentX = 0

