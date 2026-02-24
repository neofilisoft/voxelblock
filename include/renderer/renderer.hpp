#pragma once
/**
 * VoxelBlock — Main Renderer
 * Owns shader programs, chunk mesh cache, draw-call dispatch.
 * The editor (Phase 3) accesses render output via render-to-texture
 * exposed through the C-API (voxelblock_capi.h).
 */

#include "shader.hpp"
#include "camera.hpp"
#include "mesh.hpp"
#include <unordered_map>
#include <memory>
#include <cstdint>

namespace VoxelBlock {

class World;
class Window;

struct RenderStats {
    int chunks_drawn    = 0;
    int draw_calls      = 0;
    int triangles       = 0;
    float frame_ms      = 0.f;
};

class Renderer {
public:
    Renderer() = default;
    ~Renderer();

    // init: call after OpenGL context is current
    bool init(int viewport_width, int viewport_height);
    void shutdown();

    void resize(int w, int h);

    // Per-frame
    void begin_frame();
    void render_world(const World& world, const Camera& cam);
    void end_frame();

    // Editor: render to texture (Phase 3)
    // Returns OpenGL texture ID of the scene colour buffer
    uint32_t scene_texture_id() const { return _scene_colour_tex; }

    const RenderStats& stats() const { return _stats; }

    // Skybox colour (set from Lua / editor)
    void set_sky_color(float r, float g, float b) {
        _sky_r=r; _sky_g=g; _sky_b=b;
    }

private:
    Shader   _chunk_shader;
    Shader   _sky_shader;

    // Chunk mesh cache: chunk key → Mesh
    struct CKey { int cx, cz; };
    struct CKeyHash {
        size_t operator()(const CKey& k) const {
            return std::hash<int>{}(k.cx) ^ (std::hash<int>{}(k.cz) << 16);
        }
    };
    struct CKeyEq {
        bool operator()(const CKey& a, const CKey& b) const {
            return a.cx==b.cx && a.cz==b.cz;
        }
    };
    std::unordered_map<CKey, std::unique_ptr<Mesh>, CKeyHash, CKeyEq> _chunk_meshes;

    // Dirty chunks (need re-mesh)
    std::unordered_map<CKey, bool, CKeyHash, CKeyEq> _dirty;

    void _rebuild_dirty(const World& world);
    void _draw_chunks(const Camera& cam);

    // Render-to-texture (for editor viewport)
    uint32_t _fbo             = 0;
    uint32_t _scene_colour_tex = 0;
    uint32_t _depth_rbo       = 0;
    int      _vp_w = 0, _vp_h = 0;
    void _create_framebuffer(int w, int h);

    float _sky_r = 0.53f, _sky_g = 0.81f, _sky_b = 0.92f;
    RenderStats _stats;
};

} // namespace VoxelBlock
