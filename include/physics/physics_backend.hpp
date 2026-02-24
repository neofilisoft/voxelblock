#pragma once

#include "physics/physics_types.hpp"

#include <cstdint>
#include <optional>

namespace VoxelBlock::Physics {

class IPhysicsBackend {
public:
    virtual ~IPhysicsBackend() = default;

    virtual const char* backend_name() const noexcept = 0;
    virtual void reset() = 0;
    virtual std::uint32_t create_body(const RigidBodyDesc& desc, const Transform& initial_transform) = 0;
    virtual bool destroy_body(std::uint32_t body_id) = 0;
    virtual bool set_body_transform(std::uint32_t body_id, const Transform& transform) = 0;
    virtual std::optional<BodyState> get_body_state(std::uint32_t body_id) const = 0;
    virtual void step(float dt) = 0;
};

} // namespace VoxelBlock::Physics

