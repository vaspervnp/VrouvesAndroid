"""The art the Android version adds to VrouvesPC's: the Shepherd and the Priest, the props
of the new maps and the buildings of stages 10-19, built with VrouvesPC's Blender tools
(../VrouvesPC/tools/blender: blender_toon's flat two-tone materials and outlines, models'
Rig camera, render_stages' map). Run inside Blender with both tools/blender folders on
sys.path (tools/render_extra.py does it through the Blender MCP or Blender's console):

    import vrouves_extra as vx
    vx.sprites(out)            # shepherd.png, priest.png
    vx.stages(out, [10, 11])   # stageN.png, 2560 x 1792 like the PC version's

The stage data comes from this repository's Vrouves/Assets/data/stages.json.
"""
import json
import math
import os
import random

import bpy

import blender_toon
import buildings as bd
import models as m
import render_stages as rs
from blender_toon import R

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

blender_toon.C.update({
    "hay": (0.95, 0.78, 0.32), "hay2": (0.8, 0.62, 0.22), "copper": (0.88, 0.45, 0.18),
    "grape": (0.42, 0.14, 0.55), "vine": (0.32, 0.62, 0.14), "honey": (1.0, 0.68, 0.08),
    "alpine": (0.52, 0.62, 0.36), "thatch": (0.72, 0.55, 0.3), "hive": (0.95, 0.93, 0.85),
    "hiveblue": (0.25, 0.55, 0.9), "sail": (0.98, 0.97, 0.92), "monk": (0.12, 0.1, 0.12),
    "milk": (0.92, 0.94, 0.97), "barrel": (0.55, 0.32, 0.12),
})


# ====================================================================== the defenders

def shepherd(t, p="Shepherd"):
    """the Shepherd: brown cloak, red sash, grey beard, the sariki; his woven bag at the hip,
    the sling whirling over his head with a stone in its pouch"""
    t.cyl(p + "_body", (0, 0, 1.05), 0.56, 1.3, "brown", r2=0.45)
    t.cyl(p + "_sash", (0, 0, 0.62), 0.58, 0.18, "red")
    head = ((0, -0.04, 2.1), (0.6, 0.55, 0.62))
    t.sphere(p + "_head", *head, "skin")
    t.face(p, head, 0.16, (0, -0.1), smile=0)
    my = t.surface_y(head, 0, -0.27) - 0.05
    t.sphere(p + "_beard", (0, my + 0.08, 1.78), (0.42, 0.24, 0.34), "silver")
    for sd in (-1, 1):
        t.sphere(p + "_mous%d" % sd, (sd * 0.2, my - 0.02, 1.98), (0.24, 0.09, 0.09), "silver",
                 rot=(0, R(sd * 15), 0))
    t.sphere(p + "_sariki", (0, -0.02, 2.48), (0.64, 0.58, 0.34), "black")
    for k in range(12):
        a = math.pi + k * math.pi / 11
        t.sphere(p + "_fr%d" % k, (math.cos(a) * 0.62, -math.sin(a) * 0.55 - 0.05, 2.36), (0.055, 0.055, 0.055),
                 "black")
    for sd in (-1, 1):
        t.sphere(p + "_boot%d" % sd, (sd * 0.3, -0.15, 0.12), (0.22, 0.32, 0.14), "black")
    # the vourgia, the woven shoulder bag, striped
    t.box(p + "_strap", (-0.05, -0.6, 1.45), (0.1, 0.05, 1.3), "orange", rot=(0, R(-38), 0))
    t.sphere(p + "_bag", (-0.62, -0.6, 0.8), (0.34, 0.2, 0.32), "orange")
    for k, col in enumerate(("red", "yellow", "red")):
        t.sphere(p + "_bst%d" % k, (-0.62, -0.75, 0.68 + k * 0.12), (0.28 - 0.03 * abs(k - 1), 0.06, 0.035), col)
    # the right arm up, whirling the sling: its cord a blur of a ring, the pouch with the stone
    t.cyl(p + "_arm", (0.55, -0.25, 2.05), 0.13, 0.95, "brown", rot=(0, R(-22), 0))
    hx, hy, hz = 0.72, -0.3, 2.55
    t.sphere(p + "_hand", (hx, hy, hz), (0.14, 0.14, 0.14), "skin")
    t.torus(p + "_whirl", (hx, hy, hz + 0.25), 0.55, 0.025, "wood", rot=(R(72), 0, 0))
    t.sphere(p + "_pouch", (hx + 0.52, hy - 0.05, hz + 0.32), (0.16, 0.1, 0.12), "brown")
    t.sphere(p + "_stone", (hx + 0.55, hy - 0.12, hz + 0.36), (0.11, 0.09, 0.1), "rock")
    t.box(p + "_cord", (hx + 0.26, hy - 0.03, hz + 0.16), (0.55, 0.02, 0.02), "wood", rot=(0, R(-25), 0))


