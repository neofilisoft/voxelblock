#include "scene/scene_serializer.hpp"

#include <fstream>
#include <iomanip>
#include <sstream>

namespace VoxelBlock::Scene {
namespace {

std::string json_escape(const std::string& s)
{
    std::string out;
    out.reserve(s.size() + 8);
    for (char c : s) {
        switch (c) {
        case '\\': out += "\\\\"; break;
        case '"':  out += "\\\""; break;
        case '\n': out += "\\n"; break;
        case '\r': out += "\\r"; break;
        case '\t': out += "\\t"; break;
        default:   out += c; break;
        }
    }
    return out;
}

const char* body_type_to_string(ECS::BodyType t)
{
    switch (t) {
    case ECS::BodyType::Static: return "static";
    case ECS::BodyType::Kinematic: return "kinematic";
    case ECS::BodyType::Dynamic: return "dynamic";
    }
    return "static";
}

void append_vec3(std::ostringstream& os, const Vec3& v)
{
    os << "{\"x\":" << v.x << ",\"y\":" << v.y << ",\"z\":" << v.z << "}";
}

} // namespace

SceneDocument SceneSerializer::capture_registry(const ECS::Registry& registry, std::string scene_name)
{
    SceneDocument doc{};
    doc.scene_name = std::move(scene_name);

    registry.for_each<ECS::TransformComponent>([&](ECS::Entity e, const ECS::TransformComponent& tx) {
        SerializedEntity row{};
        row.entity_index = e.index;
        row.generation = e.generation;
        row.transform = SerializedTransform{
            .position = {tx.position.x, tx.position.y, tx.position.z},
            .rotation_euler_deg = {tx.rotation_euler_deg.x, tx.rotation_euler_deg.y, tx.rotation_euler_deg.z},
            .scale = {tx.scale.x, tx.scale.y, tx.scale.z},
        };

        if (auto* name = registry.try_get<ECS::NameComponent>(e)) {
            row.name = name->value;
        }
        if (auto* mesh = registry.try_get<ECS::StaticMeshComponent>(e)) {
            row.static_mesh_asset = mesh->mesh_asset;
            row.material_asset = mesh->material_asset;
        }
        if (auto* rb = registry.try_get<ECS::RigidBodyComponent>(e)) {
            row.rigid_body = SerializedRigidBody{
                .body_type = body_type_to_string(rb->body_type),
                .mass = rb->mass,
                .use_gravity = rb->use_gravity,
            };
        }
        if (auto* col = registry.try_get<ECS::BoxColliderComponent>(e)) {
            row.box_collider = SerializedBoxCollider{
                .half_extents = {col->half_extents.x, col->half_extents.y, col->half_extents.z},
                .is_trigger = col->is_trigger,
            };
        }
        if (auto* script = registry.try_get<ECS::ScriptComponent>(e)) {
            row.lua_script = script->lua_script;
        }
        if (auto* audio = registry.try_get<ECS::AudioSourceComponent>(e)) {
            row.wav_asset = audio->wav_asset;
        }
        if (auto* voxel = registry.try_get<ECS::VoxelBlockTagComponent>(e)) {
            row.voxel_block_type = voxel->block_type;
        }

        doc.entities.push_back(std::move(row));
    });

    return doc;
}

std::string SceneSerializer::to_json(const SceneDocument& doc)
{
    std::ostringstream os;
    os << "{\n";
    os << "  \"schema_version\": " << doc.schema_version << ",\n";
    os << "  \"scene_name\": \"" << json_escape(doc.scene_name) << "\",\n";
    os << "  \"entities\": [\n";
    for (std::size_t i = 0; i < doc.entities.size(); ++i) {
        const auto& e = doc.entities[i];
        os << "    {\n";
        os << "      \"entity_index\": " << e.entity_index << ",\n";
        os << "      \"generation\": " << e.generation;

        auto emit_str_opt = [&](const char* key, const std::optional<std::string>& v) {
            if (!v) return;
            os << ",\n      \"" << key << "\": \"" << json_escape(*v) << "\"";
        };

        emit_str_opt("name", e.name);
        emit_str_opt("static_mesh_asset", e.static_mesh_asset);
        emit_str_opt("material_asset", e.material_asset);
        emit_str_opt("lua_script", e.lua_script);
        emit_str_opt("wav_asset", e.wav_asset);
        emit_str_opt("voxel_block_type", e.voxel_block_type);

        if (e.transform) {
            os << ",\n      \"transform\": {\"position\": ";
            append_vec3(os, e.transform->position);
            os << ", \"rotation_euler_deg\": ";
            append_vec3(os, e.transform->rotation_euler_deg);
            os << ", \"scale\": ";
            append_vec3(os, e.transform->scale);
            os << "}";
        }

        if (e.rigid_body) {
            os << ",\n      \"rigid_body\": {"
               << "\"body_type\": \"" << json_escape(e.rigid_body->body_type) << "\", "
               << "\"mass\": " << e.rigid_body->mass << ", "
               << "\"use_gravity\": " << (e.rigid_body->use_gravity ? "true" : "false") << "}";
        }

        if (e.box_collider) {
            os << ",\n      \"box_collider\": {\"half_extents\": ";
            append_vec3(os, e.box_collider->half_extents);
            os << ", \"is_trigger\": " << (e.box_collider->is_trigger ? "true" : "false") << "}";
        }

        os << "\n    }";
        if (i + 1 < doc.entities.size()) os << ",";
        os << "\n";
    }
    os << "  ]\n";
    os << "}\n";
    return os.str();
}

bool SceneSerializer::save_json_file(const SceneDocument& doc, const std::filesystem::path& path, std::string* error)
{
    std::error_code ec;
    const auto parent = path.parent_path();
    if (!parent.empty()) {
        std::filesystem::create_directories(parent, ec);
        if (ec) {
            if (error) *error = "Failed to create directories: " + ec.message();
            return false;
        }
    }

    std::ofstream out(path, std::ios::binary | std::ios::trunc);
    if (!out) {
        if (error) *error = "Failed to open file for writing";
        return false;
    }
    out << to_json(doc);
    if (!out.good()) {
        if (error) *error = "Failed while writing JSON";
        return false;
    }
    return true;
}

} // namespace VoxelBlock::Scene

