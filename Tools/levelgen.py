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
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, name + ".txt"), "w", newline="\n") as f:
        f.write(m.text(comment))
    print("wrote", name, m.w, "x", m.h)


# ---------------------------------------------------------------- Level 5: Swing Hall
def level5():
    m = Map(72, 18)
    m.border()
    m.put(4, 2, "S")
    m.floor_thorns(18, 21, 2)
    m.floor_thorns(38, 43, 2)
    m.fill(30, 2, 31, 5)           # low pillar to swing over
    m.fill(52, 2, 53, 7)           # taller pillar
    m.floor_thorns(54, 56, 2)
    m.fill(66, 2, 69, 3, "E")
    return m


# ---------------------------------------------------------------- Level 6: Scroll Run
def level6():
    m = Map(130, 24)
    m.border()
    # ceiling profile: 14 high, rises to 18 between 45 and 85, back to 14
    m.fill(2, 15, 44, 21)
    for i in range(4):
        m.put(45 + i, 15 + i, "q")
        m.fill(45 + i, 16 + i, 45 + i, 21)
    m.fill(49, 19, 80, 21)
    for i in range(4):
        m.put(81 + i, 18 - i, "p")
        m.fill(81 + i, 19 - i, 81 + i, 21)
    m.fill(85, 15, 127, 21)
    # floor hazards: thorn pits with islands
    m.put(4, 2, "S")
    m.floor_thorns(20, 27, 2)
    m.fill(28, 2, 29, 2)
    m.floor_thorns(30, 40, 2)
    m.floor_thorns(55, 70, 2)
    m.fill(71, 2, 72, 3)
    m.floor_thorns(73, 84, 2)
    m.floor_thorns(95, 110, 2)
    m.fill(122, 2, 126, 4, "E")
    return m


# ---------------------------------------------------------------- Level 7: Push Off
def level7():
    m = Map(64, 34)
    m.border()
    # start chamber bottom-left, shaft in the middle, run along the top to the end
    m.fill(2, 2, 61, 5)                 # thick floor
    m.put(5, 6, "S")
    m.fill(14, 6, 61, 12)               # big block; shaft cut through it
    m.fill(14, 6, 19, 8, ".")           # tunnel from the start chamber into the shaft
    m.fill(20, 6, 23, 12, ".")          # shaft bottom opening (4 wide)
    m.fill(2, 13, 61, 26)               # upper block
    m.fill(20, 13, 23, 26, ".")         # shaft continues
    # ledges inside the shaft to push off
    for y in range(9, 26, 4):
        side = 20 if (y // 4) % 2 == 0 else 23
        m.put(side, y, "#")
    m.fill(2, 27, 61, 31, ".")          # top corridor
    m.floor_thorns(30, 36, 27)
    m.floor_thorns(44, 48, 27)
    m.fill(50, 27, 51, 29)              # wall to push off / hop
    m.fill(56, 27, 60, 29, "E")
    m.fill(2, 6, 13, 12, ".")           # start chamber air
    m.floor_thorns(9, 13, 6)
    return m


# ---------------------------------------------------------------- Level 8: Alleys
def level8():
    m = Map(120, 22)
    m.border()
    m.put(4, 2, "S")
    # room 1 then corridor A (height 4), room 2, corridor B (height 3), room 3, corridor C (height 2)
    m.fill(2, 2, 117, 19)               # solid everything, then carve
    m.fill(2, 2, 14, 12, ".")           # room 1
    m.fill(15, 6, 44, 9, ".")           # corridor A, 4 high
    m.floor_thorns(22, 26, 6)
    m.floor_thorns(34, 38, 6)
    m.fill(45, 2, 58, 13, ".")          # room 2
    m.floor_thorns(50, 53, 2)
    m.fill(59, 9, 84, 11, ".")          # corridor B, 3 high
    m.floor_thorns(66, 68, 9)
    m.floor_thorns(76, 78, 9)
    m.fill(85, 2, 98, 14, ".")          # room 3
    m.fill(99, 10, 112, 11, ".")        # corridor C, 2 high
    m.fill(113, 2, 117, 12, ".")        # end room
    m.fill(114, 2, 116, 3, "E")
    m.put(4, 2, "S")
    return m


# ---------------------------------------------------------------- Level 9: Hooks
def level9():
    m = Map(96, 26)
    m.border()
    m.put(4, 2, "S")
    # hanging hooks: a stem from the ceiling with a bar at the bottom, rope must wrap the bar
    for x in (24, 48, 72):
        m.fill(x, 14, x + 1, 23)        # stem
        m.fill(x - 4, 12, x + 5, 13)    # bar
    # floating block in a loop room section
    m.fill(60, 7, 62, 8)
    m.floor_thorns(20, 30, 2)
    m.floor_thorns(44, 54, 2)
    m.floor_thorns(68, 78, 2)
    m.fill(90, 2, 93, 4, "E")
    return m


# ---------------------------------------------------------------- Level 10: Gauntlet
def level10():
    m = Map(150, 28)
    m.border()
    m.put(4, 2, "S")
    # 1) swing hall with thorns
    m.floor_thorns(14, 22, 2)
    m.fill(26, 2, 27, 6)
    # 2) ramp up to a plateau, run under a low ceiling
    m.ramp_up(34, 2, 6)
    m.fill(40, 2, 70, 7)
    m.fill(44, 14, 66, 25)              # low ceiling over the plateau (6 high gap)
    m.floor_thorns(52, 56, 8)
    m.ramp_down(71, 7, 6)
    # 3) hook to wrap
    m.fill(86, 16, 87, 25)
    m.fill(82, 14, 91, 15)
    m.floor_thorns(78, 94, 2)
    # 4) push-off shaft up to the finish ledge
    m.fill(100, 2, 147, 12)
    m.fill(100, 2, 103, 4, ".")         # tunnel into the shaft
    m.fill(104, 2, 107, 12, ".")        # shaft
    for y in range(4, 12, 3):
        m.put(104 if (y // 3) % 2 else 107, y, "#")
    m.fill(108, 13, 147, 25)
    m.fill(108, 13, 147, 17, ".")       # finish corridor
    m.floor_thorns(118, 126, 13)
    m.fill(140, 13, 145, 15, "E")
    return m


if __name__ == "__main__":
    write("Level 5", level5(), "Level 5: Swing Hall. Fire, swing, release. Flat ceiling, two pillars, floor thorns.")
    write("Level 6", level6(), "Level 6: Scroll Run. Long ceiling with a rise and fall; chain ropes without touching the floor.")
    write("Level 7", level7(), "Level 7: Push Off. Climb the shaft by pushing off the walls (Space), then run the top corridor.")
    write("Level 8", level8(), "Level 8: Alleys. Three corridors of decreasing height. Stay centred.")
    write("Level 9", level9(), "Level 9: Hooks. Hanging bars the rope must wrap around; a floating block to loop.")
    write("Level 10", level10(), "Level 10: Gauntlet. Everything together.")
