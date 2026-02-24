#include "runtime/scene_render_bridge.hpp"

namespace VoxelBlock::Runtime {

void SceneRenderBridge::build_from_ecs(const ECS::Registry& registry, RenderScene2D& out_2d, RenderScene3D& out_3d) const
{
    out_2d.clear();
    out_3d.clear();

    registry.for_each<ECS::TransformComponent>([&](ECS::Entity e, const ECS::TransformComponent& tx) {
        if (auto* mesh = registry.try_get<ECS::StaticMeshComponent>(e)) {
            Mesh3DInstance inst{};
            inst.position[0] = tx.position.x;
            inst.position[1] = tx.position.y;
            inst.position[2] = tx.position.z;
            inst.rotation_euler_deg[0] = tx.rotation_euler_deg.x;
            inst.rotation_euler_deg[1] = tx.rotation_euler_deg.y;
            inst.rotation_euler_deg[2] = tx.rotation_euler_deg.z;
            inst.scale[0] = tx.scale.x;
            inst.scale[1] = tx.scale.y;
            inst.scale[2] = tx.scale.z;
            inst.mesh_asset = mesh->mesh_asset;
            inst.visible = mesh->visible;
            if (_materials && !mesh->material_asset.empty()) {
                if (auto* rec = _materials->find_by_id(mesh->material_asset)) {
                    inst.material = rec->handle;
                }
            }
            out_3d.opaque_meshes.push_back(std::move(inst));
            return;
        }

        Sprite2DInstance sprite{};
        sprite.x = tx.position.x;
        sprite.y = tx.position.y;
        sprite.z = tx.position.z;
        sprite.width = 16.0f;
        sprite.height = 16.0f;

        if (auto* name = registry.try_get<ECS::NameComponent>(e)) {
            sprite.texture_asset = name->value; // placeholder asset lookup key
        }
        if (auto* block = registry.try_get<ECS::VoxelBlockTagComponent>(e)) {
            sprite.texture_asset = block->block_type;
            sprite.static_batched = true;
        }

        out_2d.dynamic_sprites.push_back(std::move(sprite));
    });
}

} // namespace VoxelBlock::Runtime

