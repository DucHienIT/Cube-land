#!/usr/bin/env python3
"""
Cube Blaster level generator — object-shaped sculptures.

Each level's sculpture is a recognisable OBJECT (ball, strawberry, ice cream, rocket...)
rather than an abstract figure. A shape is authored as a 2D pixel map of semantic colour
letters; the generator revolves it into a 3D solid so it reads as a round volume from the
game's steep 3/4 camera, then builds a per-colour bank that is solvable by construction.

DENSITY: every level is a HIGH-density SOLID sculpture — VOXEL_MIN..VOXEL_MAX cubes, ramped
linearly across the 60 levels. The pixel maps are authored at a readable ~11x12, far too
coarse to reach four figures, so each level RESAMPLES its map to a larger resolution before
revolving it (`resample`), and `solve_resample` solves for the factor that hits the level's
cube target rather than hand-authoring bigger art.

The sculptures are NOT hollowed. An earlier version kept only the one-cell shell, which held
the cube count down but made every half-demolished fruit an empty husk. VOXEL_MAX is a
pacing knob (it sets how long a level runs), not a rendering budget — Unity only instantiates
the exposed cubes, so raising it costs level duration and JSON size, not frame time.

Run:  python Tools/gen_levels.py
Writes Assets/_Project/Resources/Levels/level_NNN.asset (+ .meta for new files) — real Unity
ScriptableObjects (`LevelAsset`), not JSON, so a level's palette / gun slots / bank are
inspectable and editable in the editor and the custom inspector can verify solvability. The
sculpture itself is written PACKED, one int per cube, because a VoxelDef[] of 3000-8000
entries would be a megabyte of YAML per level and would hang the inspector.

Writing the .asset YAML from here rather than through an editor import step keeps
`python Tools/gen_levels.py` the single command that rebuilds all content, with no open Unity
required. The only thing it needs from the project is LevelAsset's script GUID, read from the
committed .cs.meta.

NOTE: keep this file IN the repo. The previous generator lived only in a scratchpad and was
lost, which meant level content could not be regenerated.
"""

import hashlib
import math
import os
import random
import re

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.join(HERE, "..", "Assets", "_Project", "Resources", "Levels")
SCRIPT_META = os.path.join(HERE, "..", "Assets", "_Project", "Scripts", "Core", "Level",
                           "LevelAsset.cs.meta")
LEVEL_COUNT = 60

# Matches LevelAsset.CoordinateOffset — a cube packs into one int as four bytes, and the axes
# are centred on the sculpture so two of them go negative.
COORDINATE_OFFSET = 128

# Cubes per sculpture, level 1 -> level 60. These are SOLID counts. Raising them is safe for
# frame time (only exposed cubes get a GameObject) but costs level duration roughly linearly:
# four guns at gunFireInterval 0.03 clear ~133 cubes a second.
VOXEL_MIN = 3000
VOXEL_MAX = 8000
# How far off target a level may land. The count is a step function of the resample factor,
# so an exact hit is not always reachable.
VOXEL_TOLERANCE = 0.03

# Ammo per bank block. A block is a single readable two-digit number, NOT a share of the
# sculpture — an earlier version capped the block count to the 15 that fit on screen and let
# the value grow with the level, which put 100-240 on every block. The bank is a scrolling
# queue instead (BankArea shows GameConfig.bankVisibleRows of it), so the count is free.
#
# A level therefore holds cubes/60 blocks — 49 early, 133 late — and one gun burns a block in
# BLOCK_TARGET * gunFireInterval seconds. That product IS the player's deploy cadence, so
# moving this band moves how frantic the game is.
BLOCK_MIN = 50
BLOCK_MAX = 70
BLOCK_TARGET = 60

BANK_COLUMNS = 5

