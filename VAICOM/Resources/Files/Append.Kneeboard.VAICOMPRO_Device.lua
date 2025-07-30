-- VAICOMPRO_Device.lua (Optimized)

-- Assumes prerequisite modules: socket, JSON, etc.

-- === Config, Globals ===
local sender, receiver
local dev_timer = 0
local update_time_step = 0.1
local master_opacity = 1.0

-- === Utilities ===

function DecodeMessage(rawdata)
    local decodeerror = false
    function JSON:onDecodeError(message, text, location, etc)
        decodeerror = true
    end
    local msg = JSON:decode(rawdata)
    if decodeerror or type(msg) ~= "table" then
        return nil
    end
    return msg
end

function wraptext(cat, str, keeplines)
    local linelen = (cat == "LOG" or cat == "NOTES") and 55 or 40
    local text = keeplines and str or str:gsub("\n", " ")
    local wrapped = ""
    for i = 0, math.floor(#text / linelen) do
        wrapped = wrapped .. string.sub(text, i * linelen + 1, (i + 1) * linelen) .. "\n"
    end
    return wrapped
end

function getmodeltime()
    local t = get_model_time()
    if t <= 0 then return "00:00:00" end
    local h = math.floor(t / 3600)
    local m = math.floor((t % 3600) / 60)
    local s = math.floor(t % 60)
    return string.format("%02d:%02d:%02d", h, m, s)
end

-- === Socket Management ===

local function create_socket(mode, address, port, timeout)
    local sock = socket.try(socket.udp())
    if mode == "send" then
        socket.try(sock:setpeername(address, port))
    else
        socket.try(sock:setsockname(address, port))
    end
    socket.try(sock:settimeout(timeout))
    return sock
end

local function start_network()
    sender = create_socket("send", config.sendaddress, config.sendport, config.sendtimeout)
    receiver = create_socket("receive", config.receiveaddress, config.receiveport, config.receivetimeout)
end

local function stop_network()
    for _, sock in pairs({ "sender", "receiver" }) do
        if _G[sock] then
            socket.try(_G[sock]:close())
            _G[sock] = nil
        end
    end
end

-- === Opacity ===

function update_opacity(msg)
    if LockOn_Options.screen.oculus_rift then
        master_opacity = 1.0
    else
        master_opacity = msg.opacity or master_opacity
    end
    Master_Opacity:set(master_opacity)
end

function get_page_opacity(page)
    return page_active[page] and master_opacity or 0
end

function update_Pages_Opacity()
    for page in pairs(logcats) do
        _G["Page_" .. page]:set(get_page_opacity(page))
    end
end

function update_Layers()
    local elapsed = get_model_time()
    local fade = master_opacity * (0.1 + 0.7 * (elapsed / (6 * 3600)))
    Smudge_Opacity:set(math.min(fade, 0.8))
end

-- === Headers ===

function update_headers()
    local page_label = get_page_active()
    Header_TopLeft:set(string.sub("Notepad | " .. page_label .. " | ", 1, 45))
    Header_TopMid:set(serverdata["dictmode"] == 0 and "DICT OFF" or "[DICT ON]")
    Header_TopRight:set("| VAICOM PRO | " .. getmodeltime())

    local ac     = get_aircraft_type() or ""
    local flight = serverdata["groupcount"] > 0 and serverdata["groupcount"] or 1
    local calls  = serverdata["playercallsign"] or ""
    local title  = serverdata["missiontitle"] or ""
    Header_BottomLeft:set(string.sub(ac .. " | 1/" .. flight .. " | " .. calls .. " | " .. title, 1, 45))

    local mp = serverdata["multiplayer"] and "MP" or "SP"
    local ver = string.sub(tostring(_ED_VERSION), 1, 9) or ""
    Header_BottomRight:set("| " .. mp .. " | " .. ver)
end

-- === Update Content ===

function update_contents()
    update_Pages_Opacity()
    update_headers()
end

-- === Command Dispatch ===

local command_handlers = {
    [3001] = function() if kneeboard then kneeboard:performClickableAction(3006, 0) end end,
    [3002] = function() if kneeboard then kneeboard:performClickableAction(3005, value) end end,
    [3003] = function() if kneeboard then kneeboard:performClickableAction(3004, 1) end end,
    [3004] = function() if kneeboard then kneeboard:performClickableAction(3002, 1) end end,
    [3005] = function() if kneeboard then kneeboard:performClickableAction(3001, 1) end end,
    [3006] = function() serverdata["dictmode"] = value; Dictate_Status:set(value) end,
    [3010] = function() set_page_active("ALL") end,
    [3011] = function() set_page_active("LOG") end,
    [3012] = function() set_page_active("AWACS") end,
    [3013] = function() set_page_active("JTAC") end,
    [3014] = function() set_page_active("ATC") end,
    [3015] = function() set_page_active("TANKER") end,
    [3016] = function() set_page_active("FLIGHT") end,
    [3017] = function() set_page_active("AOCS") end,
    [3018] = function() set_page_active("REF") end,
    [3019] = function() set_page_active("NOTES") end,
    [3020] = function() update_opacity({ opacity = value }) end,
}

function SetCommand(command, value)
    local handler = command_handlers[command]
    if handler then handler(value) end
    update_contents()
end

-- === Update Cycle ===

function update()
    dev_timer = dev_timer + update_time_step

    local data = receiver and receiver:receive()
    if data then
        local msg = DecodeMessage(data)
        if msg then
            process_message(msg)
            update_contents()
        end
    end

    update_Layers()
    update_headers()
end
