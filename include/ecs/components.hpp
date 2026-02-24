#pragma once

#include <cstdint>
#include <string>

namespace VoxelBlock::ECS {

struct Vec3 {
    float x = 0.0f;
    float y = 0.0f;
    float z = 0.0f;
};

struct TransformComponent {
    Vec3 position{};
    Vec3 rotation_euler_deg{};
    Vec3 scale{1.0f, 1.0f, 1.0f};
};

struct NameComponent {
    std::string value;
};

struct StaticMeshComponent {
    std::string mesh_asset;
    std::string material_asset;
    bool visible = true;
};

enum class BodyType : std::uint8_t {
    Static,
    Kinematic,
    Dynamic,
};

struct RigidBodyComponent {
    BodyType body_type = BodyType::Static;
    float mass = 1.0f;
    bool use_gravity = true;
};

struct BoxColliderComponent {
    Vec3 half_extents{0.5f, 0.5f, 0.5f};
    bool is_trigger = false;
};

struct ScriptComponent {
    std::string lua_script;
    bool enabled = true;
};

struct AudioSourceComponent {
    std::string wav_asset;
    float volume = 1.0f;
    bool loop = false;
    bool play_on_start = false;
};

struct VoxelBlockTagComponent {
    std::string block_type;
    std::int32_t grid_x = 0;
    std::int32_t grid_y = 0;
    std::int32_t grid_z = 0;
};

} // namespace VoxelBlock::ECS