# ---------------------------------------------------------------------------
# Palette: semantic letter -> colour slot, per PaletteConfig voxel set.
# Sets differ (set A has no yellow, set C has no purple...), so each shape is
# rendered with a set that can express every colour it uses.
# ---------------------------------------------------------------------------
SETS = {
    0: {"R": 0, "O": 1, "G": 2, "P": 3, "W": 4, "K": 5},           # A: red/orange/green/purple
    1: {"R": 0, "O": 0, "Y": 1, "G": 2, "P": 3, "W": 4, "K": 5},   # B: red-orange/yellow/green/purple
    2: {"R": 0, "O": 1, "G": 2, "Y": 3, "W": 4, "K": 5},           # C: deep red/orange/green/yellow
    3: {"R": 0, "Y": 1, "G": 2, "P": 3, "W": 4, "K": 5},           # D: red/yellow/light green/purple
}


def sets_supporting(letters):
    return [i for i, m in SETS.items() if all(ch in m for ch in letters)]


# ---------------------------------------------------------------------------
# Shape authoring
# ---------------------------------------------------------------------------
# '.' = empty. Rows are top-to-bottom (row 0 is the top of the object).
# `mode`:
#   "round" — revolve: depth per column follows the row's circular profile, so the
#             object bulges toward the camera and reads as a solid volume.
#   "flat"  — uniform slab depth (for genuinely flat things: slices, cards).

SHAPES = []


def shape(name, art, mode="round", depth=1.0):
    rows = [r for r in art.strip("\n").split("\n")]
    w = max(len(r) for r in rows)
    rows = [r.ljust(w, ".") for r in rows]
    SHAPES.append({"name": name, "rows": rows, "mode": mode, "depth": depth})


shape("heart", """
..RR...RR..
.RRRRRRRRR.
RRRRRRRRRRR
RRRRRRRRRRR
RRRRRRRRRRR
.RRRRRRRRR.
..RRRRRRR..
...RRRRR...
....RRR....
.....R.....
""")

shape("strawberry", """
....G.G....
..GGGGGGG..
.GGRRRRRGG.
.RRRWRRWRR.
RRRRRRRRRRR
RRWRRRRRWRR
RRRRRWRRRRR
.RRWRRRRWR.
.RRRRRRRRR.
..RRRWRRR..
...RRRRR...
....RRR....
""")

shape("apple", """
.....K.....
....KKGG...
...KKGGGGG.
.RRRRRRRRR.
RRRRRRRRRRR
RRRRRRRRRRR
RRRRRRRRRRR
RRRRRRRRRRR
RRRRRRRRRRR
.RRRRRRRRR.
..RRR.RRR..
..RR...RR..
""")

shape("lemon", """
....GGG....
...YYYYY...
..YYYYYYY..
.YYYYYYYYY.
YYYYYYYYYYY
YYYYYYYYYYY
YYYYYYYYYYY
.YYYYYYYYY.
..YYYYYYY..
...YYYYY...
""")

shape("cherry", """
...GGGGG...
...G...G...
...G...G...
..GG...GG..
..G.....G..
.WRR...RRW.
.RRRR.RRRR.
RRRRR.RRRRR
RRRRR.RRRRR
.RRRR.RRRR.
..RRR.RRR..
""")

shape("watermelon", """
.....RRR.....
...RRRRRRR...
..RRRKRRKRR..
.RRRRRRRRRRR.
RRRKRRRRRKRRR
RRRRRRRRRRRRR
WWWWWWWWWWWWW
GGGGGGGGGGGGG
GGGGGGGGGGGGG
""", mode="flat", depth=0.22)

shape("pineapple", """
....G.G....
...GGGGG...
..GGGGGGG..
...GGGGG...
..YYYOYYY..
.YOYYYYYOY.
YYYOYYYOYYY
YOYYYOYYYOY
YYYOYYYOYYY
YOYYYOYYYOY
YYYOYYYOYYY
.YYYYOYYYY.
..YYYYYYY..
...YYYYY...
""")

