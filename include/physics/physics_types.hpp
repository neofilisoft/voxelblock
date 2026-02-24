#pragma once

#include <cstdint>

namespace VoxelBlock::Physics {

struct Vec3 {
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
};

enum class BodyType : std::uint8_t {
    Static = 0,
    Kinematic = 1,
    Dynamic = 2,
};

struct Transform {
    Vec3 position{};
    Vec3 rotation_euler_deg{};
    Vec3 scale{1.0f, 1.0f, 1.0f};
};

struct RigidBodyDesc {
    BodyType type = BodyType::Static;
    float mass = 1.0f;
    bool use_gravity = true;
    Vec3 box_half_extents{0.5f, 0.5f, 0.5f};
};

struct BodyState {
    std::uint32_t id = 0;
    Transform transform{};
    Vec3 linear_velocity{};
    bool sleeping = false;
};

} // namespace VoxelBlock::Physics

