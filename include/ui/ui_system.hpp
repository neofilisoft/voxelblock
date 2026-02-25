#pragma once

#include "ui/sprite9slice.hpp"
#include "ui/ui_canvas.hpp"
#include "ui/ui_font.hpp"
#include "ui/ui_input.hpp"
#include "ui/ui_renderer.hpp"

#include <cstdint>
#include <optional>
#include <string>
#include <unordered_map>
#include <vector>

namespace VoxelBlock::UI {

using WidgetId = std::uint32_t;

enum class WidgetLayout : std::uint8_t {
    Absolute,
    VerticalStack,
    HorizontalStack,
};

struct UIWidget {
    WidgetId id = 0;
    WidgetId parent = 0;
    std::vector<WidgetId> children;

    std::string name;
    Rect rect{};
    Rect computed_rect{};
    Thickness padding{};
    bool visible = true;
    bool enabled = true;
    WidgetLayout layout = WidgetLayout::Absolute;

    Color background{};
    bool draw_background = false;
    std::optional<std::string> text;
    Color text_color{1.0f, 1.0f, 1.0f, 1.0f};
    std::optional<Sprite9SliceDesc> panel_9slice;
};

class UISystem {
public:
    UISystem();

    void set_canvas(UICanvas canvas) { _canvas = std::move(canvas); }
    [[nodiscard]] UICanvas& canvas() noexcept { return _canvas; }
    [[nodiscard]] const UICanvas& canvas() const noexcept { return _canvas; }

    WidgetId create_widget(std::string name = {});
    bool destroy_widget(WidgetId id);
    bool set_parent(WidgetId child, WidgetId parent);
    UIWidget* try_get(WidgetId id);
    const UIWidget* try_get(WidgetId id) const;

    WidgetId root_widget() const noexcept { return _root; }

    void update_layout();
    void update(float dt, const UIInputState& input);
    void build_draw_commands(UIRenderer& renderer, const UIFont& default_font) const;

    [[nodiscard]] WidgetId hovered_widget() const noexcept { return _hovered_widget; }
    [[nodiscard]] WidgetId focused_widget() const noexcept { return _focused_widget; }
    [[nodiscard]] std::size_t widget_count() const noexcept { return _widgets.size(); }

private:
    WidgetId _next_id = 1;
    WidgetId _root = 0;
    WidgetId _hovered_widget = 0;
    WidgetId _focused_widget = 0;

    UICanvas _canvas{};
    std::unordered_map<WidgetId, UIWidget> _widgets;

    void layout_children(UIWidget& widget);
    void update_hover_and_focus(const UIInputState& input);
    WidgetId hit_test(Vec2 canvas_point) const;
    void emit_widget_draw(const UIWidget& widget, UIRenderer& renderer, const UIFont& default_font) const;
};

} // namespace VoxelBlock::UI

