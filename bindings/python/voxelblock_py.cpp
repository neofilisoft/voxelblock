/**
 * VoxelBlock — Python Binding (pybind11)
 * Phase 4: exposes Engine, World, Inventory to Python
 *
 * Usage:
 *   import voxelblock as vb
 *   e = vb.Engine(); e.init_headless()
 *   e.world.place_voxel(0,20,0,"stone")
 */

#if __has_include(<pybind11/pybind11.h>)
#include <pybind11/pybind11.h>
#include <pybind11/stl.h>
#include <pybind11/functional.h>
#include "engine.hpp"
#include "core/voxel.hpp"
#include "core/crafting.hpp"
#include "core/event_system.hpp"

namespace py = pybind11;
using namespace VoxelBlock;

PYBIND11_MODULE(voxelblock, m)
{
    m.doc() = "VoxelBlock Engine Python bindings";

    py::class_<Engine>(m, "Engine")
        .def(py::init<>())
        .def("init_headless", [](Engine& e, int w, int h){
            EngineConfig c; c.headless=true; c.viewport_w=w; c.viewport_h=h;
            e = Engine(c); return e.init();
        }, py::arg("width")=1280, py::arg("height")=720)
        .def("init_windowed", [](Engine& e, int w, int h, std::string t){
            EngineConfig c; c.headless=false; c.viewport_w=w; c.viewport_h=h;
            e = Engine(c); return e.init();
        }, py::arg("width")=1280, py::arg("height")=720, py::arg("title")="VoxelBlock")
        .def("tick",    &Engine::tick)
        .def("running", &Engine::running)
        .def("quit",    &Engine::quit)
        .def_property_readonly("world",     [](Engine& e) -> World&     { return e.world(); })
        .def_property_readonly("inventory", [](Engine& e) -> Inventory& { return e.inventory(); })
        .def("load_mods", [](Engine& e, std::string d){ return e.lua().load_mods(d); }, py::arg("dir")="mods")
        .def("exec_lua",  [](Engine& e, std::string c){
            std::string err; bool ok = e.lua().exec(c,err);
            return py::make_tuple(ok, err);
        })
        .def("save", [](Engine& e, std::string n){ return e.save_manager().save(n, e.world(), e.inventory()); })
        .def("load", [](Engine& e, std::string n){ return e.save_manager().load_into_engine(n, e.world(), e.inventory()); });

    py::class_<World>(m, "World")
        .def_property_readonly("seed",        &World::seed)
        .def_property_readonly("chunk_count", &World::loaded_chunk_count)
        .def("place_voxel",   [](World& w, int x,int y,int z,std::string bt){ return w.place_voxel(x,y,z,bt)!=nullptr; })
        .def("destroy_voxel", &World::destroy_voxel)
        .def("get_voxel",     [](World& w, int x,int y,int z) -> py::object {
            auto* v = w.get_voxel(x,y,z); return v ? py::cast(v->block_type) : py::none();
        });

    py::class_<Inventory>(m, "Inventory")
        .def("add_item",   &Inventory::add_item,   py::arg("block_type"), py::arg("count")=1)
        .def("remove_item",&Inventory::remove_item, py::arg("block_type"), py::arg("count")=1)
        .def("has_item",   &Inventory::has_item,   py::arg("block_type"), py::arg("count")=1)
        .def("item_count", &Inventory::item_count)
        .def("items",      [](Inventory& i){ return i.items(); });

    py::class_<BlockDef>(m, "BlockDef")
        .def_readonly("name",     &BlockDef::name)
        .def_readonly("hardness", &BlockDef::hardness)
        .def_readonly("tags",     &BlockDef::tags);

    m.def("register_block", [](std::string n, int r,int g,int b,float h){
        BlockRegistry::register_block(n,{(uint8_t)r,(uint8_t)g,(uint8_t)b},"white_cube",h);
    }, py::arg("name"), py::arg("r"),py::arg("g"),py::arg("b"), py::arg("hardness")=1.f);

    m.def("all_blocks", &BlockRegistry::all_blocks);

    m.def("register_recipe", [](std::vector<std::string> p, std::string r, int c){
        std::array<std::string,9> arr; arr.fill("");
        for (int i=0; i<(int)p.size()&&i<9; ++i) arr[i]=p[i];
        CraftingSystem::register_recipe(arr,r,c);
    }, py::arg("pattern"),py::arg("result"),py::arg("count")=1);

    m.def("craft", [](std::vector<std::string> p, Inventory& inv) -> py::object {
        std::array<std::string,9> arr; arr.fill("");
        for (int i=0;i<(int)p.size()&&i<9;++i) arr[i]=p[i];
        auto res = CraftingSystem::craft(arr,inv);
        return res ? py::make_tuple(res->block_type,res->count) : py::none();
    });

    m.def("on", [](std::string evt, py::function fn){
        EventSystem::on(evt,[fn](const EventData&) mutable {
            py::gil_scoped_acquire a;
            try { fn(); } catch(py::error_already_set& e){ PyErr_Print(); }
        });
    });
}
#else
#include <iostream>
int main(){ std::cout<<"pybind11 not found. pip install pybind11\n"; return 1; }
#endif
