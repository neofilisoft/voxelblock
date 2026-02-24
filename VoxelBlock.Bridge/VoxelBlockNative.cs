// VoxelBlock.Bridge — VoxelBlockNative.cs
// P/Invoke declarations that mirror voxelblock_capi.h exactly.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace VoxelBlock.Bridge
{
    public static class Native
    {
        private const string DLL = "voxelblock";

        // Lifecycle
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern long vb_engine_create();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_engine_destroy(long handle);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_engine_init_headless(long handle, int vw, int vh);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_engine_init_windowed(long handle, int w, int h,
            [MarshalAs(UnmanagedType.LPStr)] string title);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_engine_tick(long handle, float dt);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_engine_running(long handle);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_engine_quit(long handle);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint vb_engine_scene_texture(long handle);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_engine_resize_viewport(long handle, int w, int h);

        // World
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint vb_world_get_seed(long handle);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vb_world_chunk_count(long handle);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_world_place_voxel(long handle, int x, int y, int z,
            [MarshalAs(UnmanagedType.LPStr)] string blockType);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_world_destroy_voxel(long handle, int x, int y, int z);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_world_get_voxel(long handle, int x, int y, int z,
            StringBuilder outBlock, int bufSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_world_save(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_world_load(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string name);

        // Block Registry
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_block_register(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string name,
            byte r, byte g, byte b, float hardness);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vb_block_count(long handle);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_block_get_name(long handle, int index,
            StringBuilder nameBuf, int bufSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_block_get_color(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string name,
            out byte r, out byte g, out byte b);

        // Inventory
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_inv_add_item(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string blockType, int count);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_inv_remove_item(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string blockType, int count);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vb_inv_item_count(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string blockType);

        // Crafting

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_craft_can_craft(long handle, IntPtr pattern9);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_craft_do_craft(long handle, IntPtr pattern9,
            StringBuilder resultBlock, int bufSize, out int resultCount);

        // Lua Scripting
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vb_lua_load_mods(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string dir);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool vb_lua_exec(long handle,
            [MarshalAs(UnmanagedType.LPStr)] string code,
            StringBuilder errorBuf, int bufSize);

        // Events
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EventCallbackDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string eventName,
            [MarshalAs(UnmanagedType.LPStr)] string payloadJson);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_events_subscribe(long handle, EventCallbackDelegate cb);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_events_unsubscribe(long handle, EventCallbackDelegate cb);

        // Stats
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vb_stats_get(long handle,
            out int chunksDrawn, out int drawCalls,
            out int triangles,   out float frameMs);
    }
}
