-- RPG-style event hooks (placeholder API examples for modders)

local RPGEvents = {}

function RPGEvents.on_player_enter_town(ctx)
  if not ctx then return end
  if ctx.log then
    ctx.log("[rpg_events] player entered town")
  end
  if ctx.set_music then
    ctx.set_music("audio/town_theme.wav")
  end
end

function RPGEvents.on_npc_interact(ctx, npc_id)
  if not ctx then return end
  local message = "Hello, traveler."
  if npc_id == "innkeeper" then
    message = "Need a room? The night is dangerous."
  elseif npc_id == "blacksmith" then
    message = "Bring ore and I'll forge something useful."
  end

  if ctx.show_dialog then
    ctx.show_dialog({
      speaker = npc_id or "npc",
      text = message
    })
  end
end

function RPGEvents.on_quest_complete(ctx, quest_id)
  if not ctx or not quest_id then return end
  if ctx.give_item then
    ctx.give_item("gold_coin", 50)
  end
  if ctx.log then
    ctx.log("[rpg_events] quest complete: " .. tostring(quest_id))
  end
end

return RPGEvents

