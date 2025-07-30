-- VAICOM PRO server-side script (Optimized)
-- VAICOMPRO_Common.lua

-- === Category Definitions ===

logcats = {}

local predefined_logcats = {
    "ALL", "LOG", "AOCS", "JTAC", "TANKER",
    "AWACS", "ATC", "FLIGHT", "REF", "NOTES"
}

init_logcats = function()
    for _, name in ipairs(predefined_logcats) do
        logcats[name] = name
    end
end

-- === Generalized Table Initializer ===

local function init_table_keys(target, keys, default)
    for k in pairs(keys) do
        target[k] = default
    end
end

-- === Message Log ===

messagelog = {}

init_messagelog_all = function()
    init_table_keys(messagelog, logcats, "")
end

init_messagelog = function(str)
    messagelog[str] = ""
end

set_messagelog = function(cat, content)
    messagelog[cat] = content or ""
end

get_messagelog = function(cat)
    return messagelog[cat] or ""
end

-- === Alias Data ===

aliasdata = {}

init_aliasdata_all = function()
    for i = 1, 4 do
        aliasdata[i] = {}
        init_table_keys(aliasdata[i], logcats, "")
    end
end

init_aliasdata = function(str)
    for i = 1, 4 do
        if not aliasdata[i] then aliasdata[i] = {} end
        aliasdata[i][str] = ""
    end
end

local function should_include(n, p)
    return (n > (p - 1) * 12 and n <= p * 12 and n <= 46)
end

local function format_alias(i, j)
    if not j or #j == 0 then return i end
    i = i .. " "
    for k = 1, #j do
        local word = j[k]
        if #i + #word < 500 then
            i = i .. (k == 1 and word or " / " .. word)
        end
    end
    return i .. " |
"
end

set_aliasdata = function(cat, content, chunk)
    if chunk == 0 then
        init_aliasdata(cat)
    end

    local n = 2 * 12 * chunk
    local start_p = (chunk == 1 and 3) or 1
    local end_p = (chunk == 1 and 4) or 2

    for i, j in pairs(content) do
        n = n + 1
        for p = start_p, end_p do
            if should_include(n, p) then
                local line = aliasdata[p][cat] or ""
                aliasdata[p][cat] = format_alias(line .. i, j)
            end
        end
    end
end

get_aliasdata = function(cat)
    local append = ""
    for i = 1, 4 do
        append = append .. (aliasdata[i][cat] or "")
    end
    return append
end

-- === Units Data ===

unitsdata = {}

init_unitsdata_all = function()
    init_table_keys(unitsdata, logcats, "N/A")
end

init_unitsdata = function(str)
    unitsdata[str] = "N/A"
end

set_unitsdata = function(cat, content)
    unitsdata[cat] = "ATO DCS" .. serverdata["ato"] .. " / " .. cat .. "\n"
    if #content == 0 then
        unitsdata[cat] = unitsdata[cat] .. "N/A"
    else
        for i = 1, math.min(4, #content) do
            unitsdata[cat] = unitsdata[cat] .. "#" .. i .. "/" .. tostring(#content) .. " " .. content[i] .. "\n"
        end
    end
end

get_unitsdata = function(cat)
    return unitsdata[cat] or ""
end

-- === Units Details ===

unitsdetails = {}

init_unitsdetails_all = function()
    init_table_keys(unitsdetails, logcats, "")
end

init_unitsdetails = function(str)
    unitsdetails[str] = ""
end

set_unitsdetails = function(cat, content)
    unitsdetails[cat] = ""
    for i = 1, math.min(4, #content) do
        unitsdetails[cat] = content[i]
        messagelog[cat] = mergelog(messagelog[cat], content[i])
    end
end

get_unitsdetails = function(cat)
    return unitsdetails[cat] or ""
end

-- === Log Categories + Keywords ===

logcategories = {}

init_logcategories = function()
    init_table_keys(logcategories, logcats, true)
end

logkeywords = {}

init_logkeywords = function()
    init_table_keys(logkeywords, logcats, "")
    logkeywords["FLIGHT"] = "WINGMAN"
    logkeywords["ALL"] = "ALLIES"
end

-- === Server Data ===

serverdata = {}

init_serverdata = function()
    serverdata = {
        ato             = "",
        theater         = "",
        autoswitch      = false,
        dictmode        = 0,
        dcsversion      = "",
        aircraft        = "",
        groupcount      = 0,
        playerusername  = "",
        playercallsign  = "",
        coalition       = "",
        missiontitle    = "",
        missionbriefing = "",
        missiondetails  = "",
        sortie          = "",
        task            = "",
        country         = "",
        multiplayer     = false,
    }
end

-- === Page State ===

page_active = {}

init_page_active = function()
    init_table_keys(page_active, logcats, false)
end

set_page_active = function(selected)
    for page in pairs(page_active) do
        page_active[page] = (page == selected)
    end
end

get_page_active = function()
    for page, state in pairs(page_active) do
        if state then
            return page == "LOG" and "ATO" or page
        end
    end
end

-- === Utilities ===

getlines = function(text)
    local lines = {}
    local count = 0
    for line in text:gmatch("[^\r\n]+") do
        count = count + 1
        lines[count] = line
    end
    return lines, count
end

mergearrays = function(t1, t2)
    local l = #t1
    for i = 1, #t2 do
        t1[l + i] = t2[i]
    end
    return t1
end

mergelog = function(log1, log2)
    local lines1 = getlines(log1)
    local lines2 = getlines(log2)
    local merged = mergearrays(lines1, lines2)

    local max_lines = 27
    local start_idx = math.max(1, #merged - max_lines + 1)
    local output = ""

    for i = start_idx, #merged do
        output = output .. merged[i] .. "\n"
    end

    return output
end
