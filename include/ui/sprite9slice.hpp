#pragma once

#include "ui/ui_types.hpp"

#include <array>
#include <cstdint>
#include <string>

namespace VoxelBlock::UI {

struct Sprite9SliceDesc {
    std::string texture_asset;
    float texture_width = 1.0f;
    float texture_height = 1.0f;
    Thickness border{};
};

struct SpriteQuad {
    Rect dest{};
    Rect uv{};
    Color tint{};
};

class Sprite9Slice {
public:
    explicit Sprite9Slice(Sprite9SliceDesc desc = {}) : _desc(std::move(desc)) {}

    void set_desc(Sprite9SliceDesc desc) { _desc = std::move(desc); }
    [[nodiscard]] const Sprite9SliceDesc& desc() const noexcept { return _desc; }

    [[nodiscard]] std::array<SpriteQuad, 9> build(Rect dest, Color tint = {}) const noexcept;

private:
    Sprite9SliceDesc _desc{};
};

} // namespace VoxelBlock::UI

