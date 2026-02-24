#pragma once
/**
 * VoxelBlock — GPU Mesh + Chunk Mesh Builder
 * Phase 2:  VAO/VBO/EBO abstraction.
 * Chunk meshing: naive per-face culling (greedy meshing in Phase 4).
 */

#include <vector>
#include <cstdint>
#include <string>

namespace VoxelBlock {

class Chunk;   // forward

// One vertex: position(3) + normal(3) + color(3) + uv(2) = 11 floats
struct Vertex {
    float px, py, pz;
    float nx, ny, nz;
    float cr, cg, cb;
    float u, v;
};

class Mesh {
public:
    Mesh() = default;
    ~Mesh();
    Mesh(const Mesh&) = delete;
    Mesh& operator=(const Mesh&) = delete;
    Mesh(Mesh&& o) noexcept;
    Mesh& operator=(Mesh&& o) noexcept;

    void upload(const std::vector<Vertex>& vertices,
                const std::vector<uint32_t>& indices);
    void draw()  const;
    void update(const std::vector<Vertex>& vertices,
                const std::vector<uint32_t>& indices);

    bool  empty()       const { return _index_count == 0; }
    int   index_count() const { return _index_count; }

private:
    uint32_t _vao = 0, _vbo = 0, _ebo = 0;
    int      _index_count = 0;
};

// Face directions
enum class Face { PosX, NegX, PosY, NegY, PosZ, NegZ };

// Build mesh from chunk voxels — face-culling against neighbours
// colours come from BlockRegistry
struct ChunkMeshData {
    std::vector<Vertex>   vertices;
    std::vector<uint32_t> indices;
};

ChunkMeshData build_chunk_mesh(const Chunk& chunk,
                               const Chunk* nx, const Chunk* px,
                               const Chunk* nz, const Chunk* pz);

} // namespace VoxelBlock