def priest(t, p="Priest"):
    """the Priest: the village papas of VrouvesPC's church, his cross in one hand and a
    hand bell in the other, a gold stole down his front"""
    t.priest(p, 0, 0, 0, look=(0, -0.1))
    t.box(p + "_stole", (0, -0.62, 0.95), (0.22, 0.05, 1.05), "gold")
    for k in range(3):
        t.box(p + "_stx%d" % k, (0, -0.66, 0.6 + k * 0.32), (0.06, 0.02, 0.16), "red")
    t.sphere(p + "_hand2", (0.78, -0.45, 1.75), (0.13, 0.13, 0.13), "skin")
    t.cyl(p + "_bhandle", (0.78, -0.5, 1.95), 0.05, 0.3, "wood", verts=8)
    t.cyl(p + "_bell", (0.78, -0.5, 1.5), 0.28, 0.42, "gold", r2=0.1)
    t.torus(p + "_brim", (0.78, -0.5, 1.3), 0.28, 0.04, "gold")
    t.sphere(p + "_clap", (0.78, -0.5, 1.24), (0.07, 0.07, 0.07), "dark")


SPRITES = {"shepherd": shepherd, "priest": priest}


def sprites(out, names=None):
    os.makedirs(out, exist_ok=True)
    t = m.Rig("VA_Sprites")
    done = []
    try:
        for name, build in SPRITES.items():
            if names and name not in names:
                continue
            t.clear()
            build(t)
            t.frame(256, 256, 128, 200)
            t.render_to(os.path.join(out, name + ".png"))
            done.append(name)
    finally:
        t.restore()
    return done


# ====================================================================== props of the new maps

def beehive(t, p, x, y, k):
    """a painted box hive on a stone, a bee or two about"""
    t.box(p + "_stand", (x, y + 0.1, 0.18), (1.1, 0.9, 0.36), "stone2")
    t.box(p + "_box", (x, y + 0.1, 0.8), (1.0, 0.8, 0.9), "hive" if k % 2 else "hiveblue")
    t.box(p + "_band", (x, y - 0.31, 0.62), (1.0, 0.02, 0.12), "hiveblue" if k % 2 else "hive")
    t.box(p + "_lid", (x, y + 0.1, 1.33), (1.2, 1.0, 0.16), "tile")
    t.box(p + "_door", (x, y - 0.31, 0.42), (0.4, 0.03, 0.08), "dark")
    for j in range(2):
        bx, bz = x - 0.3 + 0.7 * j, 1.65 + 0.25 * ((k + j) % 2)
        t.sphere(p + "_bee%d" % j, (bx, y - 0.3, bz), (0.1, 0.07, 0.07), "yellow")
        t.box(p + "_bs%d" % j, (bx, y - 0.37, bz), (0.04, 0.01, 0.12), "black")
        t.sphere(p + "_wing%d" % j, (bx, y - 0.32, bz + 0.1), (0.08, 0.02, 0.05), "white")


