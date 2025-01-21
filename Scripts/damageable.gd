class_name Damageable
extends CharacterBody3D
var max_health : int = 100
var current_health : int 
enum health_state {ALIVE,DEAD}
var health : health_state = health_state.ALIVE

func _ready():
    instantiate_from_pool()

func instantiate_from_pool():
    if health != health_state.ALIVE:
        health = health_state.ALIVE
    current_health = max_health


func take_damage(damage : int):
    current_health -= damage
    if(current_health <= 0):
        destroy()

func heal(heal_value : int):
    if(current_health + heal_value <= max_health):
        current_health += heal_value
    else:
        current_health = max_health

func destroy():
    health = health_state.DEAD
