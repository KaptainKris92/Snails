"""Writes the text maps in Assets/Levels. One character = one world unit.

Legend (see Assets/Editor/LevelBuilder.cs):
  #  solid    /  floor slope up-right    \\  floor slope down-right
  q  ceiling slope solid top-left        p  ceiling slope solid top-right
  ^  thorns   S  spawn   E  end zone     .  air

Coordinates here are x to the right and y UP from the floor row 0; the file is written top row
first. Edit the .txt files directly for small tweaks, or this script for bigger changes, then run
Snails > Build All Levels From Text in the editor.

Run from the project root:  python Tools/levelgen.py
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Levels")


class Map:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.g = [["." for _ in range(w)] for _ in range(h)]

    def put(self, x, y, ch):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.g[y][x] = ch

    def fill(self, x0, y0, x1, y1, ch="#"):
        for y in range(min(y0, y1), max(y0, y1) + 1):
            for x in range(min(x0, x1), max(x0, x1) + 1):
                self.put(x, y, ch)

    def border(self, t=2):
        self.fill(0, 0, self.w - 1, t - 1)
        self.fill(0, self.h - t, self.w - 1, self.h - 1)
        self.fill(0, 0, t - 1, self.h - 1)
        self.fill(self.w - t, 0, self.w - 1, self.h - 1)

    def floor_thorns(self, x0, x1, y):
        self.fill(x0, y, x1, y, "^")

    def ramp_up(self, x, y, n):
        """Floor slope rising to the right, starting at (x, y), n cells long, solid underneath."""
        for i in range(n):
            self.put(x + i, y + i, "/")
            self.fill(x + i, 0, x + i, y + i - 1)

    def ramp_down(self, x, y, n):
        """Floor slope falling to the right from height y."""
        for i in range(n):
            self.put(x + i, y - i, "\\")
            self.fill(x + i, 0, x + i, y - i - 1)

    def text(self, comment):
        rows = ["// " + line for line in comment.strip().splitlines()]
        rows += ["".join(r) for r in reversed(self.g)]
        return "\n".join(rows) + "\n"


def write(name, m, comment):
    for problem in check(name, m):
        print("  WARNING", name + ":", problem)
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, name + ".txt"), "w", newline="\n") as f:
        f.write(m.text(comment))
    print("wrote", name, m.w, "x", m.h)


# ---------------------------------------------------------------- Level 5: Swing Hall
# Open height 7 everywhere, so the ceiling is always in rope range (8) of the floor.
def level5():
    m = Map(72, 11)
    m.border()
    m.floor_thorns(16, 20, 2)
    m.fill(28, 2, 29, 4)           # low pillar to swing over
    m.floor_thorns(36, 42, 2)
    m.fill(50, 2, 51, 5)           # taller pillar
    m.floor_thorns(52, 56, 2)
    m.fill(66, 2, 69, 3, "E")
    m.put(4, 2, "S")
    return m


# ---------------------------------------------------------------- Level 6: Scroll Run
def level6():
    m = Map(130, 16)
    m.border()
    m.fill(2, 9, 44, 13)            # ceiling, open height 7
    for i in range(2):              # rise by 2
        m.put(45 + i, 9 + i, "q")
        m.fill(45 + i, 10 + i, 45 + i, 13)
    m.fill(47, 11, 80, 13)          # higher ceiling, open height 9: scroll, don't touch the floor
    for i in range(2):              # fall by 2
        m.put(81 + i, 10 - i, "p")
        m.fill(81 + i, 11 - i, 81 + i, 13)
    m.fill(83, 9, 127, 13)
    m.floor_thorns(18, 26, 2)
    m.fill(27, 2, 28, 2)            # island
    m.floor_thorns(29, 38, 2)
    m.floor_thorns(47, 68, 2)      # the floor under the high ceiling is out of rope range
    m.fill(69, 2, 70, 3)            # island
    m.floor_thorns(71, 80, 2)
    m.floor_thorns(92, 108, 2)
    m.fill(120, 2, 124, 4, "E")
    m.put(4, 2, "S")
    return m


# ---------------------------------------------------------------- Level 7: Push Off
def level7():
    m = Map(64, 34)
    m.border()
    m.fill(2, 2, 61, 5)             # thick floor, top at y 6
    m.fill(14, 6, 61, 12)           # lower block
    m.fill(2, 13, 61, 26)           # upper block
    m.fill(2, 6, 13, 12, ".")       # start chamber, open height 7
    m.fill(14, 6, 19, 8, ".")       # tunnel into the shaft
    m.fill(20, 6, 23, 26, ".")      # shaft, 4 wide, 21 tall
    for i, y in enumerate(range(10, 26, 4)):
        m.put(20 if i % 2 == 0 else 23, y, "#")   # alternating ledges to grab and push off
    m.fill(2, 27, 61, 31, ".")      # top corridor, open height 5
    m.floor_thorns(30, 36, 27)
    m.floor_thorns(44, 48, 27)
    m.fill(50, 27, 51, 29)          # wall to push off / hop
    m.fill(56, 27, 60, 29, "E")
    m.floor_thorns(10, 12, 6)
    m.put(4, 6, "S")
    return m


# ---------------------------------------------------------------- Level 8: Alleys
def level8():
    m = Map(120, 16)
    m.border()
    m.fill(2, 2, 117, 13)           # solid, then carve
    m.fill(2, 2, 12, 8, ".")        # room 1, open height 7
    m.fill(13, 4, 44, 7, ".")       # corridor A, 4 high
    m.floor_thorns(22, 25, 4)
    m.floor_thorns(33, 36, 4)
    m.fill(45, 2, 58, 8, ".")       # room 2
    m.floor_thorns(49, 53, 2)
    m.fill(59, 5, 84, 7, ".")       # corridor B, 3 high
    m.floor_thorns(66, 68, 5)
    m.floor_thorns(76, 78, 5)
    m.fill(85, 2, 98, 8, ".")       # room 3
    m.fill(99, 6, 112, 7, ".")      # corridor C, 2 high
    m.fill(113, 2, 117, 8, ".")     # end room
    m.fill(114, 2, 116, 3, "E")
    m.put(4, 2, "S")
    return m


# ---------------------------------------------------------------- Level 9: Hooks
def level9():
    m = Map(90, 18)
    m.border()
    # Hooks hanging from the ceiling: a stem and a bar. Swinging past a bar end wraps the rope.
    for x in (14, 28, 42, 56, 70):
        m.fill(x, 10, x + 1, 15)        # stem
        m.fill(x - 3, 8, x + 4, 9)      # bar, 5.75 above the floor
    for x0 in (10, 24, 38, 52, 66):
        m.floor_thorns(x0, x0 + 9, 2)
    m.fill(79, 8, 80, 9)                # floating block to loop around
    m.fill(84, 2, 87, 4, "E")
    m.put(4, 2, "S")
    return m


# ---------------------------------------------------------------- Level 10: Gauntlet
def level10():
    m = Map(150, 30)
    m.border()
    # 1) swing hall
    m.fill(2, 9, 33, 27)                # ceiling, open height 7
    m.floor_thorns(14, 22, 2)
    m.fill(26, 2, 27, 4)
    # 2) ramp up to a plateau under a low ceiling, then ramp down
    m.ramp_up(34, 2, 6)
    m.fill(40, 2, 70, 7)                # plateau, top at y 8
    m.fill(34, 15, 75, 27)              # ceiling, open height 7 over the plateau
    m.floor_thorns(52, 56, 8)
    m.ramp_down(71, 7, 6)
    # 3) a hook to wrap around, over a thorn pit
    m.fill(76, 16, 99, 27)              # ceiling
    m.fill(87, 10, 88, 15)              # stem
    m.fill(84, 8, 91, 9)                # bar
    m.floor_thorns(79, 96, 2)
    # 4) push-off shaft up to the finish corridor
    m.fill(100, 2, 147, 12)             # block
    m.fill(100, 13, 101, 27)            # wall, so the shaft cannot be skipped
    m.fill(100, 2, 103, 4, ".")         # tunnel into the shaft
    m.fill(104, 2, 107, 12, ".")        # shaft
    for i, y in enumerate((5, 8, 11)):
        m.put(104 if i % 2 == 0 else 107, y, "#")
    m.fill(102, 18, 147, 27)            # corridor ceiling, open height 5
    m.floor_thorns(118, 126, 13)
    m.fill(140, 13, 145, 15, "E")
    m.put(4, 2, "S")
    return m


# ---------------------------------------------------------------- checks
SOLID = set("#/qp") | {chr(92)}  # chr(92) is the backslash slope


def check(name, m):
    """Spawn and end exist, the end is connected to the spawn through open cells, and every
    standable floor cell has a solid cell within rope range above it. Returns a list of problems."""
    problems = []
    cells = {(x, y): m.g[y][x] for y in range(m.h) for x in range(m.w)}
    spawn = [p for p, c in cells.items() if c == "S"]
    ends = {p for p, c in cells.items() if c == "E"}
    if len(spawn) != 1:
        problems.append("expected one S, found %d" % len(spawn))
    if not ends:
        problems.append("no E")
    if spawn and ends:
        seen, todo = {spawn[0]}, [spawn[0]]
        while todo:
            x, y = todo.pop()
            for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if n in cells and n not in seen and cells[n] not in SOLID and cells[n] != "^":
                    seen.add(n)
                    todo.append(n)
        if not (seen & ends):
            problems.append("E is not connected to S")
    rope = 7.5   # 8 u rope minus margin, measured from the cell centre
    no_anchor = []
    for (x, y), c in cells.items():
        if c in SOLID or c == "^" or (x, y - 1) not in cells or cells[(x, y - 1)] not in SOLID:
            continue
        cx, cy = x + 0.5, y + 0.25
        ok = any(cells.get((ax, ay)) in SOLID
                 for ax in range(int(cx - rope) - 1, int(cx + rope) + 2)
                 for ay in range(y + 1, int(cy + rope) + 2)
                 if (ax + 0.5 - cx) ** 2 + (ay - cy) ** 2 <= rope * rope)
        if not ok:
            no_anchor.append((x, y))
    if no_anchor:
        problems.append("%d floor cells with nothing to grapple above, e.g. %s" % (len(no_anchor), no_anchor[:4]))
    return problems


if __name__ == "__main__":
    write("Level 5", level5(), "Level 5: Swing Hall. Fire, swing, release. Flat ceiling, two pillars, floor thorns.")
    write("Level 6", level6(), "Level 6: Scroll Run. Long ceiling with a rise and fall; chain ropes without touching the floor.")
    write("Level 7", level7(), "Level 7: Push Off. Climb the shaft by pushing off the walls (Space), then run the top corridor.")
    write("Level 8", level8(), "Level 8: Alleys. Three corridors of decreasing height. Stay centred.")
    write("Level 9", level9(), "Level 9: Hooks. Hanging bars the rope must wrap around; a floating block to loop.")
    write("Level 10", level10(), "Level 10: Gauntlet. Everything together.")
