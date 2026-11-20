# Beach Volleyball

Indie game — an **isometric 2v2 action beach volleyball** prototype vs computer bots, in a
tropical tournament. WASD movement, timed jumping attacks, serves, defence and pancakes.

Design doc: [GAME_DESIGN_DOCUMENT](GAME_DESIGN_DOCUMENT) (project root) — the source of truth
for game design. Field sketch: [Assets/image.png](Assets/image.png).

---

## 1. The vision

- **Genre:** arcade action sport. Sunshine, beach, tropical island vibe.
- **Match:** rally scoring, first to **10 points** advances on a **tournament ladder** vs bots.
- **The core skill is timing.** Every touch (serve, pass, set, spike, block, dig, pancake)
  is a timed press with Perfect / Good / Miss windows.

### Design decisions (revamped 2026-11-20)

Do not "improve" these without asking the user.

1. **Free WASD movement** inside your own half (8 directions). Replaces the old two-rail
   lane design — `LaneMover.cs` and its W/S + ↑/↓ controls are now obsolete.
2. **One active character at a time**, auto-switching to the teammate best placed for the
   ball, with a manual swap key (Tab). The inactive teammate is AI-positioned.
   (Still an open question in the GDD — confirm before building.)
3. **Timing-based arcade ball.** Preset arcs, not a physics sim. Shadow + landing marker
   always show where the ball comes down. (Not built yet.)
4. **Placeholder art = Unity primitives.** Capsules for characters, cubes/planes for the
   court. Real art gets swapped in later.
5. **Perspective-tilt isometric camera** (not true orthographic).
6. **Scene is authored directly** in `SampleScene.unity` (the user chose this over a
   procedural builder). See conventions below for how to do that safely.

### Controls (planned)

`WASD` move · `Space` jump (spike / block / jump serve) · `J` hit (serve, pass, set, spike —
context by touch number and air state) · `K` dive / pancake · `Tab` swap character.

---

## 2. Tech stack

- **Unity 6** — `6000.4.8f1`.
- **URP** (Universal Render Pipeline) `17.4.0`. Use the **URP/Lit** shader for materials
  (`guid: 933532a4fcc9baf4fa0491de14d08ed7`), not the legacy Standard shader.
- **New Input System** `1.19.0`, and the project is set to **New only**
  (`activeInputHandler: 1` in ProjectSettings).
  - ⚠️ **Legacy `Input.GetKey` / `Input.GetAxis` will throw at runtime.** Always use
    `UnityEngine.InputSystem` (e.g. `Keyboard.current[Key.W].isPressed`).

---

## 3. Court layout & coordinate system

Top-down view (the camera looks in from the player's back-left corner):

```
                ENEMY SIDE (z+, bots)
     EB1(-1,+3)   EB2(+1,+3)    back row
     EF1(-1,+1)   EF2(+1,+1)    forward row
     ============ NET (z = 0) ============
     PF1(-1,-1)   PF2(+1,-1)    forward row
     PB1(-1,-3)   PB2(+1,-3)    back row   <- characters start here
                PLAYER SIDE (z-, you)
       lane 1         lane 2
```

- **Tile size:** 2 units. **Lanes (X):** lane 1 at `x = -1`, lane 2 at `x = +1`.
- **Rows (Z):** back `±3`, forward `±1`. **Net** is at `z = 0`.
- **Player side is negative Z; enemy side is positive Z.** "Forward" for a player means
  moving toward the net (Z increasing toward 0).
- Characters move freely in their own half (x within the court, z from the baseline to just
  short of the net).

---

## 4. Project structure

- [Assets/Scenes/SampleScene.unity](Assets/Scenes/SampleScene.unity) — the one scene.
  Contains the camera, light, global volume, the **Court** parent (ground, 8 tiles, net
  band, 2 posts) and the **Players** parent (Player1, Player2 capsules).
- [Assets/Scripts/LaneMover.cs](Assets/Scripts/LaneMover.cs) — per-character lane movement.
  Attached to both capsules; differs only by `playerIndex` (1 or 2).
- [Assets/Materials/](Assets/Materials/) — URP/Lit placeholder materials: `SandLight`,
  `SandDark`, `GroundSand`, `NetWhite`, `PostWood`, `Player1`, `Player2`.
- `Assets/Settings/` — URP pipeline assets (PC + Mobile renderers), volume profiles.
- `FirstScript.cs` — leftover empty template, unused. Safe to delete.

### Hierarchy (current)

```
Main Camera           (perspective tilt: pos ~(-9.27, 9.18, -9.27), euler (35, 45, 0), FOV 40)
Directional Light
Global Volume
Court
 ├─ Beach            (large sand plane)
 ├─ PB1 PB2 PF1 PF2  (player tiles, checkered SandLight/SandDark)
 ├─ EF1 EF2 EB1 EB2  (enemy tiles)
 ├─ NetBand
 ├─ NetPostL / NetPostR
Players
 ├─ Player1          (blue capsule, lane 1, LaneMover playerIndex 1)
 └─ Player2          (orange capsule, lane 2, LaneMover playerIndex 2)
```

---

## 5. Conventions for editing the scene by hand

The scene is hand-authored YAML, so be careful:

- **fileIDs for generated/added objects use the `17000000xx` range** to stay clear of
  Unity's existing anchors (camera/light/volume are in the 330M–832M range). Never reuse an
  anchor — every `--- !u!T &id` must be unique.
- **Primitive meshes** reference the built-in default resources
  (`guid: 0000000000000000e000000000000000`): Cube `10202`, Plane `10209`, Capsule `10208`,
  Sphere `10207`, Cylinder `10206`.
- **Materials** are referenced as `{fileID: 2100000, guid: <mat-guid>, type: 2}`.
- A custom script component references its `.cs.meta` guid via
  `m_Script: {fileID: 11500000, guid: <script-guid>, type: 3}`. Serialized fields that
  aren't written to YAML deserialize to **type default (0)**, *not* the C# initializer —
  so always write every field you care about into the MonoBehaviour block.
- Root objects must be listed under `SceneRoots.m_Roots`. Parented objects need both the
  child's `m_Father` and the parent's `m_Children` list.
- **After any hand-edit, validate** before considering it done: no duplicate anchors, every
  `component`/`m_Father`/material reference resolves, and material guids exist on disk. (A
  quick Python regex pass over the file catches these.)

---

## 6. Roadmap

Built so far:
- ✅ Isometric court + camera (visual foundation).
- ⚠️ Two-rail lane movement (`LaneMover`) — obsolete after the revamp, to be replaced.

Next slices (rough order):
1. **WASD movement + active-character switching** — replaces `LaneMover`.
2. **Timing-based ball** — preset arcs, shadow + landing marker, Perfect/Good/Miss hits.
3. **Serve**, then **pass / set / spike** (3-touch rally).
4. **Jump, block, dig, dive & pancake.**
5. **Enemy bot AI** — same moves and timing windows as the player.
6. **Scoring** — rally scoring, first to 10 wins the match.
7. **Tournament ladder** — advance through bot opponents.
8. **Art pass** — swap primitives for real models/sprites and tropical-beach styling.

---

## 7. Working agreement

- This is a **prototype**: favor simple, readable, feel-first implementations over
  architecture. Keep placeholders obviously placeholder.
- The user playtests in the Unity editor (Claude builds headless and can't feel the game),
  so expose tuning values as Inspector fields and call out anything that needs a feel check.
- When a design choice is genuinely open, **ask** rather than assume — the timing-based
  action feel is the heart of the game and shouldn't drift.
