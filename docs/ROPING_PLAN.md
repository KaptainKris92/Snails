# Snails: Roping Overhaul Plan

Goal: make Snails feel like Worms Armageddon / World Party (WA/WWP) rope racing, and turn the
existing levels into a proper speedrun course set. This document is written to be handed to an
implementing agent. Each phase is independently shippable; do them in order.

Research sources: Hedgewars rope code (`uGearsHandlersRope.pas`, an open-source WA clone and the
only readable implementation of this rope), the Mablak "Basic Roping Tutorial 1" video
(frame-sampled for timing), the Worms Knowledge Base and TUS community descriptions of Rope Race /
TTRR (Time Trial Rope Race), and the current Snails source on branch `feat/levels_1_to_10`.

---

## 1. What WA roping actually is

The rope in WA is not a swing toy. It is a momentum engine that the player chains at roughly one
rope per second. The things that define the feel, in priority order:

1. **Near-instant attach.** Rope travel to a nearby ceiling takes under 0.1 s. Fire, hit, swing.
   Any attach latency (slow extension, a frame of delay before the joint engages) breaks chaining.
2. **Momentum is never thrown away.** Attaching removes only the radial velocity component
   (tangential is kept). Releasing keeps full velocity. Re-firing has no cooldown and no ammo
   limit in race schemes. Terrain hits while roped bounce (Hedgewars: 60% restitution, both axes)
   instead of stopping.
3. **Strong horizontal input while roped.** Left/Right add a constant horizontal acceleration
   (Hedgewars: about 0.2 g, applied every physics step). This is how you pump a swing, not by
   shortening the rope. Up/Down reel in/out at a constant rate (Hedgewars: ~125 px/s against a max
   rope of 450 px, so about 28% of max length per second; min length 30 px).
4. **Rigid tether, full loops allowed.** The rope is a fixed-length rod, not a slack string: the
   worm is projected onto the circle each step and can loop fully over the anchor. (Snails'
   `DistanceJoint2D` with `maxDistanceOnly = false` already matches this. Keep it.)
5. **Rope wraps around corners.** The rope bends over terrain edges (new pivot, shortened length)
   and unwraps when the worm swings back. This is what makes tunnels and "alleys" playable.
6. **Hard speed cap.** Hedgewars clamps to ~800 px/s while roped, about one screen width per
   0.8 s. Fast, but bounded, so terrain stays readable at top speed.
7. **Firing angle is derived from the hang angle** (mirror image, clamped to a max angle). Snails
   uses free mouse aim. That is a legitimate modern choice and should stay, but note it removes
   one WA skill (angle management) and makes "shadows" trivially easy. See open questions.

Technique vocabulary (this drives level design in section 4):

| Term | What it is | Terrain that produces it |
|---|---|---|
| Scroll | Repeated rope shots into a ceiling while carrying horizontal speed. ~1 rope/s cadence; rope length ~15–20% of screen height; keep a steady distance below the ceiling. | Long ceilings, 2–3 rope-lengths above the floor |
| Alley | A scroll through a narrow horizontal corridor; must stay centred. | Corridor height 1.5–2.5 rope lengths |
| Shadow | Release, re-attach to the ceiling, release again, to drop from a high area to a low one quickly and change direction under an overhang. | Overhangs / ledges with open space beneath |
| Spike | Dropping vertically from a high point into a narrow slot, bouncing off the walls on the way. | Narrow vertical shafts |
| Bounce | Hitting a wall while roped and keeping speed. | Walls at the end of a swing arc |
| Loop | Full 360° around the anchor to redirect. | Open rooms with a single anchor point |

Measured from the video (scroll sequence at 9 fps): attach to release ~0.45 s, free flight
~0.45 s, so one full cycle ≈ 0.9–1.0 s; the worm travels ~¼ of the screen width per cycle at a
comfortable scroll speed, much more when pumping.

---

## 2. Current Snails state and the gaps

