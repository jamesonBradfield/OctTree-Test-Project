class_name QuadTreeHelper

static func vec2_vec3(vec2 : Vector2) -> Vector3:
    return Vector3(vec2.x,0,vec2.y)

static func vec3_vec2(vec3 : Vector3) -> Vector2:
    return Vector2(vec3.x,vec3.z)