shape("avocado", """
...GGG...
..GGGGG..
.GGGGGGG.
.GGWWWGG.
GGWWWWWGG
GGWWKWWGG
GGWWKWWGG
GGWWWWWGG
GGWWWWWGG
.GGWWWGG.
..GGGGG..
...GGG...
""")

shape("carrot", """
...G.G...
...GGG...
...GGG...
...OOO...
..OOOOO..
..OOOOO..
..OOOOO..
...OOO...
...OOO...
...OOO...
....O....
....O....
""")

shape("ice_cream", """
...WWWWW...
..WWWWWWW..
.PPPPPPPPP.
.PPPPPPPPP.
RRRRRRRRRRR
RRRRRRRRRRR
.OOOOOOOOO.
..OOOOOOO..
..OOOOOOO..
...OOOOO...
...OOOOO...
....OOO....
....OOO....
.....O.....
""")

shape("donut", """
...WWWWW...
..WWWWWWW..
.WWWWWWWWW.
WWWW...WWWW
OWW.....WWO
OOO.....OOO
OOO.....OOO
OOOO...OOOO
.OOOOOOOOO.
..OOOOOOO..
...OOOOO...
""")

shape("cupcake", """
.....R.....
....RRR....
...WWWWW...
..WWWWWWW..
.WWWWWWWWW.
.WWWWWWWWW.
WWWWWWWWWWW
.PPPPPPPPP.
.POPPOPPOP.
.PPPPPPPPP.
..POPPOPP..
..PPPPPPP..
...PPPPP...
""")

shape("lollipop", """
..RRRRR..
.RWWWWWR.
RWRRRRRWR
RWRWWWRWR
RWRWRWRWR
RWRWWWRWR
RWRRRRRWR
.RWWWWWR.
..RRRRR..
....W....
....W....
....W....
....W....
""")

shape("gem", """
..PPPPPPP..
.PWPPPPPWP.
PPPPPPPPPPP
.PPPPPPPPP.
..PPPPPPP..
...PPPPP...
...PPPPP...
....PPP....
....PPP....
.....P.....
""")

shape("star", """
.....Y.....
....YYY....
....YYY....
YYYYYYYYYYY
.YYYYYYYYY.
..YYYYYYY..
..YYYYYYY..
.YYY...YYY.
.YY.....YY.
YY.......YY
""")

shape("rocket", """
....W....
...WWW...
..WWWWW..
..WRRRW..
..WWWWW..
..WWWWW..
..WKKKW..
..WWWWW..
..WWWWW..
.RWWWWWR.
RRWWWWWRR
RR.WWW.RR
...OOO...
....O....
""")

shape("cactus", """
....GGG....
....GGG....
.GG.GGG.GG.
GGG.GGG.GGG
GGGGGGGGGGG
GGGGGGGGGGG
.GGGGGGGGG.
....GGG....
....GGG....
....GGG....
...OOOOO...
...OOOOO...
...OOOOO...
""")

shape("mushroom", """
...RRRRR...
..RRWRRWR..
.RRRRRRRRR.
RRWRRRRRWRR
RRRRRRRRRRR
RRRWRRRWRRR
.RRRRRRRRR.
..WWWWWWW..
...WWWWW...
...WWWWW...
...WWWWW...
..WWWWWWW..
""")

shape("present", """
...Y...Y...
..YYY.YYY..
...YYYYY...
PPPPYYYPPPP
PPPPYYYPPPP
PPPPYYYPPPP
YYYYYYYYYYY
PPPPYYYPPPP
PPPPYYYPPPP
PPPPYYYPPPP
PPPPYYYPPPP
""")

shape("grapes", """
....GG.....
...G.G.....
...G..GG...
..PPP.PPP..
.PPPPPPPPP.
.PPPPPPPPP.
..PPPPPPP..
..PPPPPPP..
...PPPPP...
...PPPPP...
....PPP....
""")