Read from `Assets/Scripts/HeadControl.cs`, `PlayerMovement.cs`, the `Snail` prefab, scenes and
project settings. Unity 2022.3.22f1, built-in 2D physics, legacy Input.

**What exists**

- Circle shell (`Rigidbody2D` mass 0.195, angular drag 3, no interpolation, continuous collision),
  physics material bounciness 0.5 normal / **1.0 while grappled**, friction 0.5 / 0.2.
- Mouse-aimed grapple on LMB: a raycast that extends at 20 u/s to max 8 u, then spawns a
  kinematic `GrappleAnchor` prefab and enables a `DistanceJoint2D` **one physics frame later** via
  a coroutine. W/S change length at 5.5 u/s.
- RMB "quick pull": reel in at up to 10 u/s with an ease curve and an impulse on release.
- Space "jump": fires a rope and applies a push-away force (a pole vault). Note that while
  jumping, both `AnimateGrappleExtension` and `HandleJumpPush` extend the rope each
  `FixedUpdate`, so extension speed is doubled and two raycasts run.
- A/D: `AddForce` 0.01 N normal / 0.1 N grappled. On a 0.195 kg body that is ~0.05 g while roped,
  ~0.005 g in the air. About 4× weaker than Hedgewars while roped and effectively zero otherwise.
- Gravity modulation for upward velocity exists but is disabled by prefab values (min scale 1).
- Camera: hard lock on the player, orthographic size 5.31 (view ≈ 19 × 10.6 units), no
  look-ahead, no smoothing.
- Death (RedThorns): freezes, crumble effect, **reloads the whole scene** after 0.5 s. R resets
  instantly without a reload. Timer starts in `Start()`, not on first input.
- Levels 1–3 are ~22 × 17 units (about two screens), built from 560–940 individual 1-unit
  prefabs with separate `BoxCollider2D`s. Level 4 is ~156 × 42 units and uses 403 thorn
  instances. No `CompositeCollider2D` anywhere; no tilemap colliders in use.
- Online leaderboard (Flask on Railway) with top-10 submit; finish sets `timeScale = 0`.

**Gaps against WA, ranked by impact on feel**

1. Attach latency: up to 0.4 s of rope travel plus a one-frame coroutine delay plus prefab
   instantiation. WA is < 0.1 s.
2. Horizontal input while roped is ~4× too weak; air control is absent. Pumping a swing is not
   possible, so speed can only come from quick pull.
3. Bounciness 1.0 while grappled makes wall hits ping-pong (the exact complaint reviewers had
   about the WWP port). WA/Hedgewars is 0.6.
4. No rope wrapping: the rope clips through terrain, so corners, tunnels and alleys cannot be
   designed around.
5. No speed cap: with stronger inputs the snail will become uncontrollable; with a cap it stays
   readable and levels can be tuned for a known top speed.
6. Seam collisions: a rolling circle over hundreds of adjacent box colliders catches on internal
   edges (Box2D "ghost collision"). This reads as random bumps and speed loss.
7. Rigidbody interpolation off at 50 Hz physics: visible jitter on a fast-moving sprite, worse
   with a hard-locked camera.
8. Three overlapping rope verbs (grapple, quick pull, jump) with separate code paths and shared
   flags. Hard to tune, has at least one double-execution bug, and splits the skill ceiling.
9. Speedrun loop: scene reload on death, timer starting before input, no ghost, no splits, no
   "next level" flow.
10. Levels 1–3 are too short to express any of the techniques above.

---

## 3. Mechanics plan

### Phase 0: feel fundamentals (half a day, no architecture change)

All of these are settings or one-line changes. Do them first and playtest before anything else.

