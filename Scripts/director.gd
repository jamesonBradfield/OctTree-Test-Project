extends Node3D
@export var spawnable_enemies_in_round : Array[PackedScene]
@export var enemy_point_array : Array[int]
var current_round : int = 0
var current_points : int
@export var round_at_points_relationship : Curve
@onready var end_round_ui = $EndRoundUI
var pathfind_interval : float = .25
var pathfind_timer = Timer.new()
var process_enemy_pathfinding : bool = false
var enemies_in_round : Array[Enemy]
var enemies_to_pathfind_per_frame
var current_enemy_pathfind_index = 0
var pool : Array[Enemy]
enum round_state {SPAWNING,BEGIN,FIGHT,END}
var current_round_state : round_state = round_state.SPAWNING

func add_to_pool(node : Enemy):
    print("enemy ", node, " added to pool")
    node.hide()
    enemies_in_round.erase(node)
    if enemies_in_round.size() == 0:
        current_round_state = round_state.END
    self.add_child(node)
    pool.append(node)

func can_grab_from_pool(enemy : Enemy) -> bool:
    print("trying to find ", enemy, " in pool")
    if(pool.find(enemy)):
        print("success")
        return true
    else:
        print("fail")
        return false

func grab_from_pool(enemy : Enemy) -> Enemy:
    print("grabbing ", enemy, " from pool")
    var index = pool.find(enemy)
    var node : Enemy = pool[index]
    pool.remove_at(index)
    self.remove_child(node)
    node.show()
    node.instantiate_from_pool()
    return node
    
func _ready():
    add_child(pathfind_timer)
    pathfind_timer.wait_time = pathfind_interval
    pathfind_timer.timeout.connect(func(): process_enemy_pathfinding = true)
    current_round_state = round_state.SPAWNING


func _process(delta):
    if( current_round_state == round_state.SPAWNING):
        # print("Director is currently in state ", str(current_round_state))
        spawn_enemies_in_round_with_points()
    elif( current_round_state == round_state.BEGIN):
        # print("Director is currently in state ", str(current_round_state))
        enemies_to_pathfind_per_frame = enemies_in_round.size()/10.0
        current_round_state = round_state.FIGHT
    elif( current_round_state == round_state.FIGHT):
        # print("Director is currently in state ", str(current_round_state))
        if(pathfind_timer.is_stopped()):
            pathfind_timer.start()
    elif( current_round_state == round_state.END):
        # print("Director is currently in state ", str(current_round_state))
        end_round_ui.show()



func _physics_process(_delta):
    if (process_enemy_pathfinding):
        frame_split_pathfinding()

func round_started():
    pass

func spawn_enemies_in_round_with_points():
    current_points = round_at_points_relationship.sample(current_round)
    for index in range(0,spawnable_enemies_in_round.size()):
        while(enemy_point_array[index] <= current_points):
            # if(can_grab_from_pool(spawnable_enemies_in_round[index])):
            #     new_enemy = grab_from_pool(spawnable_enemies_in_round[index])
            # else:
            var new_enemy : Enemy = spawnable_enemies_in_round[index].instantiate() 
            new_enemy.position = Vector3(randf_range(-10,10),1.5,randf_range(-10,10))
            enemies_in_round.append(new_enemy)
            add_child(new_enemy)
            new_enemy.destroy_signal.connect(add_to_pool)
            current_points -= enemy_point_array[index] 
    current_round_state = round_state.BEGIN


func frame_split_pathfinding():
    var processed_this_frame : float = 0
    while processed_this_frame <= enemies_to_pathfind_per_frame and current_enemy_pathfind_index < enemies_in_round.size():
        enemies_in_round[current_enemy_pathfind_index].update_pathfinding()
        processed_this_frame += 1
        current_enemy_pathfind_index += 1
        if current_enemy_pathfind_index >= enemies_in_round.size():
            current_enemy_pathfind_index = 0
            process_enemy_pathfinding = false

