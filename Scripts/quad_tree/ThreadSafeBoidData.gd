class_name ThreadSafeBoidData
extends RefCounted

# Core data
var position: Vector3
var velocity: Vector3
var id: int

# Flocking parameters - Consolidated for cache efficiency
var flocking_params = {
	"alignment_strength": 0.15,  # Reduced from 0.3
	"cohesion_strength": 0.1,  # Reduced from 0.2
	"separation_strength": 0.2,  # Reduced from 0.4
	"max_speed": 10.0,  # Reduced from 15.0
	"max_force": 0.5,  # New parameter for force limiting
	"smoothing": 0.1,  # New smoothing factor
	# Radius parameters
	"alignment_radius": 12.0,
	"cohesion_radius": 12.0,
	"separation_radius": 12.0,
	# Leader parameters
	"leader_influence": 0.5,  # Reduced from 1.0
	"leader_detection_radius": 30.0,
	"min_leader_distance": 8.0
}

# Cached vectors
var _temp_vector: Vector3
var _steering_force: Vector3
var _squared_radii = {}


func _init(p_position: Vector3, p_velocity: Vector3, p_id: int):
	position = p_position
	velocity = velocity.lerp(p_velocity, 0.1)  # Smooth initial velocity
	id = p_id
	_temp_vector = Vector3.ZERO
	_steering_force = Vector3.ZERO

	# Pre-calculate squared radii
	_squared_radii = {
		"alignment": flocking_params.alignment_radius * flocking_params.alignment_radius,
		"cohesion": flocking_params.cohesion_radius * flocking_params.cohesion_radius,
		"separation": flocking_params.separation_radius * flocking_params.separation_radius,
		"leader": flocking_params.leader_detection_radius * flocking_params.leader_detection_radius
	}


static func compute_flocking(
	boid: ThreadSafeBoidData, nearby: Array[ThreadSafeBoidData], leader_pos: Vector3
) -> Vector3:
	# Pre-calculate parameters once
	var params = boid.flocking_params
	var max_speed: float = params.max_speed
	var max_force: float = params.max_force
	var smoothing: float = params.smoothing

	# Flocking force calculations
	var alignment = Vector3.ZERO
	var cohesion = Vector3.ZERO
	var separation = Vector3.ZERO
	var alignment_count := 0
	var cohesion_count := 0
	var separation_count := 0

	# Single pass through nearby boids with early exits
	for other in nearby:
		if other.id == boid.id:
			continue

		var offset = other.position - boid.position
		var dist_sq = offset.length_squared()

		# Alignment
		if dist_sq < params.alignment_radius * params.alignment_radius:
			alignment += other.velocity
			alignment_count += 1

		# Cohesion
		if dist_sq < params.cohesion_radius * params.cohesion_radius:
			cohesion += other.position
			cohesion_count += 1

		# Separation (using inverse square falloff)
		if dist_sq < params.separation_radius * params.separation_radius && dist_sq > 0.0001:
			separation -= offset / (dist_sq + 1.0)  # Avoid division by zero

	# Normalize forces
	if alignment_count > 0:
		alignment = alignment.normalized() * params.alignment_strength
	if cohesion_count > 0:
		cohesion = (
			(cohesion / cohesion_count - boid.position).normalized() * params.cohesion_strength
		)
	if separation_count > 0:
		separation = separation.normalized() * params.separation_strength

	# Leader following (2D calculation)
	var to_leader = Vector3(leader_pos.x, 0, leader_pos.z) - boid.position
	var leader_dist_sq = to_leader.length_squared()
	var leader_force = Vector3.ZERO

	if leader_dist_sq < params.leader_detection_radius * params.leader_detection_radius:
		var leader_dist = sqrt(leader_dist_sq)
		if leader_dist > params.min_leader_distance:
			leader_force = to_leader.normalized() * params.leader_influence

	# Combine forces
	var desired_velocity = alignment + cohesion + separation + leader_force

	# Limit and steer
	var speed = desired_velocity.length()
	if speed > 0:
		desired_velocity = (desired_velocity / speed) * max_speed  # Faster than normalized()

	var steering = desired_velocity - boid.velocity
	steering = steering.limit_length(max_force)

	# Smoothed velocity update
	return boid.velocity.lerp(boid.velocity + steering, smoothing)


static func _calculate_flocking_forces(
	boid: ThreadSafeBoidData, nearby: Array[ThreadSafeBoidData]
) -> Dictionary:
	var total_alignment := Vector3.ZERO
	var total_cohesion := Vector3.ZERO
	var total_separation := Vector3.ZERO
	var counts := Vector3.ZERO  # x=alignment, y=cohesion, z=separation

	for other in nearby:
		if other.id == boid.id:
			continue

		var offset = other.position - boid.position
		var dist_squared = offset.length_squared()

		if dist_squared < boid._squared_radii.alignment:
			total_alignment += other.velocity
			counts.x += 1

		if dist_squared < boid._squared_radii.cohesion:
			total_cohesion += other.position
			counts.y += 1

		if dist_squared < boid._squared_radii.separation:
			var dist = sqrt(dist_squared)
			if dist > 0:
				boid._temp_vector = offset.normalized() * (-1.0 / dist)
				total_separation += boid._temp_vector
				counts.z += 1

	var forces = {"alignment": Vector3.ZERO, "cohesion": Vector3.ZERO, "separation": Vector3.ZERO}

	if counts.x > 0:
		forces.alignment = (total_alignment / counts.x).normalized()

	if counts.y > 0:
		var center_of_mass = total_cohesion / counts.y
		forces.cohesion = (center_of_mass - boid.position).normalized()

	if counts.z > 0:
		forces.separation = (total_separation / counts.z).normalized()

	return forces


static func _calculate_leader_force(boid: ThreadSafeBoidData, leader_pos: Vector3) -> Vector3:
	var dist_squared = boid.position.distance_squared_to(leader_pos)
	if dist_squared >= boid._squared_radii.leader:
		return Vector3.ZERO

	var dist = sqrt(dist_squared)
	if dist <= boid.flocking_params.min_leader_distance:
		return Vector3.ZERO

	var to_leader = (leader_pos - boid.position).normalized()
	var influence = 1.0 - (dist / boid.flocking_params.leader_detection_radius)
	return to_leader * influence


static func from_boid(boid: Node3D, id: int) -> ThreadSafeBoidData:
	var data = ThreadSafeBoidData.new(boid.position, boid.velocity, id)

	if boid.has_method("get_boid_params"):
		var params = boid.get_boid_params()
		for key in params:
			if data.flocking_params.has(key):
				data.flocking_params[key] = params[key]

		# Update squared radii
		data._squared_radii.alignment = (
			data.flocking_params.alignment_radius * data.flocking_params.alignment_radius
		)
		data._squared_radii.cohesion = (
			data.flocking_params.cohesion_radius * data.flocking_params.cohesion_radius
		)
		data._squared_radii.separation = (
			data.flocking_params.separation_radius * data.flocking_params.separation_radius
		)
		data._squared_radii.leader = (
			data.flocking_params.leader_detection_radius
			* data.flocking_params.leader_detection_radius
		)

	return data