shape("balloon", """
...RRRRR...
..RRRRRRR..
.RRRRRRRRR.
RRRRRRRRRRR
RRRRRRRRRRR
RRRRRRRRRRR
.RRRRRRRRR.
..RRRRRRR..
...RRRRR...
....RRR....
....W.W....
.....W.....
.....W.....
""")

shape("penguin_toy", """
...KKKKK...
..KKKKKKK..
.KKWWKWWKK.
.KKWKKKWKK.
.KKKOOOKKK.
KKKWWWWWKKK
KKWWWWWWWKK
KKWWWWWWWKK
KKWWWWWWWKK
.KWWWWWWWK.
.KKWWWWWKK.
..OO...OO..
""")

shape("robot_head", """
.K.......K.
.K.......K.
.KKKKKKKKK.
KKKKKKKKKKK
KWWKKKKKWWK
KWWKKKKKWWK
KKKKKKKKKKK
KKKGGGGGKKK
KKKKKKKKKKK
.KKKKKKKKK.
...KKKKK...
....K.K....
""")

shape("dice", """
WWWWWWWWW
WWWWWWWWW
WWKWWWKWW
WWKWWWKWW
WWWWWWWWW
WWWWKWWWW
WWWWKWWWW
WWWWWWWWW
WWKWWWKWW
WWKWWWKWW
WWWWWWWWW
""", mode="flat", depth=1.0)


