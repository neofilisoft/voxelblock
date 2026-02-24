#pragma once

#include "physics/physics_backend.hpp"

#include <memory>
#include <optional>

namespace VoxelBlock::Physics {

class PhysicsWorld {
public:
    PhysicsWorld();
    explicit PhysicsWorld(std::unique_ptr<IPhysicsBackend> backend);
    ~PhysicsWorld();

    void set_backend(std::unique_ptr<IPhysicsBackend> backend);
    [[nodiscard]] const IPhysicsBackend& backend() const noexcept { return *_backend; }
    [[nodiscard]] IPhysicsBackend& backend() noexcept { return *_backend; }

    std::uint32_t create_body(const RigidBodyDesc& desc, const Transform& initial_transform);
    bool destroy_body(std::uint32_t body_id);
    bool set_body_transform(std::uint32_t body_id, const Transform& transform);
    [[nodiscard]] std::optional<BodyState> get_body_state(std::uint32_t body_id) const;
    void step(float dt);
    void reset();

private:
    std::unique_ptr<IPhysicsBackend> _backend;
};

std::unique_ptr<IPhysicsBackend> make_null_physics_backend();

} // namespace VoxelBlock::Physics

