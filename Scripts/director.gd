extends Node3D
var current_round : int = 0
var current_points : int
@export var round_at_points_relationship : Curve
var end_round_ui : Control
var pathfind_interval : float = .25
var pathfind_timer = Timer.new()
var process_enemy_pathfinding : bool = false
@export var enemies : Array[EnemyData]
var enemies_in_round : Array[Enemy]
var fake_enemies_in_round : Array[FakeEnemy]
var enemies_to_pathfind_per_frame 
var current_enemy_pathfind_index = 0
enum round_state {SPAWNING,BEGIN,FIGHT,END}
var current_round_state : round_state = round_state.SPAWNING
@export var spawn_points : Array[SpawnPoint]

func _ready():
    end_round_ui = Globals.ui.end_round
    add_child(pathfind_timer)
    pathfind_timer.wait_time = pathfind_interval
    pathfind_timer.timeout.connect(func(): process_enemy_pathfinding = true)
    current_round_state = round_state.SPAWNING

func _process(_delta):
    if( current_round_state == round_state.SPAWNING):
        current_points = round_at_points_relationship.sample(current_round*.001)
        add_enemies_to_round_list()
        current_round_state = round_state.BEGIN
    elif( current_round_state == round_state.BEGIN):
        enemies_to_pathfind_per_frame = enemies_in_round.size()/10.0
        current_round_state = round_state.FIGHT
    elif( current_round_state == round_state.FIGHT):
        if(pathfind_timer.is_stopped()):
            pathfind_timer.start()
    elif( current_round_state == round_state.END):
        current_round += 1
        current_round_state = round_state.SPAWNING
        # end_round_ui.show()

func _physics_process(_delta):
    if (process_enemy_pathfinding):
        frame_split_pathfinding()

func add_enemies_to_round_list():
    for index in range(0,enemies.size()):
        while(enemies[index].points <= current_points):
            var spawn_point_index = randi_range(0,spawn_points.size()-1)
            var new_enemy
            if spawn_points[spawn_point_index].count == 0:
                new_enemy = enemies[index].enemy_scene.instantiate()
                new_enemy.destroy_signal.connect(destroy_enemy)
                enemies_in_round.append(new_enemy)
                spawn_points[spawn_point_index].spawn_enemies_on_point(new_enemy)
            else:
                new_enemy = enemies[index].fake_enemy_scene.instantiate()
                new_enemy.get_node("Area3D").destroy_signal.connect(destroy_enemy)
                fake_enemies_in_round.append(new_enemy)
                spawn_points[spawn_point_index].spawn_enemies_on_point(new_enemy)
            current_points -= enemies[index].points

func destroy_enemy(enemy : Enemy):
    enemies_in_round.erase(enemy)
    if enemies_in_round.size() == 0 and current_points <= 0:
        current_round_state = round_state.END

    enemy.queue_free()

func frame_split_pathfinding():
    var processed_this_frame : float = 0
    while processed_this_frame <= enemies_to_pathfind_per_frame and current_enemy_pathfind_index < enemies_in_round.size():
        enemies_in_round[current_enemy_pathfind_index].update_pathfinding()
        processed_this_frame += 1
        current_enemy_pathfind_index += 1
        if current_enemy_pathfind_index >= enemies_in_round.size():
            current_enemy_pathfind_index = 0
            process_enemy_pathfinding = false
