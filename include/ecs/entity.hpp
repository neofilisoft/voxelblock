#pragma once

#include <cstdint>

namespace VoxelBlock::ECS {

using EntityIndex = std::uint32_t;
using EntityGeneration = std::uint32_t;

struct Entity {
    EntityIndex index = 0;
    EntityGeneration generation = 0;

    constexpr bool valid() const noexcept { return generation != 0; }
    constexpr explicit operator bool() const noexcept { return valid(); }
    friend constexpr bool operator==(Entity a, Entity b) noexcept = default;
};

} // namespace VoxelBlock::ECS