| Change | Where | Value |
|---|---|---|
| Rigidbody interpolation on | `Snail` prefab, `Rigidbody2D.interpolation` | Interpolate |
| Physics rate | `ProjectSettings/TimeManager.asset` Fixed Timestep | 0.01 (100 Hz) |
| Grappled bounciness | `Assets/Materials/ShellGrappled.physicsMaterial2D` | 0.6 (friction 0.1) |
| Default bounciness | `ShellDefault.physicsMaterial2D` | 0.35 |
| Rope extension speed | `HeadControl.grappleSpeed` | 60 u/s (8 u in ~0.13 s) |
| Retraction speed | `grappleRetractionSpeed` | 80 u/s (purely visual) |
| Horizontal input while roped | `PlayerMovement.grappledMoveForce` | 0.45 N (≈ 0.23 g) |
| Horizontal input in air | `PlayerMovement.normalMoveForce` | 0.2 N (≈ 0.1 g) |
| Remove the one-frame joint delay | `HeadControl.EnableGrappleNextPhysicsFrame` | enable joint in the same `FixedUpdate` as the hit |
| Anchor without a prefab | `HeadControl` | `grappleJoint.connectedBody = null; connectedAnchor = hit.point` (world-space anchor; no instantiate, no destroy, collisions unaffected) |
| Speed cap while roped | new, in `FixedUpdate` | clamp `rb.velocity` to 22 u/s |
| Delete gravity modulation | `HeadControl.AdjustGravity` | remove; constant gravity is the WA model |

Expected result: chaining ropes at ~1/s becomes possible, swings can be pumped with A/D,
wall hits keep speed, movement looks smooth.

### Phase 1: rope rewrite as one state machine (2–3 days)

Replace `HeadControl` with a `RopeController` that owns a single rope and a single explicit state:

```
Idle -> Firing -> Attached -> Idle
                 \-> Missed -> Idle
```

Rules:

- **Firing.** On LMB down, cast a ray from the shell centre toward the mouse. Extend a visual tip
  at `ropeExtendSpeed` (60 u/s) and attach at the first `Environment` hit within `ropeMaxLength`
  (8 u). If nothing is hit by max length, retract and return to Idle. Allow firing at any time,
  including mid-air and while already attached (re-fire replaces the current rope with no gap;
  this is how a shadow works).
- **Attached.** `DistanceJoint2D`, `maxDistanceOnly = false` (rigid tether, loops allowed),
  `connectedBody = null`, `connectedAnchor = pivot`. `distance` = current distance at attach, so no
  snap. Every `FixedUpdate`:
  - W / S: `distance ± reelSpeed * dt`, clamped to [`ropeMinLength` 0.6 u, `ropeMaxLength`].
    Reel speed 4 u/s (≈ 50% of max per second; a bit faster than Hedgewars because our levels
    are tighter).
  - A / D: `rb.AddForce(Vector2.right * horizontalAccel * rb.mass)` with `horizontalAccel`
    2.2 u/s² (≈ 0.23 g). Expose as a g-multiple in the inspector so tuning is unit-free.
  - Clamp speed to `maxRopedSpeed` 22 u/s.
  - Wrapping (below).
- **Release** on LMB up: disable the joint, keep velocity exactly as is. No impulse, no boost.
  Clean release is what makes release timing a skill.
- **Hold-to-fire variant** (optional toggle): WA fires on press and releases on press again.
  Mouse hold-to-stay-attached is more natural for a mouse game; keep hold, but expose the toggle.

Delete `quick pull` and `jump` as separate rope verbs:

- Quick pull becomes "hold W" (reel in). If a burst is wanted, make W reel at 2× for the first
  0.2 s after attach. One code path.
- Jump becomes a **ground hop** on Space: if grounded (circle cast down, 0.05 u), set
  `velocity.y = 6`. Snails on the floor need a way to get off it; a rope-push does not need to
  exist for that. Mid-air Space does nothing.

Rope wrapping (needed before the level work in section 4 pays off):

- Keep a `List<Vector2> pivots`; the joint anchors to the last pivot.
- Each `FixedUpdate`, raycast from the shell to the last pivot. If it hits terrain before the
  pivot, add the hit point (nudged 0.02 u along the surface normal) as a new pivot, and subtract
  the segment length from `distance`.
- Unwrap: if there are ≥ 2 pivots, test whether the shell has crossed the line through the last
  two pivots (sign of the 2D cross product changes relative to the sign stored when the pivot was
  added). If so, pop the pivot and add its segment length back.
