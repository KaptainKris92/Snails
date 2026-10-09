# Snails: art asset prompts

Prompts for an image model (GPT Image, DALL·E, Midjourney, etc.) to replace the placeholder art.
Every prompt shares one style block so the pieces match. Paste the style block first, then the
asset prompt.

## Technical constraints (apply to every image)

- Output: PNG with a transparent background. No drop shadows outside the silhouette, no vignette,
  no text or watermark.
- Square canvases at 1024×1024 unless stated. Downscale in Unity, do not upscale.
- The game imports at 512 pixels per unit for tiles and 256 for the shell. Tiles are 0.5 world
  units (256 px), the shell is 1 unit (256 px at 256 PPU, then scaled 0.5 in the prefab).
- Side view, orthographic (no perspective), light from the top-left.
- Flat vector-cartoon look with a 6–8 px dark outline, two-tone cel shading, a slightly textured
  fill. Saturated but not neon. Palette: grass #4FA64F / #2E7D32, soil #6B4423 / #4E3119, stone
  #8E8A82, snail shell #7A3B2E with a #3B1F1A spiral, snail body #B9D86A with #6E8F2F accents,
  thorns #8B2630 with #CFCFCF spikes, finish flag #F2C94C.

Style block to prepend to every prompt:

> Flat vector cartoon game sprite, clean thick dark outline, two-tone cel shading, subtle hand-drawn
> texture, side view, orthographic, lit from the top-left, transparent background, no text, centred
> on the canvas, consistent with a cheerful 2D platformer.

## Character: the snail

The snail is the player. Its shell is a rigid rolling ball; its soft body stretches out of the
shell to grab terrain (the grapple) or shove off it (the push-off). Only the shell is drawn as a
sprite; the body is drawn as a stretched rope in-engine, so the body texture must tile.

1. **Shell (idle)**: "A round snail shell seen exactly from the side, a perfect circle silhouette
   filling 92% of the canvas, brown with a darker spiral that winds to the centre, a few lighter
   speckles, small highlight at the top-left. No body, no eyes." → `BasicShell.png`, 512×512.
2. **Shell crumble sheet**: "The same snail shell broken into 16 irregular jagged shards arranged
   in a 4×4 grid, each shard a piece of the original shell with matching spiral pattern and
   outline, gaps between the shards, transparent background." → `BasicShell_Crumble.png`,
   1024×1024, sliced 4×4 in the Sprite Editor.
3. **Body rope texture**: "A horizontal seamless tileable strip of a snail's soft body, pale
   yellow-green with faint darker segment rings every 64 px, slightly glossy, slime highlights
   along the top edge, tiles perfectly left to right." → `BodySprite.png`, 512×128, import with
   wrap mode Repeat; used on the LineRenderer material.
4. **Head (rope tip)**: "A snail's head seen from the side facing right: a soft rounded snout, two
   eye stalks with round black eyes looking forward, a tiny smile, pale yellow-green with the same
   outline as the body strip." → `SnailHead.png`, 256×256, pivot at the left edge so it can sit at
   the end of the rope.
5. **Eyes on the shell (optional)**: "Two small snail eye stalks poking out of the top of a shell,
   isolated on transparent, same style." → `ShellEyes.png`, 256×128.

## Terrain tiles

All tiles are 256×256 squares that butt against each other with no visible seams. Ask for each as
a separate image (models blur details when asked for sheets). Edge tiles must be flat-cut at the
canvas edge so they tile.

6. **Dirt (interior) ×3**: "A square tile of packed brown soil with a few small pebbles and faint
   curved strata, edges cut flat so it tiles seamlessly on all four sides, no border, no grass."
   → `DirtSquare1..3.png`.
7. **Rocky dirt ×3**: "A square tile of brown soil with two or three embedded grey-tan rocks, edges
   cut flat so it tiles seamlessly, no grass." → `DirtRockSquare1..3.png`.
8. **Grass top**: "A square dirt tile whose top 35% is a bright green grass surface with a slightly
   uneven blade silhouette along the top edge; sides and bottom cut flat so it tiles." →
   `Half-Grass_Up.png`. Then the same prompt rotated in words for **bottom** ("grass hanging along
   the bottom edge"), **left** and **right** → `Half-Grass_Down/Left/Right.png`.
9. **Grass outer corners ×4**: "A square dirt tile with grass along the top and left edges meeting
   in a rounded outer corner, other two edges cut flat." → `CornerGrass_UL.png`, and UR, BL, BR.
10. **Full grass block**: "A square tile entirely covered in green grass seen from the side, dense
    blades, cut flat on all edges so it tiles." → `GrassBlock.png`.
11. **Slopes ×4**: "A square tile that is solid dirt-with-grass in its top-left half and fully
    transparent in the bottom-right half, split by a perfectly straight diagonal from the top-right
    corner to the bottom-left corner; grass runs along the diagonal edge." → `RATriangle_BR.png`
    (the name is the empty corner). Repeat for the other three corners.
12. **Thorns**: "A clump of dark red bramble with sharp grey-white thorns, roughly round, filling the
    tile, clearly dangerous, transparent background." → `RedThorns.png`. Also a half-size variant.

## Level objects

13. **Finish zone**: "A checkered racing flag on a short pole planted in a small grass tuft, bright
    yellow and black, waving slightly, 256×512 portrait." → `EndZone.png`.
14. **Background**: "A soft, low-contrast parallax backdrop for a cartoon platformer: pale sky
    gradient from #BFE4FF at the top to #E8F6FF at the bottom with a few distant rounded hills in
    muted green and faint clouds, 2048×1024, no outline, tiles horizontally." Use as a stretched
    single sprite behind each level.
15. **Grapple hit dust**: "A tiny burst of 5 small pale dust puffs around an impact point, flat
    cartoon style, 128×128 sprite sheet of 4 frames." For a hit-flash particle later.

## UI

16. **Level button**: "A chunky rounded rectangle button in warm wood tones with a lighter bevel,
    empty centre, 512×256, 9-slice safe (uniform edges)." Import with a 64 px border and use as a
    sliced sprite.
17. **Panel**: "A parchment-style rounded panel with a thin dark outline, 1024×768, 9-slice safe."
18. **Icon**: "App icon of a cartoon snail shell rolling, bold outline, 1024×1024."

## How to drop them in

- Keep the file names above so existing prefabs and tiles pick the new art up automatically.
- Tiles: set Pixels Per Unit 512, Filter Mode Bilinear, Compression None, Mesh Type Full Rect,
  and regenerate the physics shape (Sprite Editor → Custom Physics Shape) for the slopes and
  thorns if the auto outline looks wrong.
- After replacing tile art, run **Snails → Build All Levels From Text** to refresh generated
  levels.
