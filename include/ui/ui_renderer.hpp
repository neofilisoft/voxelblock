#pragma once

#include "ui/ui_font.hpp"
#include "ui/ui_types.hpp"

#include <cstdint>
#include <string>
#include <string_view>
#include <vector>

namespace VoxelBlock::UI {

struct UIRectCommand {
    Rect rect{};
    Color color{};
    int layer = 0;
};

struct UISpriteCommand {
    Rect rect{};
    Rect uv{};
    Color tint{};
    std::string texture_asset;
    int layer = 0;
};

struct UITextCommand {
    Vec2 position{};
    Color color{};
    std::string text;
    std::string font_asset;
    float pixel_size = 16.0f;
    int layer = 0;
};

struct UIRenderStats {
    std::size_t rect_count = 0;
    std::size_t sprite_count = 0;
    std::size_t text_count = 0;
    std::size_t draw_calls = 0;
};

class UIRenderer {
public:
    void begin_frame();
    void submit_rect(UIRectCommand cmd);
    void submit_sprite(UISpriteCommand cmd);
    void submit_text(UITextCommand cmd);
    void submit_text(Vec2 pos, std::string text, const UIFont& font, Color color = {}, int layer = 0);
    void end_frame();

    [[nodiscard]] const UIRenderStats& stats() const noexcept { return _stats; }
    [[nodiscard]] const std::vector<UIRectCommand>& rects() const noexcept { return _rects; }
    [[nodiscard]] const std::vector<UISpriteCommand>& sprites() const noexcept { return _sprites; }
    [[nodiscard]] const std::vector<UITextCommand>& texts() const noexcept { return _texts; }

private:
    std::vector<UIRectCommand> _rects;
    std::vector<UISpriteCommand> _sprites;
    std::vector<UITextCommand> _texts;
    UIRenderStats _stats{};
};

} // namespace VoxelBlock::UI

