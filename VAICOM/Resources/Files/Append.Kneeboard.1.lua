-- === VAICOM Kneeboard Renderer (Optimized) ===

dofile(LockOn_Options.common_script_path.."KNEEBOARD/indicator/definitions.lua")
dofile(LockOn_Options.common_script_path.."VAICOMPRO/device/VAICOMPRO_Common.lua")

-- 1. Constants & Layout
local SCREEN_ASPECT = GetAspect()

local MARGIN_LEFT   = 0.05
local MARGIN_RIGHT  = 0.90
local COLUMN_WIDTH  = MARGIN_RIGHT - MARGIN_LEFT

local ROW_SPACING   = 0.022
local LOG_START_Y   = 0.12
local ALIAS_START_Y = 0.70
local ROW_COUNT     = 27

-- 2. Fonts & Materials
local function MakeUIFont(alpha)
    return MakeFont({used_DXUnicodeFontData = "font_dejavu_lgc_sans_22"}, {255, 255, 255, alpha or 255})
end

Font_big   = {0.007, 0.007, 0.0, 0.0}
Font_med   = {0.006, 0.006, 0.0, 0.0}
Font_small = {0.005, 0.005, 0.0, 0.0}

-- 3. Draw Helpers

local function texture_box(x, y, w, h)
    return {
        {x, y},
        {x + w, y},
        {x + w, y + h},
        {x, y + h}
    }
end

function add_text(text, x, y, alpha, scale)
    local txt = CreateElement "ceStringPoly"
    txt.value = text
    txt.material = MakeUIFont(alpha or 255)
    txt.stringdefs = scale or Font_med
    txt.init_pos = {(x or 0) - 1, SCREEN_ASPECT - (y or 0)}
    txt.alignment = "LeftTop"
    txt.use_mipfilter = true
    txt.h_clip_relation = h_clip_relations.COMPARE
    txt.level = DEFAULT_LEVEL
    Add(txt)
    return txt
end

function add_picture(picture, x, y, w, h, tx_x, tx_y, tx_w, tx_h, alpha, name, params, controllers)
    local mat = MakeMaterial(picture, {255, 255, 255, alpha or 255})
    local width = w or 2
    local height = h or 2 * SCREEN_ASPECT
    local element = CreateElement "ceTexPoly"
    element.name = name
    element.material = mat
    element.init_pos = {(x or 0) - 1, SCREEN_ASPECT - (y or 0)}
    element.vertices = {
        {0, 0}, {width, 0}, {width, -height}, {0, -height}
    }
    element.indices = {0, 1, 2, 0, 2, 3}
    element.tex_coords = texture_box(tx_x or 0, tx_y or 0, tx_w or 1, tx_h or 1)
    element.h_clip_relation = h_clip_relations.COMPARE
    element.level = DEFAULT_LEVEL
    if params then element.element_params = params end
    if controllers then element.controllers = controllers end
    Add(element)
    return element
end

local function add_param_text(name, x, y, alignment, font)
    local txt = CreateElement "ceStringPoly"
    txt.name = name
    txt.material = MakeUIFont(255)
    txt.stringdefs = font or Font_med
    txt.init_pos = {x, SCREEN_ASPECT - y}
    txt.alignment = alignment or "LeftTop"
    txt.use_mipfilter = true
    txt.h_clip_relation = h_clip_relations.COMPARE
    txt.level = DEFAULT_LEVEL
    txt.element_params = {name}
    txt.controllers = {{"text_using_parameter", 0}}
    Add(txt)
end

local function add_param_block(prefix, row_count, start_y, font)
    for i = 1, row_count do
        local txt = CreateElement "ceStringPoly"
        txt.name = prefix .. i
        txt.material = MakeUIFont(255)
        txt.stringdefs = font or Font_small
        txt.init_pos = {MARGIN_LEFT - 1, SCREEN_ASPECT - (start_y + (i - 1) * ROW_SPACING)}
        txt.alignment = "LeftTop"
        txt.element_params = {prefix .. i}
        txt.controllers = {{"text_using_parameter", 0}}
        txt.use_mipfilter = true
        txt.h_clip_relation = h_clip_relations.COMPARE
        txt.level = DEFAULT_LEVEL
        Add(txt)
    end
end

-- 4. Initialize Data and Draw Elements

init_logcats()

-- Draw headers
add_param_text("Header_TopLeft",     -0.9, 0.05, "LeftTop",    Font_med)
add_param_text("Header_TopMid",     -0.1, 0.05, "CenterTop",  Font_med)
add_param_text("Header_TopRight",    0.9, 0.05, "RightTop",   Font_med)

add_param_text("Header_BottomLeft", -0.9, 1.06, "LeftBottom",  Font_med)
add_param_text("Header_BottomRight", 0.9, 1.06, "RightBottom", Font_med)

-- Draw logs and aliases
add_param_block("Line", ROW_COUNT, LOG_START_Y, Font_small)
add_param_block("AliasLine", ROW_COUNT, ALIAS_START_Y, Font_small)

return true
