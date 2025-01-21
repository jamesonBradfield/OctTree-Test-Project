class_name Enemy
extends Damageable 

const SPEED = 4.0
const ATTACK_RANGE = 1.5
# NOTE: LOOK INTO GROUPS IF HAVING PERFORMANCE ISSUES WITH PATHFINDING!!!! (https://docs.godotengine.org/en/stable/tutorials/scripting/groups.html)
# TODO: create an enemy_manager that stores enemies in a group and handles them with one timer and calls one enemy to get the path to the player at a time
@onready var nav_agent = $NavigationAgent3D
@onready var attack_timer = $AttackTimer
@onready var health_bar = $SubViewport/Control/ProgressBar
@onready var collision_shape = $CollisionShape3D
@onready var player = $"../../Player"
enum state {IDLE,FOLLOW}
var current_state : state = state.FOLLOW
signal destroy_signal(enemy : Enemy)

var distance_to_player : float

func _ready():
    super()

func instantiate_from_pool():
    super()
    self.collision_shape.disabled = false

func _process(_delta):
    if(player and health == health_state.ALIVE):
        distance_to_player = global_position.distance_to(player.global_position)

        if distance_to_player > ATTACK_RANGE: 
            move_to_target()
        # attacking code
        else:
            velocity = Vector3.ZERO
            attack()
        # move enemy
        move_and_slide()

func move_to_target():
    var next_nav_point = nav_agent.get_next_path_position()
    velocity = (next_nav_point - global_transform.origin).normalized() * SPEED

func attack():
    if self.attack_timer.is_stopped():
        player.take_damage(10)
        self.attack_timer.start()

func update_pathfinding():
    if not player:
        return
    nav_agent.set_target_position(player.global_position)
    look_at(Vector3(player.global_position.x,global_position.y,player.global_position.z),Vector3.UP)

func take_damage(damage : int):
    super(damage)
    health_bar.value = current_health

func destroy():
    super()
    collision_shape.disabled = true
    destroy_signal.emit(self)
