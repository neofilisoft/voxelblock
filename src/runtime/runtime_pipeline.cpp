#include "runtime/runtime_pipeline.hpp"

namespace VoxelBlock::Runtime {

RuntimePipeline::RuntimePipeline()
    : _bridge(&_materials)
{
    _post_graph.set_default_tier_a_2d_stack();
}

void RuntimePipeline::step(ECS::Registry& registry, float dt)
{
    _physics.step(dt);

    // Apply null-physics preview state back to ECS transform components.
    registry.for_each<ECS::TransformComponent, ECS::RigidBodyComponent>(
        [&](ECS::Entity e, ECS::TransformComponent& tx, ECS::RigidBodyComponent&) {
            const auto body_id = e.index + 1U; // deterministic placeholder mapping for scaffold stage
            const auto state = _physics.get_body_state(body_id);
            if (!state) return;
            tx.position.x = state->transform.position.x;
            tx.position.y = state->transform.position.y;
            tx.position.z = state->transform.position.z;
        }
    );

    rebuild_render_scenes(registry);
}

void RuntimePipeline::rebuild_render_scenes(const ECS::Registry& registry)
{
    switch (_mode) {
    case ProjectRenderMode::Mode2D:
        _post_graph.set_default_tier_a_2d_stack();
        break;
    case ProjectRenderMode::ModeHD2D:
    case ProjectRenderMode::Mode3D:
        _post_graph.set_default_tier_b_unified_stack();
        break;
    }
    _bridge.build_from_ecs(registry, _scene_2d, _scene_3d);
}

} // namespace VoxelBlock::Runtime
