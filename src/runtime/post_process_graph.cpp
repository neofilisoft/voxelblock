#include "runtime/post_process_graph.hpp"

#include <sstream>

namespace VoxelBlock::Runtime {
namespace {

const char* pass_name(PostPassType type)
{
    switch (type) {
    case PostPassType::Bloom: return "Bloom";
    case PostPassType::ToneMap: return "ToneMap";
    case PostPassType::ColorGrading: return "ColorGrading";
    case PostPassType::Vignette: return "Vignette";
    case PostPassType::Sharpen: return "Sharpen";
    case PostPassType::StylizedPixel: return "StylizedPixel";
    }
    return "Unknown";
}

} // namespace

void PostProcessGraph::clear()
{
    _passes.clear();
}

void PostProcessGraph::set_default_tier_a_2d_stack()
{
    _passes.clear();
    _passes.push_back({PostPassType::ColorGrading, true, 1.0f});
    _passes.push_back({PostPassType::Bloom, true, 0.25f});
    _passes.push_back({PostPassType::StylizedPixel, false, 1.0f});
}

void PostProcessGraph::set_default_tier_b_unified_stack()
{
    _passes.clear();
    _passes.push_back({PostPassType::Bloom, true, 0.6f});
    _passes.push_back({PostPassType::ToneMap, true, 1.0f});
    _passes.push_back({PostPassType::ColorGrading, true, 1.0f});
    _passes.push_back({PostPassType::Vignette, false, 0.25f});
    _passes.push_back({PostPassType::Sharpen, false, 0.2f});
}

void PostProcessGraph::add_pass(PostPassDesc pass)
{
    _passes.push_back(pass);
}

std::string PostProcessGraph::debug_summary() const
{
    std::ostringstream os;
    for (std::size_t i = 0; i < _passes.size(); ++i) {
        const auto& p = _passes[i];
        if (i) os << " -> ";
        os << pass_name(p.type) << '(' << (p.enabled ? "on" : "off") << ", " << p.intensity << ')';
    }
    return os.str();
}

} // namespace VoxelBlock::Runtime

