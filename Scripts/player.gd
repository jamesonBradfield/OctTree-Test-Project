class_name Player
extends Damageable 
@onready var camera : Camera3D = $Head/Camera3D
var ui = {}
@onready var bhop_player : AudioStreamPlayer3D = $BhopPlayer3D
@export var look_sensitivity : float = 0.006
@export var controller_look_sensitivity : float = 0.05
@export var jump_velocity := 6.0
@export var auto_bhop := true
const HEADBOB_MOVE_AMOUNT = 0.06
const HEADBOB_FREQUENCY = 2.4
const IDLE_HEADBOB_MOVE_AMOUNT = 0.035
const IDLE_HEADBOB_FREQUENCY = 1.4
var headbob_time := 0.0

# air movement settings
@export var air_cap := 0.85
@export var air_accel := 800.0
@export var air_move_speed := 500.0

# ground movement settings
@export var walk_speed := 7.0
@export var sprint_speed := 8.5
@export var ground_accel := 14.0
@export var ground_decel := 10
@export var ground_friction := 6.0


var jumping := false
var jumping_started := false

var input_dir : Vector2
var wish_dir := Vector3.ZERO
var _cur_controller_look = Vector2()

func _ready():
    ui = Globals.ui
    super()
    ui.health.max_value = max_health
    ui.health.value = current_health

func _unhandled_input(event):
    if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
        if event is InputEventMouseMotion:
            rotate_y(-event.relative.x * look_sensitivity)
            camera.rotate_x(-event.relative.y * look_sensitivity)
            camera.rotation.x = clamp(camera.rotation.x, deg_to_rad(-90),deg_to_rad(90))

func get_move_speed() -> float:
    return sprint_speed if Input.is_action_pressed("sprint") else walk_speed

func _handle_controller_look_input(delta):
    var target_look = Input.get_vector("look_right","look_left","look_down", "look_up")
    if target_look.length() < _cur_controller_look.length():
        _cur_controller_look = target_look
    else:
        _cur_controller_look = _cur_controller_look.lerp(target_look,5.0*delta)

    rotate_y(_cur_controller_look.x * controller_look_sensitivity)
    camera.rotate_x(_cur_controller_look.y * controller_look_sensitivity)
    camera.rotation.x = clamp(camera.rotation.x, deg_to_rad(-90),deg_to_rad(90))

func _process(delta):
    if is_on_floor():
        if Input.is_action_just_pressed("jump") or (auto_bhop and Input.is_action_pressed("jump")):
            jumping_started = true
            jumping = true
    input_dir = Input.get_vector("left","right","up","down").normalized()
    _handle_controller_look_input(delta)

    # if is_on_floor() and jumping:
    #     bhop_player.pitch_scale = randf_range(.85,1.125)
    #     bhop_player.volume_db = randf_range(.85,1.125)
    #     bhop_player.play()

func _physics_process(delta):
    #depending on which way you have your character facing, you may have to negate the input directions
    wish_dir = self.global_transform.basis * Vector3(input_dir.x,0., input_dir.y)

    if jumping_started == true:
        self.velocity.y = jump_velocity
        jumping_started = false

    if is_on_floor():
        _handle_ground_physics(delta)
    else:
        _handle_air_physics(delta)


    ui.velocity.text = str(int(self.velocity.length()))

    move_and_slide()


func _handle_ground_physics(delta) -> void:
    var cur_speed_in_wish_dir = self.velocity.dot(wish_dir)
    var add_speed_till_cap = get_move_speed() - cur_speed_in_wish_dir
    if add_speed_till_cap > 0:
        var accel_speed = ground_accel * delta * get_move_speed()
        accel_speed = min(accel_speed, add_speed_till_cap)
        self.velocity += accel_speed * wish_dir

    var control = max(self.velocity.length(), ground_decel)
    var drop = control * ground_friction * delta
    var new_speed = max(self.velocity.length() - drop, 0.0)
    if self.velocity.length() > 0:
        new_speed /= self.velocity.length()
    self.velocity *= new_speed
    _headbob_effect(delta)

func _handle_air_physics(delta) -> void:
    self.velocity.y -= ProjectSettings.get_setting("physics/3d/default_gravity") * delta
    # Classic battle tested & fan favorite source/quake air movement recipe
    var cur_speed_in_wish_dir = self.velocity.dot(wish_dir)
    # Wish speed (if wish_dir > 0 length) capped to air_cap
    var capped_speed = min((air_move_speed * wish_dir).length(), air_cap)
    # How much to get to the speed the player wishes (in the new dir)
    # Notice this allows for infinite speed. If wish_dir is perpindicular, we always need to add velocity no matter how fast we're going. this is what allows for things like bhop in CSS & Quake. Also happens to just give some very nice feeling movement & responsiveness when in the air.
    var add_speed_till_cap = capped_speed - cur_speed_in_wish_dir
    if add_speed_till_cap > 0:
        var accel_speed = air_accel * air_move_speed * delta
        accel_speed = min(accel_speed, add_speed_till_cap)
        self.velocity += accel_speed * wish_dir

func _headbob_effect(delta):
    if velocity.length() > 0:
        headbob_time += delta * self.velocity.length()
        camera.transform.origin = Vector3(
            cos(headbob_time * HEADBOB_FREQUENCY * 0.5) * HEADBOB_MOVE_AMOUNT,
            sin(headbob_time * HEADBOB_FREQUENCY) * HEADBOB_MOVE_AMOUNT,
            0
        )
    else:
        headbob_time += delta
        camera.transform.origin = Vector3(
            cos(headbob_time * IDLE_HEADBOB_FREQUENCY * 0.5) * IDLE_HEADBOB_MOVE_AMOUNT,
            sin(headbob_time * IDLE_HEADBOB_FREQUENCY) * IDLE_HEADBOB_MOVE_AMOUNT,
            0
        )


func _on_mouse_slider_value_changed(value: float) -> void:
    look_sensitivity = value

func _on_controller_slider_value_changed(value: float) -> void:
    controller_look_sensitivity = value

func take_damage(damage : int):
    super(damage)
    ui.health.value = current_health

func heal(heal_value : int):
    super(heal_value)
    ui.health.value = current_health

func destroy():
    Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
    ui.death.show()