def vine(t, p, x, y, w, left, right, k):
    """a cell of a vine row on its wire: posts, leaves, bunches of grapes"""
    t.cyl(p + "_post", (x - w * 0.45, y, 0.8), 0.06, 1.6, "wood", verts=8)
    if not right:
        t.cyl(p + "_post2", (x + w * 0.45, y, 0.8), 0.06, 1.6, "wood", verts=8)
    t.box(p + "_wire", (x, y, 1.45), (w, 0.03, 0.03), "dark")
    for j in range(3):
        lx = x - w * 0.33 + j * w * 0.33
        t.sphere(p + "_leaf%d" % j, (lx, y, 1.3 + 0.1 * ((j + k) % 2)), (0.42, 0.32, 0.36), "vine")
        if (j + k) % 2 == 0:
            for g in range(4):
                gx, gz = lx + (g % 2) * 0.12 - 0.06, 0.95 - (g // 2) * 0.12
                t.sphere(p + "_gr%d_%d" % (j, g), (gx, y - 0.3, gz), (0.08, 0.08, 0.08), "grape")


def bale(t, p, x, y, k):
    """a round bale of hay on its side"""
    t.cyl(p + "_bale", (x, y, 0.55), 0.55, 1.1, "hay", rot=(0, R(90), R(15 if k % 2 else -12)))
    t.torus(p + "_ring", (x, y - 0.05, 0.55), 0.35, 0.03, "hay2", rot=(0, R(90), R(15 if k % 2 else -12)))
    t.sphere(p + "_straw", (x + 0.4, y - 0.5, 0.06), (0.35, 0.2, 0.05), "hay")


def windmill(t, p, x, y, s=1.0, sails=8, ang=0.0):
    """a stone windmill: round tower, pointed roof, cloth sails on spokes facing us"""
    t.cyl(p + "_tower", (x, y, 1.3 * s), 0.75 * s, 2.6 * s, "plaster", r2=0.6 * s)
    t.cyl(p + "_roof", (x, y, 2.9 * s), 0.72 * s, 0.7 * s, "thatch", r2=0.0)
    t.box(p + "_door", (x, y - 0.68 * s, 0.6 * s), (0.45 * s, 0.06, 1.0 * s), "wood")
    hz, hy = 2.45 * s, y - 0.85 * s
    t.sphere(p + "_hub", (x, hy, hz), (0.14 * s, 0.14 * s, 0.14 * s), "wood")
    for k in range(sails):
        a = ang + k * 2 * math.pi / sails
        c, sn = math.cos(a), math.sin(a)
        t.box(p + "_spoke%d" % k, (x + c * 0.75 * s, hy, hz + sn * 0.75 * s), (1.5 * s, 0.04, 0.05 * s), "wood",
              rot=(0, -a, 0))
        a2 = a + 0.22
        t.box(p + "_sail%d" % k, (x + math.cos(a2) * 0.8 * s, hy + 0.02, hz + math.sin(a2) * 0.8 * s),
              (1.1 * s, 0.02, 0.32 * s), "sail", rot=(0, -a2, 0))


def snow(t, p, x, y, k):
    t.sphere(p + "_snow", (x + 0.3 * ((k % 3) - 1), y, 0.06), (0.75, 0.55, 0.08), "snow")


def water(mp, rnd):
    cells = mp.cells("~")
    if not cells:
        return
    mp.patch("Water", cells, "water", 0.025)
    for k, (c, r) in enumerate(cells):
        x, y = mp.centre(c, r)
        t = mp.t
        t.torus("Ripple%d" % k, (x + rnd.uniform(-0.5, 0.5), y + rnd.uniform(-1, 1), 0.05), 0.3, 0.03, "white",
                scale=(1, 0.6, 0.3))
        # reeds along the banks
        for sd in (-1, 1):
            if mp.ch(c + sd, r) not in "~#" and rnd.random() < 0.5:
                for j in range(3):
                    t.cyl("Reed%d_%d_%d" % (k, sd, j), (x + sd * 1.0, y - 0.6 + j * 0.5, 0.45), 0.04, 0.9,
                          "grass2", verts=6, rot=(0, R(sd * 8), 0))


def props(mp, rnd):
    t = mp.t
    for k, (c, r) in enumerate(mp.cells("HVBM")):
        ch = mp.ch(c, r)
        x, y = mp.centre(c, r)
        p = "%s_%d_%d" % (ch, c, r)
        if ch == "H":
            beehive(t, p, x, y, c + r)
        elif ch == "V":
            vine(t, p, x, y, mp.S, mp.ch(c - 1, r) == "V", mp.ch(c + 1, r) == "V", c + r)
        elif ch == "B":
            bale(t, p, x, y, c + r)
        elif ch == "M":
            windmill(t, p, x, y, 0.75, ang=(c * 0.7 + r))


# ====================================================================== the buildings

def _block(mp):
    c0, r0, c1, r1 = mp.bounds("K")
    return mp.rect(c0, r0, c1, r1)


def dairy(mp):
    """the cheese dairy: a stone house with the ΤΥΡΟΚΟΜΕΙΟ sign, cheeses ripening on racks,
    milk churns, the cheesemaker in his apron"""
    t = mp.t
    bd.paving(mp, "KP", "stone")
    x0, x1, yf, yb = _block(mp)
    h = 3.4
    yh = yf + 1.0
    d = bd.fit_depth(yh, yb, h + 1.0)
    bd.house(t, "Dairy", x0 + 0.4, x1 - 0.4, yh, d, h, wall="stone", roof="gable", windows=2,
             shutter="wood", sign="ΤΥΡΟΚΟΜΕΙΟ", sign_col="shutter")
    t.cyl("Dairy_chim", (x1 - 1.4, yh + d - 0.8, h + 1.1), 0.25, 1.0, "stone2", verts=10)
    for k in range(3):
        t.sphere("Dairy_smoke%d" % k, (x1 - 1.3 + 0.3 * k, yh + d - 0.8, h + 1.8 + 0.5 * k),
                 (0.3 + 0.08 * k, 0.25, 0.25), "smoke")
    # a rack of graviera wheels by the door, churns on the other side
    rx = x0 + 1.6
    t.box("Dairy_rack", (rx, yh - 0.6, 0.9), (2.2, 0.7, 0.08), "wood")
    t.box("Dairy_rack2", (rx, yh - 0.6, 1.6), (2.2, 0.7, 0.08), "wood")
    for sd in (-1, 1):
        t.box("Dairy_rl%d" % sd, (rx + sd * 1.05, yh - 0.6, 0.85), (0.08, 0.08, 1.7), "wood")
    for k in range(6):
        t.cyl("Dairy_ch%d" % k, (rx - 0.7 + (k % 3) * 0.7, yh - 0.6, 1.04 + 0.7 * (k // 3)), 0.3, 0.22,
              "cheese", verts=16)
    for k in range(3):
        cx = x1 - 1.0 - k * 0.7
        t.cyl("Churn%d" % k, (cx, yh - 0.5, 0.5), 0.26, 1.0, "milk", r2=0.2, verts=16)
        t.cyl("ChurnL%d" % k, (cx, yh - 0.5, 1.05), 0.16, 0.12, "silver", verts=12)
    x, y = mp.centre(3, 4)
    bd.villager(t, "Cheesemaker", x - 1.0, yf - 0.2, 0, "white", hair="black", look=(0.4, 0.2), hat="cap")


def watermill(mp):
    """the watermill: stone mill house, its wooden wheel turning in the race on the left,
    the millstone and sacks of flour in the yard"""
    t = mp.t
    bd.paving(mp, "KP", "stone")
    x0, x1, yf, yb = _block(mp)
    h = 3.4
    yh = yf + 1.2
    d = bd.fit_depth(yh, yb, h + 1.0)
    bd.house(t, "Mill", x0 + 1.8, x1 - 0.3, yh, d, h, wall="stone", roof="gable", windows=1,
             shutter="wood", sign="ΝΕΡΟΜΥΛΟΣ", sign_col="wood", sign_txt="cream")
    # the race and the wheel against the left wall
    wx = x0 + 1.0
    wy = yh - 0.2
    t.box("Race", (wx, wy, 0.05), (2.6, 1.6, 0.06), "water")
    t.box("Race_trough", (wx - 0.2, wy + 0.3, 3.2), (0.5, 0.5, 0.25), "wood")
    t.cyl("Wheel", (wx, wy, 1.6), 1.45, 0.35, "wood", rot=(R(90), 0, 0), verts=24)
    t.cyl("Wheel_in", (wx, wy - 0.02, 1.6), 1.1, 0.37, "brown", rot=(R(90), 0, 0), verts=24)
    for k in range(10):
        a = k * math.pi / 5
        t.box("Paddle%d" % k, (wx + math.cos(a) * 1.5, wy, 1.6 + math.sin(a) * 1.5), (0.45, 0.45, 0.1), "wood",
              rot=(0, -a, 0))
    for k in range(4):
        a = k * math.pi / 4
        t.box("Spoke%d" % k, (wx, wy - 0.2, 1.6), (2.3, 0.06, 0.12), "wood", rot=(0, -a, 0))
    t.cyl("Wheel_hub", (wx, wy - 0.22, 1.6), 0.22, 0.12, "dark", rot=(R(90), 0, 0), verts=12)
    for k in range(3):
        t.sphere("Splash%d" % k, (wx - 0.8 + 0.8 * k, wy - 0.7, 0.2 + 0.1 * (k % 2)), (0.3, 0.2, 0.15), "white")
    # the yard: an old millstone leaning, flour sacks, the miller white with flour
    px0, px1, py0, py1 = mp.rect(*mp.bounds("P"))
    t.cyl("Millstone", (x1 - 1.4, yf - 0.6, 0.75), 0.75, 0.3, "stone2", rot=(R(80), 0, 0), verts=24)
    t.cyl("Millstone_eye", (x1 - 1.4, yf - 0.78, 0.75), 0.15, 0.06, "dark", rot=(R(80), 0, 0), verts=12)
    for k in range(3):
        t.sphere("Flour%d" % k, (x1 - 3.2 - k * 0.8, yf - 0.4, 0.45), (0.38, 0.32, 0.45), "white")
    bd.villager(t, "Miller", x0 + 3.6, yf - 0.6, 0, "cream", hair="silver", look=(0.3, 0.2), hat="cap")


def cave(mp):
    """the Sfendoni cave: the mountainside over the K cells, the dark mouth with its
    stalactites, the sign and a lantern"""
    t = mp.t
    bd.paving(mp, "P", "stone2")
    x0, x1, yf, yb = _block(mp)
    cx, w = (x0 + x1) / 2, x1 - x0
    rnd = random.Random(12)
    for k in range(14):
        rx = x0 + 0.6 + (k % 7) * (w - 1.2) / 6 + rnd.uniform(-0.4, 0.4)
        ry = yf + 1.6 + (k // 7) * 2.6
        s = rnd.uniform(1.1, 1.6)
        t.sphere("Hill%d" % k, (rx, ry, s * 0.7), (s * 1.1, s * 0.9, s), "rock" if k % 2 else "rock2")
    t.sphere("Hilltop", (cx, yf + 3.0, 2.2), (w * 0.38, 2.4, 1.6), "rock2")
    for k in range(5):
        t.sphere("HillGrass%d" % k, (x0 + 1.0 + k * (w - 2) / 4, yf + 3.5, 3.2 - abs(k - 2) * 0.4),
                 (0.6, 0.4, 0.25), "grass2")
    t.sphere("Mouth", (cx, yf + 0.35, 1.25), (2.0, 0.4, 1.35), "black")
    t.sphere("Mouth_in", (cx, yf + 0.25, 0.85), (1.4, 0.35, 0.8), "hole")
    for k in range(6):
        t.cyl("Stal%d" % k, (cx - 1.4 + k * 0.56, yf + 0.1, 2.15 - 0.12 * (k % 2)), 0.12, 0.6, "rock",
              r2=0.0, rot=(R(180), 0, 0), verts=8)
    t.box("Cave_sign", (x0 + 1.5, yf - 0.4, 1.3), (2.6, 0.1, 0.6), "wood")
    t.text("Cave_txt", "ΣΦΕΝΔΟΝΗ", (x0 + 1.5, yf - 0.47, 1.3), 0.4, "cream", extrude=0.01)
    t.cyl("Cave_post", (x0 + 1.5, yf - 0.35, 0.5), 0.07, 1.0, "wood", verts=8)
    lx = x1 - 1.3
    t.cyl("Lantern_pole", (lx, yf - 0.3, 0.9), 0.06, 1.8, "dark", verts=8)
    t.box("Lantern", (lx, yf - 0.3, 1.95), (0.35, 0.35, 0.45), "flame")
    t.cyl("Lantern_top", (lx, yf - 0.3, 2.27), 0.27, 0.2, "dark", r2=0.05, verts=8)


def monastery(mp):
    """the monastery: a stone church with its dome, the bell tower apart, the arcade of
    the monks' cells behind, cypresses, a monk"""
    t = mp.t
    bd.paving(mp, "KP", "stone")
    x0, x1, yf, yb = _block(mp)
    h = 3.4
    d = bd.fit_depth(yf + 0.8, yb, h + 2.5)
    w = x1 - x0 - 2.4
    bd.church_body(t, "Mon", x0 + 0.6 + w / 2, yf + 0.8, w, d, h, dome="tile", tower=False)
    # the bell tower on the right, its arch and the big bell
    tx = x1 - 0.9
    t.box("Mon_tower", (tx, yf + 1.4, 2.4), (1.3, 1.3, 4.8), "plaster")
    t.box("Mon_arch", (tx, yf + 0.73, 3.9), (0.75, 0.06, 1.1), "dark")
    t.sphere("Mon_bell", (tx, yf + 1.0, 3.8), (0.3, 0.3, 0.34), "gold")
    t.cyl("Mon_tcap", (tx, yf + 1.4, 5.0), 0.75, 0.8, "tile", r2=0.0, verts=4, rot=(0, 0, R(45)))
    t.box("Mon_cross_v", (tx, yf + 1.4, 5.65), (0.08, 0.08, 0.6), "gold")
    t.box("Mon_cross_h", (tx, yf + 1.4, 5.75), (0.35, 0.08, 0.08), "gold")
    for k in range(5):     # the arcade behind
        ax = x0 + 0.8 + k * (x1 - x0 - 1.6) / 4
        t.box("Arc%d" % k, (ax, yb - 0.6, 1.2), (0.25, 0.25, 2.4), "plaster")
    t.box("Arc_roof", ((x0 + x1) / 2, yb - 0.6, 2.5), (x1 - x0 - 1.2, 0.6, 0.25), "tile")
    _, _, py0, py1 = mp.rect(*mp.bounds("P"))
    mx = x0 + 1.0
    t.priest("Monk", mx, py0 + 1.6, 0, look=(0.4, 0.2))
    t.sphere("Monk_rope", (mx, py0 + 1.1, 1.0), (0.66, 0.3, 0.08), "monk")


def apiary(mp):
    """the beekeeper's hut, ΜΕΛΙ on its sign, jars of thyme honey on a bench, the
    beekeeper in his veiled hat"""
    t = mp.t
    bd.paving(mp, "KP", "stone")
    x0, x1, yf, yb = _block(mp)
    h = 3.2
    yh = yf + 1.0
    d = bd.fit_depth(yh, yb, h + 1.0)
    bd.house(t, "Hut", x0 + 0.4, x1 - 0.4, yh, d, h, wall="cream", roof="gable", windows=1,
             shutter="wood", sign="ΜΕΛΙ", sign_col="honey", sign_txt="brown")
    t.box("Bench", (x0 + 2.0, yh - 0.6, 0.6), (2.4, 0.7, 0.1), "wood")
    for sd in (-1, 1):
        t.box("BenchL%d" % sd, (x0 + 2.0 + sd * 1.1, yh - 0.6, 0.3), (0.1, 0.6, 0.6), "wood")
    for k in range(4):
        jx = x0 + 1.1 + k * 0.6
        t.cyl("Jar%d" % k, (jx, yh - 0.6, 0.9), 0.2, 0.5, "honey", verts=14)
        t.cyl("JarLid%d" % k, (jx, yh - 0.6, 1.18), 0.21, 0.08, "red", verts=14)
    bx, by = x1 - 1.4, yf - 0.4
    t.cyl("Keeper_body", (bx, by, 1.0), 0.55, 1.3, "white", r2=0.45)
    head = ((bx, by - 0.03, 2.0), (0.5, 0.46, 0.52))
    t.sphere("Keeper_head", *head, "skin")
    t.face("Keeper", head, 0.12, (-0.4, 0.1), smile=0.15)
    t.cyl("Keeper_brim", (bx, by, 2.35), 0.85, 0.06, "hive")
    t.cyl("Keeper_hat", (bx, by, 2.55), 0.4, 0.4, "hive")
    t.cyl("Keeper_veil", (bx, by, 2.0), 0.8, 0.65, "glass", r2=0.82)
    t.cyl("Keeper_smoker", (bx + 0.7, by - 0.4, 1.2), 0.18, 0.45, "silver", verts=12)
    for k in range(3):
        t.sphere("Keeper_puff%d" % k, (bx + 0.8 + 0.2 * k, by - 0.45, 1.6 + 0.3 * k), (0.18 + 0.05 * k,) * 3,
                 "smoke")


def distillery(mp):
    """the raki still: the copper kazani on its fire under an open shed of tiles on posts,
    its pipe to the barrel, the distiller raising a glass"""
    t = mp.t
    bd.paving(mp, "KP", "stone")
    x0, x1, yf, yb = _block(mp)
    cx = (x0 + x1) / 2
    w = x1 - x0 - 0.6
    hh = 3.0
    sy0 = yf + (yb - yf) * 0.5
    d = bd.fit_depth(sy0, yb, hh + 0.8)
    t.box("Shed_back", (cx, sy0 + d - 0.2, hh / 2), (w, 0.3, hh), "stone")
    for i, px in enumerate((cx - w / 2 + 0.2, cx + w / 2 - 0.2)):
        t.cyl("Post%d" % i, (px, sy0, hh / 2), 0.13, hh, "wood", verts=10)
    t.box("Shed_roof", (cx, sy0 + d / 2, hh + 0.1), (w + 0.4, d + 0.4, 0.22), "tile", rot=(R(-8), 0, 0))
    for k in range(3):
        t.cyl("Shed_bottle%d" % k, (cx + 1.0 + k * 0.5, sy0 + d - 0.6, 0.4), 0.15, 0.8, "glass", r2=0.06,
              verts=10)
    kx, ky = cx - 0.9, yf + 1.3
    bd.cauldron_fire(t, "Still_fire", kx, ky - 0.2, "copper")
    t.sphere("Still_pot", (kx, ky, 1.35), (1.0, 0.95, 0.9), "copper")
    t.cyl("Still_neck", (kx, ky, 2.3), 0.3, 0.5, "copper", r2=0.18)
    t.cyl("Still_pipe", (kx + 1.1, ky, 2.35), 0.09, 2.2, "copper", rot=(0, R(80), 0), verts=10)
    t.cyl("Barrel", (kx + 2.4, ky, 0.8), 0.6, 1.6, "barrel", verts=16)
    for k in range(2):
        t.torus("Hoop%d" % k, (kx + 2.4, ky, 0.35 + k * 0.9), 0.61, 0.04, "dark")
    t.cretan("Distiller", x0 + 1.2, yf - 0.3, 0, look=(0.4, 0.2), glass=True)


def windmills(mp):
    """a big windmill on the hill, its eight cloth sails, flour sacks and the miller's donkey path"""
    t = mp.t
    bd.paving(mp, "P", "stone2")
    x0, x1, yf, yb = _block(mp)
    cx = (x0 + x1) / 2
    windmill(t, "Big", cx, yf + 3.0, 2.3, ang=0.3)
    for k in range(3):
        t.sphere("WFlour%d" % k, (x0 + 1.2 + k * 0.8, yf + 0.3, 0.45), (0.38, 0.32, 0.45), "white")
    t.box("WSign", (x1 - 1.8, yf + 0.0, 1.2), (2.9, 0.1, 0.6), "wood")
    t.text("WSign_txt", "ΑΝΕΜΟΜΥΛΟΣ", (x1 - 1.8, yf - 0.07, 1.2), 0.38, "cream", extrude=0.01)
    t.cyl("WSign_post", (x1 - 1.8, yf + 0.05, 0.45), 0.07, 0.9, "wood", verts=8)


def threshing(mp):
    """the threshing floor: a round paved floor ringed with standing stones, the heap of
    sheaves in the middle, the farmer with his wooden fork"""
    t = mp.t
    c0, r0, c1, r1 = mp.bounds("KP")
    x0, x1, yf, yb = mp.rect(c0, r0, c1, r1)
    cx, cy = (x0 + x1) / 2, (yf + yb) / 2
    rx, ry = (x1 - x0) / 2 + 0.3, (yb - yf) / 2 + 0.3
    t.cyl("Aloni", (cx, cy, 0.05), 1.0, 0.1, "stone", verts=40).scale = (rx, ry, 1)
    t.torus("Aloni_ring", (cx, cy, 0.12), 1.0, 0.035, "stone2", scale=(rx, ry, 1.0))
    for k in range(18):
        a = k * 2 * math.pi / 18
        t.box("Aloni_st%d" % k, (cx + math.cos(a) * rx, cy + math.sin(a) * ry, 0.2), (0.4, 0.3, 0.35), "stone2",
              rot=(0, 0, a))
    hx, hy = cx, cy + ry * 0.35
    t.sphere("Sheaves", (hx, hy, 0.6), (2.0, 1.2, 1.1), "hay")
    for k in range(7):
        a = k * math.pi / 6
        t.sphere("Sheaf%d" % k, (hx + math.cos(a) * 1.6, hy - 0.2 + math.sin(a) * 0.5, 0.35), (0.6, 0.4, 0.4),
                 "hay2" if k % 2 else "hay")
    for k in range(6):
        t.sphere("Chaff%d" % k, (cx - rx * 0.6 + k * 0.7, cy - ry * 0.5 + (k % 2) * 0.4, 0.13),
                 (0.3, 0.2, 0.04), "hay")
    fx, fy = x1 + 0.1, yf + 0.8
    t.cretan("Farmer", fx, fy, 0, look=(-0.5, 0.2))
    t.cyl("Fork", (fx + 0.6, fy - 0.4, 1.4), 0.05, 2.8, "wood", rot=(0, R(-10), 0), verts=8)
    for k in range(3):
        t.cyl("Tine%d" % k, (fx + 0.72 + (k - 1) * 0.14, fy - 0.4, 2.95), 0.03, 0.45, "wood", verts=6)


def vineyard(mp):
    """the winery: a stone house with the ΟΙΝΟΠΟΙΕΙΟ sign under a pergola of vines,
    barrels, a basket of grapes, the vintner"""
    t = mp.t
    bd.paving(mp, "KP", "stone")
    x0, x1, yf, yb = _block(mp)
    h = 3.4
    yh = yf + 1.4
    d = bd.fit_depth(yh, yb, h + 1.0)
    bd.house(t, "Winery", x0 + 0.3, x1 - 0.3, yh, d, h, wall="stone", roof="gable", windows=1,
             shutter="wine", sign="ΚΡΑΣΙ", sign_col="wine", sign_txt="cream")
    for sd in (0, 1):
        t.cyl("Perg_post%d" % sd, (x0 + 0.6 + sd * (x1 - x0 - 1.2), yh - 1.2, 1.3), 0.07, 2.6, "wood", verts=8)
    t.box("Perg_beam", ((x0 + x1) / 2, yh - 1.2, 2.6), (x1 - x0 - 1.0, 0.1, 0.1), "wood")
    for k in range(6):
        t.sphere("Perg_leaf%d" % k, (x0 + 0.8 + k * (x1 - x0 - 1.6) / 5, yh - 0.9, 2.75), (0.5, 0.7, 0.25), "vine")
        if k % 2:
            t.sphere("Perg_gr%d" % k, (x0 + 0.8 + k * (x1 - x0 - 1.6) / 5, yh - 1.3, 2.35), (0.16, 0.12, 0.25),
                     "grape")
    for k in range(2):
        bx = x0 + 1.0 + k * 1.3
        t.cyl("WBarrel%d" % k, (bx, yf - 0.1, 0.55), 0.55, 1.0, "barrel", rot=(R(90), 0, 0), verts=16)
        for j in (-1, 1):
            t.torus("WHoop%d_%d" % (k, j), (bx, yf - 0.1 + j * 0.35, 0.55), 0.56, 0.04, "dark", rot=(R(90), 0, 0))
    t.cyl("Basket", (x1 - 1.0, yf - 0.2, 0.3), 0.45, 0.55, "thatch", r2=0.55, verts=14)
    for k in range(5):
        t.sphere("BGr%d" % k, (x1 - 1.2 + (k % 3) * 0.2, yf - 0.25 + (k // 3) * 0.2, 0.62), (0.16, 0.16, 0.16),
                 "grape")
    bd.villager(t, "Vintner", x1 + 0.2, yf + 1.2, 0, "wine", hair="black", look=(-0.4, 0.2), hat="sariki")


def summit(mp):
    """the chapel on the summit of Psiloritis: dry stones under a vaulted roof, a wooden
    cross by it, snow about"""
    t = mp.t
    bd.paving(mp, "KP", "stone2")
    x0, x1, yf, yb = _block(mp)
    cx, w = (x0 + x1) / 2, x1 - x0 - 1.0
    yh = yf + 0.9
    h = 2.6
    d = bd.fit_depth(yh, yb, h + 1.0)
    t.box("Chap_wall", (cx, yh + d / 2, h / 2), (w, d, h), "stone")
    for k in range(12):
        t.sphere("Chap_st%d" % k, (x0 + 1.0 + (k % 6) * (w - 0.6) / 5, yh - 0.02, 0.45 + 0.9 * (k // 6)),
                 (0.33, 0.06, 0.2), "stone2")
    vault = t.cyl("Chap_vault", (cx, yh + d / 2, h), d / 2 + 0.2, w + 0.3, "stone2", rot=(0, R(90), 0), verts=24)
    vault.scale = (0.55, 1.0, 1.0)
    t.box("Chap_door", (cx, yh - 0.04, 0.8), (0.8, 0.08, 1.6), "wood")
    t.box("Chap_cross_v", (cx, yh - 0.06, 2.1), (0.08, 0.04, 0.5), "dark")
    t.box("Chap_cross_h", (cx, yh - 0.06, 2.2), (0.3, 0.04, 0.08), "dark")
    for k in range(4):
        t.sphere("Chap_snow%d" % k, (x0 + 1.2 + k * (w - 0.4) / 3, yh + d / 2, h + 0.85), (0.6, 0.5, 0.12), "snow")
    kx = x1 - 0.4
    t.box("Cross_v", (kx, yf + 0.3, 1.5), (0.18, 0.18, 3.0), "wood")
    t.box("Cross_h", (kx, yf + 0.3, 2.3), (1.2, 0.18, 0.18), "wood")
    t.cretan("Climber", x0 + 0.8, yf - 0.4, 0, look=(0.4, 0.3), beard=True)


BUILDINGS = {f.__name__: f for f in (dairy, watermill, cave, monastery, apiary, distillery, windmills,
                                     threshing, vineyard, summit)}


def ground_extra(mp, rnd):
    """the summit: rocky alpine turf instead of grass, and patches of snow"""
    if mp.st["base"] != "summit":
        return
    g = bpy.data.objects.get("Ground")
    if g is not None:
        g.data.materials.clear()
        g.data.materials.append(mp.t.toon("alpine"))
    for k, (c, r) in enumerate(mp.cells(".")):
        if rnd.random() < 0.12:
            x, y = mp.centre(c, r)
            snow(mp.t, "Snow%d" % k, x, y + rnd.uniform(-0.8, 0.8), k)


def load():
    with open(os.path.join(ROOT, "Vrouves", "Assets", "data", "stages.json"), encoding="utf-8") as f:
        return json.load(f)


def stages(out, which=None):
    data = load()
    os.makedirs(out, exist_ok=True)
    t = m.Rig("VA_Stage")
    done = []
    try:
        for st in data["stages"]:
            if st["base"] not in BUILDINGS or (which and st["id"] not in which):
                continue
            t.clear()
            rnd = random.Random(st["id"] * 7919)
            mp = rs.Map(t, st, data["cols"], data["rows"])
            rs.ground(mp, rnd)
            ground_extra(mp, rnd)
            water(mp, rnd)
            rs.props(mp, rnd)
            props(mp, rnd)
            BUILDINGS[st["base"]](mp)
            W, H = mp.cols * t.PX, mp.rows * t.PX
            t.sc.render.film_transparent = False
            t.frame(W, H, W / 2, H / 2, origin=(mp.cols * mp.S / 2, mp.rows * mp.D / 2))
            t.render_to(os.path.join(out, "stage%d.png" % st["id"]))
            t.sc.render.film_transparent = True
            done.append(st["id"])
    finally:
        t.restore()
    return done
