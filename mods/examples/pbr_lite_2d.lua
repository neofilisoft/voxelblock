-- PBR-lite 2D material examples (normal/emissive sprite settings)

local PBRLite2D = {}

PBRLite2D.materials = {
  neon_sign = {
    sprite = "textures/neon_sign.png",
    normal = "textures/neon_sign_n.png",
    emissive = "textures/neon_sign_e.png",
    emissive_intensity = 2.4
  },
  lamp_post = {
    sprite = "textures/lamp_post.png",
    normal = "textures/lamp_post_n.png",
    emissive = "textures/lamp_post_e.png",
    emissive_intensity = 1.1
  }
}

function PBRLite2D.register_all(ctx)
  if not ctx or not ctx.register_lit_sprite_material then return 0 end
  local count = 0
  for id, mat in pairs(PBRLite2D.materials) do
    ctx.register_lit_sprite_material(id, mat)
    count = count + 1
  end
  return count
end

return PBRLite2D

