-- VAICOM PRO Device Registration
-- Safe device declaration for kneeboard or plugin context

-- Reserve a safe device ID slot
local VAICOM_id = 255

-- Ensure global tables exist
if not creators then creators = {} end
if not devices then devices = {} end

-- Register the VAICOMPRO Lua device
creators[VAICOM_id] = {
    "avLuaDevice",
    LockOn_Options.common_script_path .. "VAICOMPRO/device/VAICOMPRO_Device.lua"
}

-- Assign to devices table
devices["VAICOMPRO"] = VAICOM_id