# Parametric ball shapes (a hand-drawn circle is hard to keep clean at several sizes).
def make_ball(name, radius, base, spot, spotted):
    rows = []
    n = radius * 2 + 1
    for j in range(n):
        row = ""
        for i in range(n):
            dx, dy = i - radius, j - radius
            if dx * dx + dy * dy <= (radius + 0.35) ** 2:
                # quantised patch pattern so the ball reads as a toy ball, not a blob
                patch = ((i + 1) // 3 + (j + 1) // 3) % 2 == 0
                row += spot if (spotted and patch) else base
            else:
                row += "."
        rows.append(row)
    SHAPES.append({"name": name, "rows": rows, "mode": "round", "depth": 1.0})


make_ball("soccer_ball", 5, "W", "K", True)
make_ball("beach_ball", 5, "R", "W", True)
make_ball("orange_fruit", 4, "O", "O", False)
make_ball("planet", 6, "P", "W", True)


# ---------------------------------------------------------------------------
# 2D map -> 3D voxels
# ---------------------------------------------------------------------------
NEIGHBOURS = ((1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1))


def runs(row):
    """Contiguous spans of non-empty cells in a row, as (lo, hi) inclusive."""
    spans = []
    start = None
    for i, ch in enumerate(row):
        if ch != "." and start is None:
            start = i
        elif ch == "." and start is not None:
            spans.append((start, i - 1))
            start = None
    if start is not None:
        spans.append((start, len(row) - 1))
    return spans


def resample(rows, factor):
    """Nearest-neighbour upscale of a pixel map.

    This is what turns a hand-authored ~11x12 map into the several-hundred-cell-across grid
    a 1000-2000 cube sculpture needs. Nearest-neighbour (not smoothing) is deliberate: the
    art is a colour map, and any interpolation would invent colours that no palette set can
    express. A single authored pixel simply becomes a solid k*k patch, which keeps details
    like the strawberry seeds recognisable at the larger size instead of dissolving.
    """
    if factor <= 1.0001:
        return rows
    h = len(rows)
    w = len(rows[0])
    height = max(1, int(round(h * factor)))
    width = max(1, int(round(w * factor)))
    out = []
    for j in range(height):
        src = rows[min(h - 1, j * h // height)]
        out.append("".join(src[min(w - 1, i * w // width)] for i in range(width)))
    return out


def build_voxels(sh, palette_set, bulk, factor=1.0):
    """Turn the pixel map into a 3D object.

    "round" mode is a true SOLID OF REVOLUTION: a row that is N cells wide is also made
    ~N cells deep, with the per-column depth following the row's circular profile. This
    matters because the sculpture is on a turntable — an object given a fixed shallow
    depth is a coin, and spinning it side-on exposes that instantly.

    The object is SOLID — every cell inside the volume is a real, shootable cube. It used to
    be hollowed to a one-cell shell so the cube count would stay affordable, but a shell is
    visibly wrong the moment the player breaks through it: a strawberry demolished halfway is
    an empty husk. Nothing here caps the count any more; the renderer cost is handled on the
    Unity side instead, where VoxelCubeField only instantiates the cubes that are currently
    exposed and reveals buried ones as their neighbours die.

    `bulk` (0..1) scales depth: low = flattened relief, 1 = fully round.
    `factor` resamples the art first — see `resample` / `solve_resample`.
    """
    slot = SETS[palette_set]
    rows = resample(sh["rows"], factor)
    h = len(rows)
    scale = bulk * sh["depth"]

    filled_by_pos = {}
    for j, row in enumerate(rows):
        y = h - 1 - j  # flip: row 0 is the TOP of the object, y grows upward
        for lo, hi in runs(row):
            # Revolve each contiguous RUN of the row, not the row as a whole. A row like the
            # heart's "..RR...RR.." or the cherry's "RRRRR.RRRRR" is two separate volumes;
            # measuring the profile across the full row gives both of them the depth of the
            # combined span, which revolves them into one wide flat plate instead of two
            # lobes. At the authored resolution that was a couple of stray cubes — at the
            # resampled resolution this generator now uses it is a very visible slab.
            half = max((hi - lo + 1) / 2.0, 0.5)
            cx = (lo + hi) / 2.0

            for i in range(lo, hi + 1):
                ch = row[i]
                if ch == ".":
                    continue
                if ch not in slot:
                    raise ValueError("shape %s uses '%s', not in set %d" % (sh["name"], ch, palette_set))

                if sh["mode"] == "flat":
                    d = max(1, int(round(len(row) * scale)))
                else:
                    t = (i - cx) / half
                    profile = math.sqrt(max(0.0, 1.0 - t * t))
                    d = max(1, int(round(2.0 * half * scale * profile)))

                z0 = -(d // 2)
                for k in range(d):
                    filled_by_pos[(i, y, z0 + k)] = slot[ch]

    return [{"x": p[0], "y": p[1], "z": p[2], "c": filled_by_pos[p]}
            for p in sorted(filled_by_pos)]


def count_exposed(voxels):
    """Cubes with at least one of six neighbours missing — i.e. how many GameObjects the
    sculpture actually starts with. This is the number that drives renderer cost, so the
    generator reports it alongside the total."""
    occupied = {(v["x"], v["y"], v["z"]) for v in voxels}
    return sum(1 for p in occupied
               if not all((p[0] + d[0], p[1] + d[1], p[2] + d[2]) in occupied for d in NEIGHBOURS))


def voxel_target(level):
    t = (level - 1) / float(max(1, LEVEL_COUNT - 1))
    return int(round(VOXEL_MIN + (VOXEL_MAX - VOXEL_MIN) * t))


def solve_resample(sh, palette_set, bulk, target):
    """Find the resample factor whose cube count lands closest to `target`.

    The sculpture is a solid volume, so count ~ factor^3 — that law gives a first guess
    accurate enough that a short bisection finishes it. (It was factor^2 while the shapes
    were hollowed to a shell; using the wrong exponent just costs extra iterations, but the
    cube root is what converges here.) Solving per level rather than picking a fixed factor
    per shape is what lets the difficulty ramp be stated directly in cubes: a tall thin
    rocket and a fat ball reach 5000 at very different resolutions.
    """
    base = len(build_voxels(sh, palette_set, bulk, 1.0))
    guess = (target / float(max(1, base))) ** (1.0 / 3.0)
    lo, hi = max(1.0, guess * 0.55), max(1.2, guess * 1.7)

    best = None
    for _ in range(16):
        mid = (lo + hi) * 0.5
        count = len(build_voxels(sh, palette_set, bulk, mid))
        if best is None or abs(count - target) < abs(best[1] - target):
            best = (mid, count)
        if abs(best[1] - target) <= target * VOXEL_TOLERANCE:
            break
        if count < target:
            lo = mid
        else:
            hi = mid
        if hi - lo < 0.004:
            break
    return best


# ---------------------------------------------------------------------------
# Bank: solvable by construction, per colour (guns are colour-locked)
# ---------------------------------------------------------------------------
def split_color(ammo, rng):
    """Break one colour's ammo into blocks that each read as a two-digit number.

    The count is whichever of floor/ceil(ammo / BLOCK_TARGET) lands closest to the target
    rather than a fixed divisor: some totals simply cannot be cut into the band (90 is one
    block of 90 or two of 45, both outside 50-70), and landing slightly under beats landing
    far over — an oversized block parks a gun on one colour for a long stretch.
    """
    if ammo <= BLOCK_MAX:
        return [ammo]

    low = max(1, ammo // BLOCK_TARGET)
    count = min((low, low + 1), key=lambda c: abs(ammo / float(c) - BLOCK_TARGET))
    base, remainder = divmod(ammo, count)
    parts = [base + (1 if i < remainder else 0) for i in range(count)]

    # Jitter inside the band so the grid does not read as machine-uniform. Moves that would
    # leave the band are skipped rather than clamped, which keeps the per-colour total exact.
    for _ in range(len(parts) * 2):
        a, b = rng.randrange(len(parts)), rng.randrange(len(parts))
        if a == b:
            continue
        move = rng.randint(1, 6)
        if parts[a] - move >= BLOCK_MIN and parts[b] + move <= BLOCK_MAX:
            parts[a] -= move
            parts[b] += move
    return parts


def build_bank(voxels, level, rng):
    need = {}
    for v in voxels:
        need[v["c"]] = need.get(v["c"], 0) + 1

    # EXACT ammo, no surplus: the bank holds one dart per cube, per colour, so
    # sum(bank) == len(voxels). Nothing can be wasted, which is what makes the
    # exact count solvable rather than brutal:
    #   - a gun only fires when the selector hands it a live, unreserved,
    #     camera-exposed voxel of its colour (Gun.Update -> RequestTarget < 0 = hold),
    #   - reservation guarantees no two darts claim the same voxel,
    #   - GameManager.DeployBlock refuses a block whose colour is already cleared,
    #   - a gun only retires early once its colour is gone, i.e. it has nothing left
    #     to shoot anyway.
    # The block COUNT is free — the bank is a scrolling queue, not a fixed grid — so the
    # value can stay in a readable band instead of absorbing the level's density.
    blocks = []
    for color in sorted(need):
        blocks += [(p, color) for p in split_color(need[color], rng)]

    # Interleave colours so the player must think about which colour to deploy next,
    # instead of clearing one colour at a time in bank order.
    rng.shuffle(blocks)
    return [b[0] for b in blocks], [b[1] for b in blocks]


def check_bank(level, voxels, bank, bank_colors):
    """Guns are colour-locked, so the exact-ammo rule has to hold PER COLOUR, not just
    in total — a global match that is short on red and long on green is unwinnable."""
    need = {}
    for v in voxels:
        need[v["c"]] = need.get(v["c"], 0) + 1
    have = {}
    for value, color in zip(bank, bank_colors):
        have[color] = have.get(color, 0) + value

    if min(bank) <= 0:
        raise AssertionError("level %d has a non-positive bank block: %s" % (level, bank))
    if need != have:
        raise AssertionError("level %d ammo != cubes per colour: need=%s have=%s"
                             % (level, need, have))
    if sum(bank) != len(voxels):
        raise AssertionError("level %d ammo %d != cubes %d" % (level, sum(bank), len(voxels)))

    # A block over the band is the one that actually hurts: it is a three-digit label and a
    # gun stuck on one colour. Under the band is only possible when a colour's whole total is
    # small, which is harmless.
    if max(bank) > BLOCK_MAX:
        raise AssertionError("level %d has a %d-ammo block, band is %d-%d"
                             % (level, max(bank), BLOCK_MIN, BLOCK_MAX))


# ---------------------------------------------------------------------------
# Level plan: which shape, how deep, which palette
# ---------------------------------------------------------------------------
def shape_sets(sh):
    letters = {ch for row in sh["rows"] for ch in row if ch != "."}
    candidates = sets_supporting(letters)
    if not candidates:
        raise ValueError("no palette set supports shape %s (%s)" % (sh["name"], sorted(letters)))
    return candidates


BULKS = (0.8, 1.0)


def build_plan():
    """Assign a (shape, bulk, palette) to every level.

    The difficulty ramp is NO LONGER a by-product of which shape is naturally biggest —
    every level is resampled to a stated cube target (`voxel_target`), so this function only
    has to decide coverage and variety. That is why the old "select, then sort by real voxel
    count" pass is gone: sorting by size would now sort a list of near-identical numbers.

    Every shape appears at least twice, once per bulk, and the second pass walks the roster
    from a different offset so a shape's two visits are far apart and no two neighbouring
    levels are the same object.
    """
    picks = []
    for pass_index, bulk in enumerate(BULKS):
        offset = (pass_index * 13) % len(SHAPES)
        order = SHAPES[offset:] + SHAPES[:offset]
        for sh in order:
            sets = shape_sets(sh)
            picks.append((sh, bulk, sets[pass_index % len(sets)]))

    # Top up to LEVEL_COUNT, still avoiding an adjacent repeat.
    i = 0
    while len(picks) < LEVEL_COUNT:
        sh = SHAPES[i % len(SHAPES)]
        if sh["name"] != picks[-1][0]["name"]:
            sets = shape_sets(sh)
            picks.append((sh, BULKS[-1], sets[(i + 1) % len(sets)]))
        i += 1
    return picks[:LEVEL_COUNT]


PLAN = None


def plan(level):
    global PLAN
    if PLAN is None:
        PLAN = build_plan()
    sh, bulk, palette_set = PLAN[level - 1]
    return sh, bulk, palette_set, random.Random(level * 7919 + 13)


# ---------------------------------------------------------------------------
# ScriptableObject output
# ---------------------------------------------------------------------------
def level_asset_script_guid():
    path = os.path.abspath(SCRIPT_META)
    if not os.path.isfile(path):
        raise SystemExit(
            "LevelAsset.cs.meta not found at %s — open the project in Unity once so it is "
            "generated, then re-run." % path)
    with open(path) as f:
        match = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", f.read(), re.M)
    if not match:
        raise SystemExit("no guid in " + path)
    return match.group(1)


def asset_guid(level):
    """Stable per-level GUID so regenerating never renames an asset. Nothing references a
    level by GUID (they load by Resources path), but a churning GUID would make every
    regeneration a 60-file rewrite in git."""
    return hashlib.md5(("cubeblaster.level.%03d" % level).encode()).hexdigest()


def pack_voxel(v):
    for axis in ("x", "y", "z"):
        value = v[axis] + COORDINATE_OFFSET
        if not 0 <= value <= 255:
            raise AssertionError("voxel %s=%d does not fit one byte" % (axis, v[axis]))
    if not 0 <= v["c"] <= 255:
        raise AssertionError("colour slot %d does not fit one byte" % v["c"])
    return ((v["x"] + COORDINATE_OFFSET)
            | ((v["y"] + COORDINATE_OFFSET) << 8)
            | ((v["z"] + COORDINATE_OFFSET) << 16)
            | (v["c"] << 24))


def yaml_int_array(name, values):
    if not values:
        return "  %s: []\n" % name
    return "  %s:\n" % name + "".join("  - %d\n" % v for v in values)


def write_level_asset(out, level, data):
    """Writes the .asset (and its .meta on first creation) in Unity's own YAML shape — the
    same NativeFormatImporter layout as the config assets already in the project."""
    body = (
        "%%YAML 1.1\n"
        "%%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n"
        "  m_Name: level_%03d\n"
        "  m_EditorClassIdentifier: \n"
        "  level: %d\n"
        "  paletteIndex: %d\n"
        "  gunSlots: %d\n"
        "  bankColumns: %d\n"
        % (data["scriptGuid"], level, level, data["paletteIndex"],
           data["gunSlots"], data["bankColumns"])
    )
    body += yaml_int_array("bank", data["bank"])
    body += yaml_int_array("bankColors", data["bankColors"])
    body += yaml_int_array("packedVoxels", [pack_voxel(v) for v in data["voxels"]])

    path = os.path.join(out, "level_%03d.asset" % level)
    with open(path, "w", newline="\n") as f:
        f.write(body)

    meta = path + ".meta"
    if not os.path.isfile(meta):
        with open(meta, "w", newline="\n") as f:
            f.write("fileFormatVersion: 2\n"
                    "guid: %s\n"
                    "NativeFormatImporter:\n"
                    "  externalObjects: {}\n"
                    "  mainObjectFileID: 11400000\n"
                    "  userData: \n"
                    "  assetBundleName: \n"
                    "  assetBundleVariant: \n" % asset_guid(level))


def main():
    out = os.path.abspath(OUT_DIR)
    # Created rather than required: git prunes the folder when the last level is deleted, and
    # a fresh checkout should not need a manual mkdir before the first run.
    os.makedirs(out, exist_ok=True)
    script_guid = level_asset_script_guid()

    report = []
    for level in range(1, LEVEL_COUNT + 1):
        sh, bulk, palette_set, rng = plan(level)
        target = voxel_target(level)
        factor, _ = solve_resample(sh, palette_set, bulk, target)
        voxels = build_voxels(sh, palette_set, bulk, factor)
        bank, bank_colors = build_bank(voxels, level, rng)
        check_bank(level, voxels, bank, bank_colors)

        data = {
            "scriptGuid": script_guid,
            "level": level,
            "paletteIndex": palette_set,
            "gunSlots": 4,
            "bankColumns": BANK_COLUMNS,
            "voxels": voxels,
            "bank": bank,
            "bankColors": bank_colors,
        }
        write_level_asset(out, level, data)

        span = max(v["x"] for v in voxels) - min(v["x"] for v in voxels) + 1
        report.append((level, sh["name"], len(voxels), target, count_exposed(voxels),
                       factor, span, palette_set, len(bank), max(bank)))

    for r in report:
        print("level %02d  %-14s voxels=%5d (target %4d) exposed=%4d x%.2f span=%2d "
              "set=%d blocks=%2d maxAmmo=%4d" % r)

    counts = [r[2] for r in report]
    print("\nvoxels: min=%d max=%d  |  exposed at start (= renderers): min=%d max=%d"
          % (min(counts), max(counts),
             min(r[4] for r in report), max(r[4] for r in report)))
    print("blocks: min=%d max=%d  |  ammo per block: max=%d  |  clear time at 133 cubes/s: %.0f-%.0fs"
          % (min(r[8] for r in report), max(r[8] for r in report), max(r[9] for r in report),
             min(counts) / 133.0, max(counts) / 133.0))
    print("%d LevelAsset .asset files written to %s" % (LEVEL_COUNT, out))


if __name__ == "__main__":
    main()
