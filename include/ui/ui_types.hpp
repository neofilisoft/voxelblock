#pragma once

#include <algorithm>

namespace VoxelBlock::UI {

struct Vec2 {
    float x = 0.0f;
    float y = 0.0f;
};

struct Color {
    float r = 1.0f;
    float g = 1.0f;
    float b = 1.0f;
    float a = 1.0f;
};

struct Rect {
    float x = 0.0f;
    float y = 0.0f;
    float w = 0.0f;
    float h = 0.0f;

    [[nodiscard]] bool contains(Vec2 p) const noexcept
    {
        return p.x >= x && p.y >= y && p.x <= (x + w) && p.y <= (y + h);
    }
};

struct Thickness {
    float left = 0.0f;
    float top = 0.0f;
    float right = 0.0f;
    float bottom = 0.0f;
};

[[nodiscard]] inline Rect inset(Rect r, Thickness t) noexcept
{
    r.x += t.left;
    r.y += t.top;
    r.w = std::max(0.0f, r.w - (t.left + t.right));
    r.h = std::max(0.0f, r.h - (t.top + t.bottom));
    return r;
}

} // namespace VoxelBlock::UI

