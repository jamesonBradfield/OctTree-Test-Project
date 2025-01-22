class_name FakeEnemy
extends FakeDamageable 

const SPEED = 4.0
const ATTACK_RANGE = 1.5
@onready var attack_timer = $AttackTimer
@onready var collision_shape = $CollisionShape3D
signal destroy_signal(enemy : Enemy)

var distance_to_player : float
@onready var boid: Boid3D = get_parent()

func _ready():
    super()

func _process(_delta):
    if(Globals.player and health == health_state.ALIVE):
        distance_to_player = global_position.distance_to(Globals.player.global_position)
        if distance_to_player < ATTACK_RANGE:
            attack()

func attack():
    if self.attack_timer.is_stopped():
        Globals.player.take_damage(10)
        self.attack_timer.start()

func update_pathfinding():
    look_at(Vector3(Globals.player.global_position.x,global_position.y,Globals.player.global_position.z),Vector3.UP)

func take_damage(damage : int):
    super(damage)

func destroy():
    super()
    collision_shape.disabled = true
    destroy_signal.emit(self)

