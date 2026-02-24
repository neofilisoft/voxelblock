#pragma once
/**
 * VoxelBlock — Shader System
 * Loads, compiles, links GLSL programs.
 * Provides type-safe uniform setters.
 */

#include "core/types.hpp"
#include <string>
#include <unordered_map>
#include <array>
#include <cstdint>

namespace VoxelBlock {

class Shader {
public:
    Shader() = default;
    Shader(const std::string& vert_src, const std::string& frag_src);
    ~Shader();

    // Non-copyable, movable
    Shader(const Shader&) = delete;
    Shader& operator=(const Shader&) = delete;
    Shader(Shader&& o) noexcept;
    Shader& operator=(Shader&& o) noexcept;

    static Shader from_files(const std::string& vert_path,
                              const std::string& frag_path);

    void bind()   const;
    void unbind() const;
    bool valid()  const { return _program != 0; }

    // Uniform setters
    void set_int  (const std::string& name, int v)           const;
    void set_float(const std::string& name, float v)         const;
    void set_vec2 (const std::string& name, Vec2 v)          const;
    void set_vec3 (const std::string& name, Vec3 v)          const;
    void set_vec4 (const std::string& name, Vec4 v)          const;
    void set_mat4 (const std::string& name, const Mat4& m)   const;
    void set_mat4 (const std::string& name, const float* m)  const;

private:
    uint32_t _program = 0;
    mutable std::unordered_map<std::string, int> _uniform_cache;

    int  _get_uniform(const std::string& name) const;
    static uint32_t _compile(uint32_t type, const std::string& src);
};

} // namespace VoxelBlock
