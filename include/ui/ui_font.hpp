#pragma once

#include "ui/ui_types.hpp"

#include <string>
#include <string_view>

namespace VoxelBlock::UI {

struct TextMeasureResult {
    float width = 0.0f;
    float height = 0.0f;
    int line_count = 1;
};

class UIFont {
public:
    UIFont() = default;
    explicit UIFont(std::string asset_id, float pixel_size = 16.0f)
        : _asset_id(std::move(asset_id)), _pixel_size(pixel_size)
    {
    }

    void set_asset(std::string asset_id) { _asset_id = std::move(asset_id); }
    void set_pixel_size(float px) noexcept { _pixel_size = px > 1.0f ? px : 1.0f; }
    void set_line_height(float px) noexcept { _line_height = px > 1.0f ? px : 1.0f; }

    [[nodiscard]] const std::string& asset_id() const noexcept { return _asset_id; }
    [[nodiscard]] float pixel_size() const noexcept { return _pixel_size; }
    [[nodiscard]] float line_height() const noexcept { return _line_height; }

    [[nodiscard]] TextMeasureResult measure_text(std::string_view text) const noexcept;

private:
    std::string _asset_id = "default";
    float _pixel_size = 16.0f;
    float _line_height = 18.0f;
};

} // namespace VoxelBlock::UI

