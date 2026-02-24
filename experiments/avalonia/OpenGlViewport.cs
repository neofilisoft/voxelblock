// VoxelBlock.Editor â€” OpenGlViewport.cs
// Renders the engine's scene texture (from render-to-texture FBO)
// into an Avalonia control using OpenGL interop.

using System;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using static Avalonia.OpenGL.GlConsts;

namespace VoxelBlock.Editor
{
    /// <summary>
    /// Avalonia OpenGL control that blits the engine's scene texture
    /// into the editor viewport. The engine renders to its FBO each
    /// tick; this control reads that texture and draws a full-screen quad.
    /// </summary>
    public class OpenGlViewport : OpenGlControlBase
    {
        // â”€â”€ Avalonia StyledProperty â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public static readonly StyledProperty<long> EngineHandleProperty =
            AvaloniaProperty.Register<OpenGlViewport, long>(nameof(EngineHandle));

        public long EngineHandle
        {
            get => GetValue(EngineHandleProperty);
            set => SetValue(EngineHandleProperty, value);
        }

        // â”€â”€ GL resources â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private int _quadVao = 0, _quadVbo = 0;
        private int _blitProgram = 0;

        private static readonly string BlitVert = @"
#version 330 core
layout(location=0) in vec2 aPos;
layout(location=1) in vec2 aUV;
out vec2 vUV;
void main(){ gl_Position = vec4(aPos, 0.0, 1.0); vUV = aUV; }
";
        private static readonly string BlitFrag = @"
#version 330 core
in vec2 vUV;
uniform sampler2D uTex;
out vec4 FragColor;
void main(){ FragColor = texture(uTex, vUV); }
";

        protected override void OnOpenGlInit(GlInterface gl)
        {
            // Full-screen quad: pos(2) + uv(2)
            float[] verts =
            {
               -1f,-1f, 0f,0f,
                1f,-1f, 1f,0f,
                1f, 1f, 1f,1f,
               -1f,-1f, 0f,0f,
                1f, 1f, 1f,1f,
               -1f, 1f, 0f,1f,
            };

            // NOTE: Full VAO/VBO/shader setup via GlInterface â€” abbreviated here.
            // In production build, call gl.GenVertexArrays / gl.BindVertexArray etc.
            // The Avalonia GlInterface wraps all OpenGL calls safely.

            // Shader compile
            int vs = _compileShader(gl, GL_VERTEX_SHADER,   BlitVert);
            int fs = _compileShader(gl, GL_FRAGMENT_SHADER, BlitFrag);
            _blitProgram = gl.CreateProgram();
            gl.AttachShader(_blitProgram, vs);
            gl.AttachShader(_blitProgram, fs);
            gl.LinkProgram(_blitProgram);
            gl.DeleteShader(vs);
            gl.DeleteShader(fs);
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            if (_blitProgram != 0) gl.DeleteProgram(_blitProgram);
        }

        protected override void OnOpenGlRender(GlInterface gl, int framebuffer)
        {
            // Get scene texture from engine
            uint texId = Bridge.Native.vb_engine_scene_texture(EngineHandle);
            if (texId == 0) { _clearBlack(gl); return; }

            gl.Viewport(0, 0, (int)Bounds.Width, (int)Bounds.Height);
            gl.ClearColor(0, 0, 0, 1);
            gl.Clear(GL_COLOR_BUFFER_BIT);

            gl.UseProgram(_blitProgram);
            gl.BindTexture(GL_TEXTURE_2D, (int)texId);

            // Draw the full-screen quad (6 verts from bound VAO)
            // gl.DrawArrays(GL_TRIANGLES, 0, 6); â€” needs VAO binding
            // Full VAO path omitted for clarity â€” Phase 4 will wire fully

            Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Background);
        }

        private void _clearBlack(GlInterface gl)
        {
            gl.ClearColor(0.1f, 0.1f, 0.1f, 1f);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }

        private int _compileShader(GlInterface gl, int type, string src)
        {
            int s = gl.CreateShader(type);
            gl.ShaderSource(s, src);
            gl.CompileShader(s);
            return s;
        }
    }
}
