#pragma once

#include "ecs/entity.hpp"

#include <algorithm>
#include <cstddef>
#include <functional>
#include <memory>
#include <stdexcept>
#include <type_traits>
#include <typeindex>
#include <unordered_map>
#include <utility>
#include <vector>

namespace VoxelBlock::ECS {

class Registry {
public:
    Registry() = default;
    Registry(const Registry&) = delete;
    Registry& operator=(const Registry&) = delete;

    Entity create()
    {
        EntityIndex index = 0;
        if (!_free_list.empty()) {
            index = _free_list.back();
            _free_list.pop_back();
            _alive[index] = true;
        } else {
            index = static_cast<EntityIndex>(_generations.size());
            _generations.push_back(1); // generation starts at 1 (0 = invalid)
            _alive.push_back(true);
        }

        ++_alive_count;
        return Entity{index, _generations[index]};
    }

    bool destroy(Entity entity)
    {
        if (!is_alive(entity)) return false;

        for (auto& [_, storage] : _storages) {
            storage->erase(entity.index);
        }

        _alive[entity.index] = false;
        ++_generations[entity.index]; // invalidate stale handles
        if (_generations[entity.index] == 0) {
            _generations[entity.index] = 1; // avoid wrapping into invalid generation
        }
        _free_list.push_back(entity.index);
        --_alive_count;
        return true;
    }

    void clear()
    {
        for (auto& [_, storage] : _storages) {
            storage->clear();
        }

        _generations.clear();
        _alive.clear();
        _free_list.clear();
        _alive_count = 0;
    }

    [[nodiscard]] bool is_alive(Entity entity) const noexcept
    {
        if (!entity.valid()) return false;
        if (entity.index >= _generations.size()) return false;
        return _alive[entity.index] && _generations[entity.index] == entity.generation;
    }

    [[nodiscard]] std::size_t alive_count() const noexcept { return _alive_count; }

    template <typename T, typename... Args>
    T& emplace_or_replace(Entity entity, Args&&... args)
    {
        require_alive(entity);
        auto& store = storage<T>();
        auto [it, inserted] = store.items.try_emplace(
            entity.index, T{std::forward<Args>(args)...}
        );
        if (!inserted) {
            it->second = T{std::forward<Args>(args)...};
        }
        return it->second;
    }

    template <typename T>
    bool has(Entity entity) const
    {
        if (!is_alive(entity)) return false;
        const auto* store = find_storage<T>();
        return store && store->items.find(entity.index) != store->items.end();
    }

    template <typename T>
    T* try_get(Entity entity)
    {
        if (!is_alive(entity)) return nullptr;
        auto* store = find_storage<T>();
        if (!store) return nullptr;
        auto it = store->items.find(entity.index);
        return it == store->items.end() ? nullptr : &it->second;
    }

    template <typename T>
    const T* try_get(Entity entity) const
    {
        if (!is_alive(entity)) return nullptr;
        const auto* store = find_storage<T>();
        if (!store) return nullptr;
        auto it = store->items.find(entity.index);
        return it == store->items.end() ? nullptr : &it->second;
    }

    template <typename T>
    T& get(Entity entity)
    {
        auto* ptr = try_get<T>(entity);
        if (!ptr) {
            throw std::runtime_error("ECS::Registry::get() missing component");
        }
        return *ptr;
    }

    template <typename T>
    const T& get(Entity entity) const
    {
        auto* ptr = try_get<T>(entity);
        if (!ptr) {
            throw std::runtime_error("ECS::Registry::get() missing component");
        }
        return *ptr;
    }

    template <typename T>
    bool remove(Entity entity)
    {
        if (!is_alive(entity)) return false;
        auto* store = find_storage<T>();
        if (!store) return false;
        return store->items.erase(entity.index) > 0;
    }

    // Iterates alive entities that have all listed components.
    // Structural changes (create/destroy entities) inside callback are not recommended.
    template <typename... Components, typename Fn>
    void for_each(Fn&& fn)
    {
        static_assert(sizeof...(Components) > 0, "for_each requires at least one component type");
        for_each_impl<Components...>(*this, std::forward<Fn>(fn));
    }

    template <typename... Components, typename Fn>
    void for_each(Fn&& fn) const
    {
        static_assert(sizeof...(Components) > 0, "for_each requires at least one component type");
        for_each_impl<Components...>(*this, std::forward<Fn>(fn));
    }

private:
    struct IStorage {
        virtual ~IStorage() = default;
        virtual void erase(EntityIndex index) = 0;
        virtual void clear() = 0;
    };

    template <typename T>
    struct Storage final : IStorage {
        std::unordered_map<EntityIndex, T> items;

        void erase(EntityIndex index) override { items.erase(index); }
        void clear() override { items.clear(); }
    };

    std::vector<EntityGeneration> _generations;
    std::vector<bool> _alive;
    std::vector<EntityIndex> _free_list;
    std::size_t _alive_count = 0;
    std::unordered_map<std::type_index, std::unique_ptr<IStorage>> _storages;

    void require_alive(Entity entity) const
    {
        if (!is_alive(entity)) {
            throw std::runtime_error("ECS::Registry: invalid or dead entity handle");
        }
    }

    template <typename T>
    Storage<T>& storage()
    {
        auto key = std::type_index(typeid(T));
        auto it = _storages.find(key);
        if (it == _storages.end()) {
            auto ptr = std::make_unique<Storage<T>>();
            auto* raw = ptr.get();
            _storages.emplace(key, std::move(ptr));
            return *raw;
        }
        return *static_cast<Storage<T>*>(it->second.get());
    }

    template <typename T>
    Storage<T>* find_storage()
    {
        auto it = _storages.find(std::type_index(typeid(T)));
        if (it == _storages.end()) return nullptr;
        return static_cast<Storage<T>*>(it->second.get());
    }

    template <typename T>
    const Storage<T>* find_storage() const
    {
        auto it = _storages.find(std::type_index(typeid(T)));
        if (it == _storages.end()) return nullptr;
        return static_cast<const Storage<T>*>(it->second.get());
    }

    template <typename... Components, typename Self, typename Fn>
    static void for_each_impl(Self& self, Fn&& fn)
    {
        for (EntityIndex idx = 0; idx < self._generations.size(); ++idx) {
            if (!self._alive[idx]) continue;
            Entity e{idx, self._generations[idx]};
            if (((!self.template has<Components>(e)) || ...)) {
                continue;
            }
            std::invoke(std::forward<Fn>(fn), e, self.template get<Components>(e)...);
        }
    }
};

} // namespace VoxelBlock::ECS
