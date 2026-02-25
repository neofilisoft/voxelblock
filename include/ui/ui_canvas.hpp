#pragma once

#include "ui/ui_types.hpp"

namespace VoxelBlock::UI {

enum class CanvasSpace {
    ScreenSpace,
    WorldSpace,
};

struct UICanvasDesc {
    CanvasSpace space = CanvasSpace::ScreenSpace;
    float width = 1280.0f;
    float height = 720.0f;
    float pixels_per_unit = 1.0f;
    int sort_order = 0;
};

class UICanvas {
public:
    explicit UICanvas(UICanvasDesc desc = {}) : _desc(desc) {}

    void configure(const UICanvasDesc& desc) noexcept { _desc = desc; }
    [[nodiscard]] const UICanvasDesc& desc() const noexcept { return _desc; }

    void set_viewport(float width, float height) noexcept;
    [[nodiscard]] Vec2 viewport_size() const noexcept { return _viewport_size; }
    [[nodiscard]] Vec2 logical_size() const noexcept;

    [[nodiscard]] Vec2 screen_to_canvas(Vec2 p) const noexcept;
    [[nodiscard]] Vec2 canvas_to_screen(Vec2 p) const noexcept;
    [[nodiscard]] Rect canvas_rect() const noexcept { return {0.0f, 0.0f, logical_size().x, logical_size().y}; }

private:
    UICanvasDesc _desc{};
    Vec2 _viewport_size{1280.0f, 720.0f};
};

} // namespace VoxelBlock::UI

