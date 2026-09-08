# Converting AAZ-Mobile to HD-2D

## What HD-2D actually is

It is not a shader you switch on. It is four decisions working together:

1. **Sprites become objects in a 3D world.** Characters stay hand-drawn pixel art,
   but they are lit, they cast and receive shadows, and they occlude and are
   occluded by real geometry. Sorting layers stop deciding depth; the depth buffer does.
2. **Environments become built sets.** Floors, walls and props are low-poly geometry
   textured with the existing tile art. The tiles are the same pixels; they are just
   standing up in space now.
3. **The camera is a long lens looking down.** A narrow field of view (roughly
   20–35°) at a 30–45° tilt. The compression is what makes the scene read as a
   physical model rather than a room.
4. **The post stack sells the miniature.** Tilt-shift depth of field, generous bloom
   on emissive sources, ACES tonemapping, a slight contrast and saturation lift.

Items 1, 3 and 4 are content-independent and are already implemented in
`Packages/com.aaz.hd2d`. Item 2 is the real work, and it is the part that needs the
project in hand.

## Staged plan

### Stage 0 — Audit (needs the project)

- Unity and URP versions; whether the project is already on URP or still Built-in.
  A Built-in project needs a pipeline migration before anything else here applies.
- How sprites are authored: `SpriteRenderer` + `Animator`, a Tilemap, an atlas, a
  custom batching system. This decides how mechanical the conversion can be.
- Pixels-per-unit, and whether Pixel Perfect Camera is in use — it conflicts with a
  perspective HD-2D camera and has to be removed, with its job handed to the rig's
  pixel snapping.
- Current frame budget and the target device tier.

### Stage 1 — Pipeline

Move to URP if needed, then run **Window → AAZ → HD-2D Setup**. Depth texture on,
HDR on, opaque texture off. Verify nothing in the existing game reads the opaque
texture.

### Stage 2 — Characters

Convert `SpriteRenderer` materials to `AAZ/HD2D/Sprite Lit`. Animator-driven sprite
swaps keep working because the shader reads `_MainTex`. Tune Light Wrap, Normal Bend
and Ambient Tint per character class, then handle the shadow question — see the
billboard-shadow limitation in the package README.

The thing to watch here is anything that depended on sorting order for depth. Those
need moving in Z.

### Stage 3 — Environments

This is the expensive stage and the one that decides whether it looks good.

- Floors: existing tilemaps become a flat mesh or a tiled quad, lying in the XZ plane.
- Walls and cliffs: extrude tile edges into real geometry so they catch light and
  cast shadows down onto the floor.
- Props: the ones that should be occluders (pillars, crates, trees) become billboards
  or simple meshes placed in 3D; the rest can stay flat on the floor plane.
- Lighting: one warm directional key with shadows, plus point lights on every
  emissive source — lanterns, windows, fires. The point lights are what make an
  HD-2D town at night look like an HD-2D town at night.

Doing one representative area end-to-end first is worth more than converting
everything shallowly.

### Stage 4 — Look development

Author volume profiles per area and blend them with local volumes. Tilt-shift band
tight in towns, wide or off in combat where the whole field has to stay readable.

### Stage 5 — Mobile performance

## Mobile budget

This is a mobile title, so the HD-2D checklist has to be read against a tile-based
GPU. In rough order of what will hurt:

| Cost | Mitigation |
| --- | --- |
| **Tilt-shift blur is fillrate** — it is several full-screen and half-screen passes | Raise `downsample` to 3–4 and drop `iterations` to 1 on low tiers. The blur is blurry; nobody can tell. |
| **Real-time shadows** | One shadow-casting directional light, one cascade, a tight shadow distance (the camera only sees a small area anyway), soft shadows off. |
| **HDR + post is bandwidth** | HDR is required for bloom, but keep MSAA low and let Alpha To Coverage do edge softening only where it earns its place. |
| **Additional lights** | Cap per-object lights. Forward+ helps a lot here if the URP version supports it. |
| **Uncompressed pixel-art textures** | Correct for quality, expensive for memory. Keep them uncompressed but keep atlases tight and pack aggressively. |
| **Overdraw from billboards** | Sprites are AlphaTest with depth write, so they are cheap to occlude but alpha test defeats early-Z on some mobile GPUs. Keep sprite quads trimmed to the sprite's tight mesh. |

The honest expectation: HD-2D on mobile means picking two or three of bloom,
tilt-shift, real-time shadows and many point lights, not all four, and scaling that
choice by device tier.

## Open questions for once the project is readable

- Is it 2D-physics based? Moving characters into a 3D world with a depth buffer does
  not require changing physics, but it does require deciding whether the floor plane
  is XY or XZ, and that choice touches movement code.
- Is there an existing camera controller to replace or wrap?
- What is the lowest device that has to hold frame rate?
