-- VAICOM PRO server-side script
-- Optimized by community review
-- www.vaicompro.com

package.path  = package.path .. ";.\\LuaSocket\\?.lua;"
package.cpath = package.cpath .. ";.\\LuaSocket\\?.dll;"

local socket = require("socket")

vaicom = {}

vaicom.debug = false -- set to true for debug logs

local function log(msg)
    if vaicom.debug then
        env.info("[VAICOM] " .. msg)
    end
end

-- Utility to create and configure a UDP socket
local function create_udp_socket(address, port, timeout, is_server)
    local sock = socket.try(socket.udp())
    if is_server then
        socket.try(sock:setsockname(address, port))
    else
        socket.try(sock:setpeername(address, port))
    end
    socket.try(sock:settimeout(timeout))
    return sock
end

-- Configuration
vaicom.config = {
    sendtoradio = {
        address = "127.0.0.1",
        port    = 33334,
        timeout = 0,
    },
    receivefromclient = {
        address = "*",
        port    = 33491,
        timeout = 0,
    },
    sendtoclient = {
        address = "127.0.0.1",
        port    = 33492,
        timeout = 0,
    },
    beaconclose = "missiondata.update.beacon.unlock",
}

-- Core insert handlers
vaicom.insert = {}

function vaicom.insert:Start()
    log("Initializing sockets...")
    vaicom.sendtoradio       = create_udp_socket(
        vaicom.config.sendtoradio.address,
        vaicom.config.sendtoradio.port,
        vaicom.config.sendtoradio.timeout,
        false
    )
    vaicom.receivefromclient = create_udp_socket(
        vaicom.config.receivefromclient.address,
        vaicom.config.receivefromclient.port,
        vaicom.config.receivefromclient.timeout,
        true
    )
    vaicom.sendtoclient      = create_udp_socket(
        vaicom.config.sendtoclient.address,
        vaicom.config.sendtoclient.port,
        vaicom.config.sendtoclient.timeout,
        false
    )
end

function vaicom.insert:BeforeNextFrame()
    local data = vaicom.receivefromclient:receive()
    if data then
        local ok, err = vaicom.sendtoradio:send(data)
        if not ok then
            log("Failed to send data to radio: " .. tostring(err))
        end
    end
end

function vaicom.insert:AfterNextFrame()
    -- Reserved for future use or flushing logic
end

function vaicom.insert:Stop()
    log("Stopping VAICOM sockets...")

    if vaicom.sendtoclient then
        vaicom.sendtoclient:send(vaicom.config.beaconclose)
    end

    for _, sock in pairs({
        "sendtoradio",
        "receivefromclient",
        "sendtoclient"
    }) do
        if vaicom[sock] then
            socket.try(vaicom[sock]:close())
            vaicom[sock] = nil
        end
    end
end

-- DCS Export Hooks (chained)
local OldLuaExportStart                = LuaExportStart
LuaExportStart = function()
    vaicom.insert:Start()
    if OldLuaExportStart then OldLuaExportStart() end
end

local OldLuaExportBeforeNextFrame     = LuaExportBeforeNextFrame
LuaExportBeforeNextFrame = function()
    vaicom.insert:BeforeNextFrame()
    if OldLuaExportBeforeNextFrame then OldLuaExportBeforeNextFrame() end
end

local OldLuaExportAfterNextFrame      = LuaExportAfterNextFrame
LuaExportAfterNextFrame = function()
    vaicom.insert:AfterNextFrame()
    if OldLuaExportAfterNextFrame then OldLuaExportAfterNextFrame() end
end

local OldLuaExportStop                = LuaExportStop
LuaExportStop = function()
    vaicom.insert:Stop()
    if OldLuaExportStop then OldLuaExportStop() end
end
