#pragma once

#include "ecs/components.hpp"
#include "ecs/registry.hpp"

namespace VoxelBlock::ECS {

class ISystem {
public:
    virtual ~ISystem() = default;
    virtual void update(Registry& registry, float dt) = 0;
};

// Example logic system that applies a simple gravity integration to dynamic rigid bodies.
// A real physics backend should replace this with proper broadphase/narrowphase/solver integration.
class GravityPreviewSystem final : public ISystem {
public:
    void update(Registry& registry, float dt) override
    {
        if (dt <= 0.0f) return;
        registry.for_each<TransformComponent, RigidBodyComponent>(
            [dt](Entity, TransformComponent& tx, RigidBodyComponent& rb) {
                if (rb.body_type != BodyType::Dynamic || !rb.use_gravity) return;
                tx.position.y -= 9.81f * dt;
            }
        );
    }
};

} // namespace VoxelBlock::ECS

