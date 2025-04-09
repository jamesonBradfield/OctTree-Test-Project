extends CenterContainer
@export var reticle_lines : Array[Line2D]
var player : Player = Globals.player
@export var reticle_speed : float = 0.25
@export var reticle_distance : float = 2.0
@export var velocity_indicator : RadialProgress
@export var DOT_RADIUS : float = 1.0
@export var DOT_COLOR : Color = Color.WHITE

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
    player= Globals.player
    queue_redraw()

func _draw():
    draw_circle(Vector2(0,0),DOT_RADIUS,DOT_COLOR)

func _process(delta):
    adjust_reticle_lines()


func adjust_reticle_lines():
    var vel = player.get_real_velocity()
    var origin = Vector3(0,0,0)
    var pos = Vector2(0,0)
    var speed = origin.distance_to(vel)
    reticle_lines[0].position = lerp(reticle_lines[0].position,pos + Vector2(0, -speed * reticle_distance),reticle_speed)
    reticle_lines[1].position = lerp(reticle_lines[1].position,pos + Vector2(speed * reticle_distance,0),reticle_speed)
    reticle_lines[2].position = lerp(reticle_lines[2].position,pos + Vector2(0, speed * reticle_distance),reticle_speed)
    reticle_lines[3].position = lerp(reticle_lines[3].position,pos + Vector2( -speed * reticle_distance,0),reticle_speed)

    # velocity_indicator.radius = lerp(speed * reticle_distance,velocity_indicator.radius, reticle_speed)
