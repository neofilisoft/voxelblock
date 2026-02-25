#pragma once

#include "ui/ui_types.hpp"

#include <cstdint>
#include <string>
#include <vector>

namespace VoxelBlock::UI {

enum class PointerButton : std::uint8_t {
    Left = 0,
    Right = 1,
    Middle = 2,
};

enum class UIInputEventType : std::uint8_t {
    PointerMove,
    PointerDown,
    PointerUp,
    MouseWheel,
    KeyDown,
    KeyUp,
    TextInput,
};

struct UIInputEvent {
    UIInputEventType type = UIInputEventType::PointerMove;
    Vec2 pointer{};
    PointerButton button = PointerButton::Left;
    float wheel_delta = 0.0f;
    std::uint32_t key_code = 0;
    std::string text_utf8;
};

class UIInputState {
public:
    void begin_frame();

    void push_pointer_move(float x, float y);
    void push_pointer_button(PointerButton button, bool down, float x, float y);
    void push_wheel(float delta, float x, float y);
    void push_key(std::uint32_t key_code, bool down);
    void push_text(std::string utf8_text);

    [[nodiscard]] bool is_button_down(PointerButton button) const noexcept;
    [[nodiscard]] Vec2 pointer_position() const noexcept { return _pointer; }
    [[nodiscard]] const std::vector<UIInputEvent>& events() const noexcept { return _events; }

private:
    Vec2 _pointer{};
    std::uint8_t _button_mask = 0;
    std::vector<UIInputEvent> _events;
};

} // namespace VoxelBlock::UI