- The `LineRenderer` draws shell → pivots in order.
- Cap pivots at 8.
  This is the standard Unity "rope wrapping with raycasts" pattern; it does not need a custom
  solver.

Input handling: read all inputs in `Update` into a buffered struct, consume in `FixedUpdate`.
Buffer LMB-down for 80 ms so a press just before landing a shot is not lost at 100 Hz.

Tuning table (starting values; all exposed on one `RopeSettings` ScriptableObject so the
implementer and the designer can iterate without touching code):

| Setting | Start value | Why |
|---|---|---|
| ropeMaxLength | 8 u | 75% of screen height, like WA |
| ropeMinLength | 0.6 u | Hedgewars 30 px / 450 px |
| ropeExtendSpeed | 60 u/s | sub-0.15 s attach |
| reelSpeed | 4 u/s | |
| horizontalAccel (roped) | 0.23 g | Hedgewars 0.2 g |
| horizontalAccel (air) | 0.10 g | enough to steer a shadow, not to fly |
| maxRopedSpeed | 22 u/s | ≈ screen width per 0.85 s |
| bounciness roped / free | 0.6 / 0.35 | Hedgewars 0.6 |
| gravity | 9.81 constant | |
| groundHopVelocity | 6 u/s | ~1.8 u apex |
| inputBuffer | 80 ms | |

### Phase 2: speedrun loop (1–2 days)

- **Instant respawn**: death and R both call the same `Respawn()`; no scene reload. Keep the
  crumble effect but respawn after 0.25 s with the camera already cut to the spawn. Keep the
  `levelCompleted` flag and timer in one `RunController`, not spread over `EndZone`,
  `PlayerMovement` and `TimerManager`.
- **Timer starts on first movement input**, stops on finish, shows milliseconds.
- **Finish**: freeze the snail (`bodyType = Static` is fine), do not set `timeScale = 0`; show
  time, PB delta, leaderboard, and "Next level / Retry" buttons. Add a `levelOrder` list so
  Enter goes to the next level.
- **Ghost replay** of the personal best: record `(t, position, rotation)` at 20 Hz during a run,
  save to `PlayerPrefs`/JSON per level on a new PB, play back as a translucent snail. This is the
  single most valuable speedrun feature after instant reset and is ~150 lines.
- **Attempt counter** and **session best** on the HUD.
- **Checkpoints**: not in WA, and they weaken the "one clean run" loop. Skip for levels under
  ~45 s. Reconsider only for levels 9–10 if they end up longer than that.
- Leaderboard: keep as is, but submit asynchronously after showing the result so a slow server
  never blocks the finish screen.

### Phase 3: level kit (1 day) then levels (ongoing)

- Build terrain on a **Tilemap with `TilemapCollider2D` + `CompositeCollider2D`** (Rigidbody2D
  Static, geometry type Outlines or Polygons). This removes seam collisions, cuts the collider
  count from ~900 to one, and makes rope raycasts hit one clean surface. The existing 1-unit
  prefabs become tiles (slopes as sprite tiles with matching physics shapes). The project
  already has the Tilemap module and a `RandomGameObjectBrush`; add a Tile Palette for the
  existing sprites.
- Thorns: a second tilemap layer with its own composite trigger collider, so a level is two
  painted layers plus spawn and finish.
- Camera: `SmoothDamp` follow with velocity look-ahead (`lead = velocity * 0.25 s`, clamped to
  ±4 u) and a mild zoom-out with speed (ortho size 5.3 → 6.5 at cap speed). Clamp to level
  bounds (a `BoxCollider2D` on an empty object, read by the camera).

### Phase 4: polish (after the above is playtested)

- Rope visual: straight taut line while attached (no sine wobble), a 0.1 s hit flash and a few
  dust particles at the pivot, small shell squash on bounces (scale 1.15 × 0.85 for 60 ms).
- Sound: rope fire, hit, release, bounce, with pitch scaled by speed.
- Speed lines or a slight motion blur above 15 u/s.
- Input display (optional, speedrunners like it).

