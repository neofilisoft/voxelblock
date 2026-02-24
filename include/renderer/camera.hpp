#pragma once
/**
 * VoxelBlock — First-Person Camera
 */

#include "shader.hpp"
#include "core/types.hpp"
#include <cmath>

namespace VoxelBlock {

class Camera {
public:
    // Position & orientation
    Vec3  position   = {0.f, 20.f, 0.f};
    float yaw        = -90.f;   // degrees
    float pitch      = 0.f;     // degrees
    float fov        = 70.f;    // degrees
    float near_plane = 0.1f;
    float far_plane  = 512.f;
    float sensitivity = 0.1f;
    float move_speed  = 9.f;

    // Derived (recalculated each frame)
    Vec3 front  = {0, 0, -1};
    Vec3 right  = {1, 0,  0};
    Vec3 up     = {0, 1,  0};

    void update_vectors();
    void process_mouse(float dx, float dy);
    void move(Vec3 dir, float dt);   // dir is in world space (WASD mapped)

    Mat4 view_matrix()                           const;
    Mat4 projection_matrix(float aspect)         const;

    // Raycast helper — returns point + direction
    Vec3 forward() const { return front; }

private:
    static constexpr float PI = 3.14159265f;
    static float to_rad(float deg) { return deg * PI / 180.f; }
    static Vec3  normalize(Vec3 v);
    static Vec3  cross(Vec3 a, Vec3 b);
};

} // namespace VoxelBlock
