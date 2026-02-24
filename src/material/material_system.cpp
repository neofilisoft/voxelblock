#include "material/material_system.hpp"

#include <utility>

namespace VoxelBlock::Material {

MaterialHandle MaterialSystem::register_pbr(PbrMaterial material)
{
    const auto id = material.id;
    return register_asset(ShadingModel::PbrMetalRough, MaterialAsset{std::move(material)}, id);
}

MaterialHandle MaterialSystem::register_lit_sprite(LitSpriteMaterial material)
{
    const auto id = material.id;
    return register_asset(ShadingModel::Lit2D, MaterialAsset{std::move(material)}, id);
}

const MaterialRecord* MaterialSystem::find(MaterialHandle handle) const noexcept
{
    auto it = _records.find(handle);
    return it == _records.end() ? nullptr : &it->second;
}

const MaterialRecord* MaterialSystem::find_by_id(const std::string& id) const noexcept
{
    auto it = _ids.find(id);
    if (it == _ids.end()) return nullptr;
    return find(it->second);
}

bool MaterialSystem::erase(MaterialHandle handle)
{
    auto it = _records.find(handle);
    if (it == _records.end()) return false;

    std::visit([this, handle](const auto& typed) {
        auto id_it = _ids.find(typed.id);
        if (id_it != _ids.end() && id_it->second == handle) {
            _ids.erase(id_it);
        }
    }, it->second.asset);

    _records.erase(it);
    return true;
}

void MaterialSystem::clear()
{
    _records.clear();
    _ids.clear();
    _next_handle = 1;
}

MaterialHandle MaterialSystem::register_asset(ShadingModel model, MaterialAsset asset, const std::string& id)
{
    if (id.empty()) return 0;

    if (auto existing = _ids.find(id); existing != _ids.end()) {
        auto& rec = _records[existing->second];
        rec.model = model;
        rec.asset = std::move(asset);
        return rec.handle;
    }

    const auto handle = _next_handle++;
    _records.emplace(handle, MaterialRecord{
        .handle = handle,
        .model = model,
        .asset = std::move(asset),
    });
    _ids[id] = handle;
    return handle;
}

} // namespace VoxelBlock::Material