---

## 4. Level plan

Design rules derived from the WA technique vocabulary and the tuning table above:

- Target run times: Level 1 ≈ 15 s, Level 10 ≈ 60 s for a good player. Length in units ≈
  average speed (8–12 u/s for good play) × target time.
- Scroll ceilings: 2–3 rope lengths' worth of clearance below the ceiling (3–5 u of open space),
  and at least 6 screen-widths long before a turn so the cadence can establish.
- Alleys: corridor height 2–3 u (≈ 1.5–2 rope lengths at scroll length), straight or with gentle
  bends; thorns only on the floor so a mistake costs a reset, not a trap.
- Shadows: an overhang whose lip is reachable from above, with 6+ u of open air below so the
  drop-reattach-release has room.
- Spikes: vertical shafts 1.5–2.5 u wide, 8–12 u tall, with the landing area clear of thorns.
- Bounce walls: a wall 4–6 u beyond the end of a natural swing arc, so hitting it at speed is a
  redirect, not a stop.
- Gaps: open-air crossings where the only anchor is a small island at 6–8 u; teaches max-length
  shots.
- Every level readable at the camera's zoom. No blind drops onto thorns. Thorns on the ceiling
  only where the player must be scrolling low on purpose.
- Finish is always visible from the last anchor point.

Progression for levels 1–10 (each level introduces one thing and reuses the previous ones):

| Level | Teaches | Shape |
|---|---|---|
| 1 | Fire, swing, release | One long room, flat ceiling, a few floor thorns. Rebuild of the current Level 1, 3× longer. |
| 2 | Scrolling | A long ceiling run with two gentle rises. |
| 3 | Reel in/out, speed control | Ceiling heights vary 3 u → 6 u → 3 u. |
| 4 | Bounce walls | A zig-zag: three vertical walls that must be bounced, not avoided. Current Level 4 is the right scale and can be adapted. |
| 5 | Shadows | Two overhangs with low exits. |
| 6 | Alleys | Three corridors of decreasing height. |
| 7 | Spikes | Two vertical drops into slots, then a climb out. |
| 8 | Gaps and max-length shots | Open sky with islands. |
| 9 | Wrapping | Pillars and hooks that force the rope to bend around corners. |
| 10 | Everything | 60 s course chaining all of the above, with a shortcut only reachable by a loop. |

Keep "OG Level" as the free-play sandbox; remove it and `SampleScene` from the build list.

---

## 5. Verification

Playtest checklist after Phase 0 and again after Phase 1, with numbers to measure (log them from
a debug overlay):

- Time from LMB down to joint enabled against a ceiling 3 u away: < 0.1 s.
- Scroll cadence: a competent player can sustain 1 rope per second along Level 2's ceiling
  without touching the floor.
- Pumping: holding A/D in time with the swing on a 3 u rope reaches the speed cap within ~4
  swings.
- Wall hit at 15 u/s while roped keeps ≥ 50% of speed and does not fling the snail back past
  the anchor.
- Rope never visibly passes through terrain in Level 9.
- Reset-to-moving latency (R or death): < 0.3 s, no scene load.
- Zero seam bumps rolling across a 20-tile flat floor at 10 u/s.

Automated tests are not worth writing for the physics feel. One EditMode test for the ghost
serialisation round trip and one for the pivot wrap/unwrap maths is enough.

---

## 6. Open questions for the owner

1. Mouse aim vs WA-style angle control. Mouse aim is recommended (keep), but it means the game
   is a "grapple speedrun" rather than a WA replica. Confirm.
2. Hold-to-stay-attached (current) vs press-to-fire / press-to-release (WA). Recommend keeping
   hold, exposed as an option.
3. Delete quick pull and rope-jump as separate verbs (recommended), or keep one of them as a
   deliberate non-WA signature mechanic?
4. Is the thorns-everywhere style of Level 4 the intended aesthetic, or should hazards be rarer
   and terrain itself be the challenge, as in WA rope races?
