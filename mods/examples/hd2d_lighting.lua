-- HD-2D / 2.5D lighting hints for a unified renderer pipeline.
-- This is a mod-side data provider example, not a renderer implementation.

local Lighting = {}

Lighting.presets = {
  town_day = {
    ambient = { 0.78, 0.74, 0.70 },
    bloom = 0.15,
    grade = "warm_day",
    sprite_rim = 0.10
  },
  town_night = {
    ambient = { 0.15, 0.18, 0.28 },
    bloom = 0.45,
    grade = "cool_night",
    sprite_rim = 0.30
  },
  cave = {
    ambient = { 0.04, 0.05, 0.06 },
    bloom = 0.25,
    grade = "desaturated",
    sprite_rim = 0.08
  }
}

function Lighting.apply(ctx, preset_name)
  if not ctx or not ctx.set_post_fx then return false end
  local preset = Lighting.presets[preset_name]
  if not preset then return false end

  ctx.set_post_fx("bloom", preset.bloom)
  if ctx.set_color_grade then
    ctx.set_color_grade(preset.grade)
  end
  if ctx.set_ambient_color then
    ctx.set_ambient_color(preset.ambient[1], preset.ambient[2], preset.ambient[3])
  end
  return true
end

return Lighting

