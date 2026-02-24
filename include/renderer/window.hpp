#pragma once
/**
 * VoxelBlock — Window & OpenGL Context
 * Phase 2: GLFW + OpenGL 4.1 core profile
 * Abstracted so Phase 3 editor (C#/Avalonia) can host its own window
 * and only call the C-API for world/render data.
 */

#include <string>
#include <functional>
#include <cstdint>

// Forward-declare to avoid pulling GLFW into every header
struct GLFWwindow;

namespace VoxelBlock {

struct WindowConfig {
    int         width        = 1280;
    int         height       = 720;
    std::string title        = "VoxelBlock Engine";
    bool        fullscreen   = false;
    bool        vsync        = true;
    int         msaa_samples = 4;
    int         gl_major     = 4;
    int         gl_minor     = 1;    // 4.1 = lowest common (macOS)
};

class Window {
public:
    explicit Window(const WindowConfig& cfg = {});
    ~Window();

    // Non-copyable
    Window(const Window&) = delete;
    Window& operator=(const Window&) = delete;

    bool should_close() const;
    void poll_events();
    void swap_buffers();

    int   width()  const { return _width; }
    int   height() const { return _height; }
    float aspect() const { return (float)_width / (float)_height; }

    // Input state
    bool  key_pressed(int glfw_key)        const;
    bool  key_down(int glfw_key)           const;
    bool  mouse_button_pressed(int button) const;
    float mouse_x() const { return _mouse_x; }
    float mouse_y() const { return _mouse_y; }
    float mouse_dx() const { return _mouse_dx; }
    float mouse_dy() const { return _mouse_dy; }
    float scroll_y() const { return _scroll_y; }

    void set_cursor_locked(bool locked);
    bool cursor_locked() const { return _cursor_locked; }

    // Callbacks (set by game/editor)
    std::function<void(int key, int action)>    on_key;
    std::function<void(int btn, int action)>    on_mouse_button;
    std::function<void(double x, double y)>     on_mouse_move;
    std::function<void(double xoff, double yoff)> on_scroll;
    std::function<void(int w, int h)>           on_resize;

    GLFWwindow* native() const { return _window; }

private:
    GLFWwindow* _window = nullptr;
    int   _width, _height;
    float _mouse_x = 0, _mouse_y = 0;
    float _mouse_dx = 0, _mouse_dy = 0;
    float _scroll_y = 0;
    bool  _cursor_locked = false;

    // Static GLFW callbacks
    static void _glfw_key_cb(GLFWwindow* w, int key, int scan, int action, int mods);
    static void _glfw_mouse_btn_cb(GLFWwindow* w, int btn, int action, int mods);
    static void _glfw_cursor_cb(GLFWwindow* w, double x, double y);
    static void _glfw_scroll_cb(GLFWwindow* w, double xo, double yo);
    static void _glfw_resize_cb(GLFWwindow* w, int width, int height);
    static void _glfw_error_cb(int code, const char* msg);
};

} // namespace VoxelBlock
