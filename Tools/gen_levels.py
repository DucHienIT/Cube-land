#!/usr/bin/env python3
"""
Cube Blaster level generator — object-shaped sculptures.

Each level's sculpture is a recognisable OBJECT (ball, strawberry, ice cream, rocket...)
rather than an abstract figure. A shape is authored as a 2D pixel map of semantic colour
letters; the generator revolves it into a 3D solid so it reads as a round volume from the
game's steep 3/4 camera, then builds a per-colour bank that is solvable by construction.

Run:  python Tools/gen_levels.py
Writes Assets/_Project/Resources/Levels/level_NNN.json (+ .meta for new files).

NOTE: keep this file IN the repo. The previous generator lived only in a scratchpad and was
lost, which meant level content could not be regenerated.
"""

import json
import math
import os
import random

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.join(HERE, "..", "Assets", "_Project", "Resources", "Levels")
LEVEL_COUNT = 60

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


def build_voxels(sh, palette_set, bulk):
    """Turn the pixel map into a 3D object.

    "round" mode is a true SOLID OF REVOLUTION: a row that is N cells wide is also made
    ~N cells deep, with the per-column depth following the row's circular profile. This
    matters because the sculpture is on a turntable — an object given a fixed shallow
    depth is a coin, and spinning it side-on exposes that instantly.

    The result is then HOLLOWED (any voxel with all six neighbours present is dropped).
    A full-depth solid ball is ~650 voxels of which the player can never see or shoot the
    interior; the shell is ~270 and plays identically, since targeting only ever picks
    camera-exposed voxels.

    `bulk` (0..1) scales depth: low = flattened relief (easy, early levels), 1 = fully round.
    """
    slot = SETS[palette_set]
    rows = sh["rows"]
    h = len(rows)
    scale = bulk * sh["depth"]

    filled_by_pos = {}
    for j, row in enumerate(rows):
        filled = [i for i, ch in enumerate(row) if ch != "."]
        if not filled:
            continue
        lo, hi = min(filled), max(filled)
        half = max((hi - lo + 1) / 2.0, 0.5)
        cx = (lo + hi) / 2.0

        for i in filled:
            ch = row[i]
            if ch not in slot:
                raise ValueError("shape %s uses '%s', not in set %d" % (sh["name"], ch, palette_set))

            if sh["mode"] == "flat":
                d = max(1, int(round(len(row) * scale)))
            else:
                t = (i - cx) / half
                profile = math.sqrt(max(0.0, 1.0 - t * t))
                d = max(1, int(round(2.0 * half * scale * profile)))

            z0 = -(d // 2)
            y = h - 1 - j  # flip: row 0 is the TOP of the object, y grows upward
            for k in range(d):
                filled_by_pos[(i, y, z0 + k)] = slot[ch]

    solid = filled_by_pos
    shell = {p for p in solid
             if not all((p[0] + d[0], p[1] + d[1], p[2] + d[2]) in solid for d in NEIGHBOURS)}

    # Hollowing can strand a shell voxel whose every neighbour happened to be interior,
    # leaving a cube floating beside the sculpture. Put back the minimum interior voxels
    # needed to keep every survivor face-connected.
    while True:
        orphans = [p for p in shell
                   if not any((p[0] + d[0], p[1] + d[1], p[2] + d[2]) in shell for d in NEIGHBOURS)]
        if not orphans:
            break
        added = False
        for p in orphans:
            for d in NEIGHBOURS:
                q = (p[0] + d[0], p[1] + d[1], p[2] + d[2])
                if q in solid and q not in shell:
                    shell.add(q)
                    added = True
                    break
        if not added:
            break  # genuinely detached in the source art — reported by the validator

    return [{"x": p[0], "y": p[1], "z": p[2], "c": solid[p]} for p in sorted(shell)]


# ---------------------------------------------------------------------------
# Bank: solvable by construction, per colour (guns are colour-locked)
# ---------------------------------------------------------------------------
def build_bank(voxels, level, rng):
    need = {}
    for v in voxels:
        need[v["c"]] = need.get(v["c"], 0) + 1

    # Surplus shrinks as levels progress: early levels forgive wasted ammo, late ones don't.
    t = (level - 1) / max(1, LEVEL_COUNT - 1)
    surplus = 1.32 - 0.20 * t

    # Block values stay in a readable band; more voxels -> more blocks, not huge numbers.
    target_block = 12 + int(26 * t)

    blocks = []
    for color in sorted(need):
        ammo = int(math.ceil(need[color] * surplus))
        count = max(2, int(round(ammo / float(target_block))))
        base, rem = divmod(ammo, count)
        parts = [base + (1 if i < rem else 0) for i in range(count)]
        # jitter so columns don't look machine-uniform, keeping the per-colour total intact
        for _ in range(len(parts)):
            a, b = rng.randrange(len(parts)), rng.randrange(len(parts))
            if a != b and parts[a] > 3:
                move = rng.randint(1, max(1, parts[a] // 4))
                parts[a] -= move
                parts[b] += move
        blocks += [(p, color) for p in parts]

    # Interleave colours so the player must think about which colour to deploy next,
    # instead of clearing one colour at a time in bank order.
    rng.shuffle(blocks)
    return [b[0] for b in blocks], [b[1] for b in blocks]


# ---------------------------------------------------------------------------
# Level plan: which shape, how deep, which palette
# ---------------------------------------------------------------------------
def shape_sets(sh):
    letters = {ch for row in sh["rows"] for ch in row if ch != "."}
    candidates = sets_supporting(letters)
    if not candidates:
        raise ValueError("no palette set supports shape %s (%s)" % (sh["name"], sorted(letters)))
    return candidates


BULKS = (0.5, 0.65, 0.8, 1.0)


def build_plan():
    """Assign a (shape, bulk, palette) to every level so voxel count ramps MONOTONICALLY.

    SELECT FIRST, THEN SORT. An earlier version walked a size-ordered list with an
    ever-advancing cursor, which ran out of large combos near level 60 and repeated one
    shape for the last dozen levels. Choosing the roster up front and only then ordering
    it by size cannot run dry: coverage and the difficulty ramp are decided separately.
    """
    # Every shape appears at least twice — smallest and fullest bulk, so a repeat visit
    # is a visibly rounder, meatier object rather than the same sculpture again.
    picks = []
    for sh in SHAPES:
        sets = shape_sets(sh)
        for k, bulk in enumerate((BULKS[0], BULKS[-1])):
            picks.append((sh, bulk, sets[k % len(sets)]))

    # Top up to LEVEL_COUNT with mid-bulk variants, spread over distinct shapes.
    extra = LEVEL_COUNT - len(picks)
    i = 0
    while extra > 0:
        sh = SHAPES[i % len(SHAPES)]
        sets = shape_sets(sh)
        bulk = BULKS[1 + (i // len(SHAPES)) % 2]
        picks.append((sh, bulk, sets[(2 + i) % len(sets)]))
        extra -= 1
        i += 1
    picks = picks[:LEVEL_COUNT]

    # Order by real voxel count -> smooth difficulty ramp.
    sized = sorted(((len(build_voxels(sh, ps, b)), sh, b, ps) for sh, b, ps in picks),
                   key=lambda c: c[0])

    # Break up any "same object twice in a row" by swapping with the next differing entry;
    # neighbours are near-identical in size, so the ramp is unaffected.
    for i in range(1, len(sized)):
        if sized[i][1]["name"] == sized[i - 1][1]["name"]:
            for j in range(i + 1, len(sized)):
                if sized[j][1]["name"] != sized[i - 1][1]["name"]:
                    sized[i], sized[j] = sized[j], sized[i]
                    break

    return [(sh, b, ps) for _, sh, b, ps in sized]


PLAN = None


def plan(level):
    global PLAN
    if PLAN is None:
        PLAN = build_plan()
    sh, depth, palette_set = PLAN[level - 1]
    return sh, depth, palette_set, random.Random(level * 7919 + 13)


def main():
    out = os.path.abspath(OUT_DIR)
    if not os.path.isdir(out):
        raise SystemExit("levels folder not found: " + out)

    report = []
    for level in range(1, LEVEL_COUNT + 1):
        sh, depth, palette_set, rng = plan(level)
        voxels = build_voxels(sh, palette_set, depth)
        bank, bank_colors = build_bank(voxels, level, rng)

        data = {
            "level": level,
            "paletteIndex": palette_set,
            "gunSlots": 4,
            "bankColumns": 5,
            "voxels": voxels,
            "bank": bank,
            "bankColors": bank_colors,
        }

        path = os.path.join(out, "level_%03d.json" % level)
        with open(path, "w") as f:
            json.dump(data, f, separators=(",", ":"))

        report.append((level, sh["name"], len(voxels), depth, palette_set, len(bank)))

    for r in report:
        print("level %02d  %-14s voxels=%3d bulk=%.2f set=%d blocks=%d" % r)
    print("\n%d levels written to %s" % (LEVEL_COUNT, out))


if __name__ == "__main__":
    main()
