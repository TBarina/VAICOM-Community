-- VAICOM command_defs.lua (Optimized)

-- Base command ID for the custom device
local base_command_id = 2999

-- Generator function returns a unique ID each time it's called
local function make_command_counter(start)
    local id = start or 2999
    return function()
        id = id + 1
        return id
    end
end

-- Local counter function used for this device's command IDs
local next_command_id = make_command_counter(base_command_id)

-- Define device commands
device_commands = {
    Master_Reset    = next_command_id(),

    Select_Page_01  = next_command_id(),
    Select_Page_02  = next_command_id(),
    Select_Page_03  = next_command_id(),
    Select_Page_04  = next_command_id(),
    Select_Page_05  = next_command_id(),
    Select_Page_06  = next_command_id(),
    Select_Page_07  = next_command_id(),
    Select_Page_08  = next_command_id(),
}
