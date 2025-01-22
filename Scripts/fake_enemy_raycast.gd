extends RayCast3D
@export var area3d : Area3D
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
    area3d.position = Vector3(area3d.position.x,self.get_collision_point().y,area3d.position.z)
