# AAZ HD-2D

A drop-in HD-2D rendering layer for Unity URP. It supplies the three things that
separate "a 2D game" from "an HD-2D game":

| Piece | File | What it does |
| --- | --- | --- |
| Lit billboard sprite | `Runtime/Shaders/HD2D_Sprite.shader` | Pixel-art sprites that face the camera, receive real-time light and shadow, cast shadows, and sort against 3D geometry |
| Tilt-shift depth of field | `Runtime/HD2DTiltShiftFeature.cs` | The horizontal band of focus that makes a scene read as a miniature diorama |
| Diorama camera | `Runtime/HD2DCameraRig.cs` | Fixed-tilt, narrow-FOV follow camera with optional pixel-grid snapping |

## Requirements

- Unity 2022.3 LTS or newer
- Universal Render Pipeline 14.0 or newer

Both the Render Graph path (Unity 6 default) and the older compatibility path are
implemented, so the renderer feature works either way.

## Install

Copy `com.aaz.hd2d` into your project's `Packages/` folder, or add it via
Package Manager → *Add package from disk*.

## Setup

Open **Window → AAZ → HD-2D Setup** and work down the three sections:

1. **Render pipeline** — adds the tilt-shift renderer feature to every renderer on
   the active URP asset, and switches on the settings the look depends on
   (depth texture, HDR).
2. **Post processing** — writes a starting volume profile with tilt-shift, bloom,
   ACES tonemapping, a small contrast/saturation lift and a vignette. Assign it to
   a global `Volume` in your scene.
3. **Sprites** — select a hierarchy and convert its `SpriteRenderer`s. One material
   is created per source texture, shadow casting is switched on, and pixel-art
   import settings are applied.

Then add `HD2DCameraRig` to your main camera and point it at the player.

## How it fits an existing 2D project

The sprite shader deliberately reads `_MainTex`, which is the property
`SpriteRenderer` binds. That means you keep your existing `SpriteRenderer`s,
`Animator` sprite swaps, sprite atlases and flipbook animation — only the
material changes. You do not have to rebuild characters as meshes.

Two consequences worth knowing:

- **Sprites render in the AlphaTest queue with depth write on.** That is what lets
  a character walk behind a 3D pillar and be correctly occluded. The cost is that
  sprite edges are hard-clipped rather than alpha-blended; enable *Alpha To
  Coverage* on the material and MSAA in the URP asset if you want them softened.
- **Sorting layers no longer decide what is in front.** Depth does. Anything that
  relied on sorting order for depth (a foreground bush drawn over the player) has
  to be moved in Z instead.

## Material settings that matter

| Property | Why |
| --- | --- |
| **Billboard Mode** | `YAxis` is the default and almost always right — yaw-only rotation keeps vertical pixel columns vertical. `Full` tilts with the camera and will shear pixel art. |
| **Light Wrap** | Pure Lambert turns half of a sprite black and it reads as cardboard. Wrapping keeps hand-authored colour visible in shadow. |
| **Normal Bend** | Fakes cylindrical curvature across the sprite so a flat card gets a soft terminator and a usable rim light. Without it a billboard is uniformly lit. |
| **Ambient Tint** | The cool fill against a warm key light. This single value does much of the work of making a scene look like a lit diorama. |
| **Vertical Tilt** | Leans the card back a few degrees so overhead lights graze it and the top edge clears low ceilings. |

## Known limitation: billboard shadows

A camera-facing card casts a shadow shaped like a card. With a low sun the shadow
collapses to a sliver, and with the sun behind the camera it becomes a blob the
size of the sprite. Both are wrong.

The shader already orients the shadow pass to the *player's* camera rather than the
light, so the silhouette at least matches what is on screen. For characters that
need a grounded shadow, the standard HD-2D answer is a separate blob-shadow decal
or a low-poly proxy mesh set to *Shadows Only*, with the sprite itself set to
**Disable Received Shadows**.
