# Beach Volleyball

Indie game — a **3rd-person 2v2 action beach volleyball** prototype vs computer bots, in a
tropical tournament. WASD movement, timed jumping attacks, serves, defence and pancakes.

Design doc: [GAME_DESIGN_DOCUMENT](GAME_DESIGN_DOCUMENT) (project root) — the source of truth
for game design.

---

## 1. The vision

- **Genre:** arcade action sport. Sunshine, beach, tropical island vibe.
- **Match:** rally scoring, first to **10 points** advances on a **tournament ladder** vs bots.
- **The core skill is timing.** Every touch (serve, pass, set, spike, block, dig, pancake)
  is a timed press with Perfect / Good / Miss windows.

### Design decisions (revamped 2026-11-20)

Do not "improve" these without asking the user.

1. **Free WASD movement** inside your own half (8 directions), world-relative: W = toward
   the net. (The old two-rail `LaneMover` design was deleted.)
2. **The player controls one character only.** The teammate is a bot, so there is no
   character switching.
3. **Ball = real physics, hand-integrated** in `Ball.cs`: real physics size (Ø 0.21 m, 270 g, `physicsDiameter`) but drawn 2× bigger (Ø 0.42) for readability,
   gravity 9.81 + quadratic air drag, dead-ish bounce on sand. Landing marker uses the same
   step function, so it's exact. No spin/Magnus and no net collision yet.
   **Pass (decided 2026-11-20):** Space is contextual. On the ground with the ball dropping
   toward you, Space *holds to charge* (longer = higher pass) and the hit fires **on
   release**, judged Perfect/Good/Miss by how far the release is from the ball reaching
   forearm height. WASD at release aims the pass; no WASD = straight up. Otherwise Space jumps.
4. **Placeholder art = Unity primitives.** Capsules for characters, cubes/planes for the
   court. Real art gets swapped in later.
5. **3rd-person follow camera** behind the player, looking toward the net (+Z). It follows
   the player's X/Z with smoothing and ignores jumps. It doesn't rotate, so W = toward the net = up on screen.
6. **Scene is authored directly** in `SampleScene.unity` (the user chose this over a
   procedural builder). See conventions below for how to do that safely.

### Controls (planned)

`WASD` move · `Space` pass when the ball is incoming (hold = height, player locked in
place while charging), else jump (spike / block / jump serve) · `K` dive / pancake.
There is no `J` key any more (removed from the GDD 2026-11-20).

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

Top-down view (the camera follows behind the player, looking toward +Z):

```
          z = +8  ── enemy baseline ──
                 ENEMY SIDE (bots)
          z =  0  ═══════ NET ═══════   (net x ±4.3, posts x ±4.6, top at y 2.4)
                 PLAYER SIDE (you)       player starts at (0, 1, -6)
          z = -8  ── your baseline ──
               x = -4            x = +4
```

- **Court:** real beach size, 8 wide × 16 long (8×8 per side), one `CourtSand` plane, no tiles.
- **Player side is negative Z; enemy side is positive Z.** "Forward" = toward the net.
- `PlayerMovement` clamps to `x ∈ [-6, 6]`, `z ∈ [-11, -0.5]`, so the player can run a little
  outside the lines and behind the baseline to serve.

---

## 4. Project structure

- [Assets/Scenes/SampleScene.unity](Assets/Scenes/SampleScene.unity) — the one scene.
  Contains the camera, light, global volume, the **Court** parent (beach, court plane, net,
  2 posts), the **Player** capsule and the **Ball**.
- [Assets/Scripts/PlayerMovement.cs](Assets/Scripts/PlayerMovement.cs) — WASD 8-way movement
  with accel / slide-to-stop, Space jump (hand-rolled gravity), clamped to the player's half.
  All tuning values are Inspector fields.
- [Assets/Scripts/Ball.cs](Assets/Scripts/Ball.cs) — ball flight (gravity + drag), sand
  bounce, landing prediction → `LandingMarker`, respawn above the player 1 s after landing.
- [Assets/Scripts/PlayerPass.cs](Assets/Scripts/PlayerPass.cs) — on Player. Space
  hold-to-charge pass (locks the player in place while charging, `PlayerMovement` reads
  `Charging`), timing grade within `reach` 1.5 m, OnGUI charge bar above the head
  (green → yellow, red at full) + result text top-left.
- [Assets/Scripts/CameraFollow.cs](Assets/Scripts/CameraFollow.cs) — on Main Camera. Keeps the
  scene's start offset to `target` and follows X/Z with `SmoothDamp`. Move the camera in the
  scene to change the offset. Also zooms FOV in while a pass charges (`chargeZoom`) and
  kicks out past normal on release (`releaseKick`), scaled by the charge.
- [Assets/Materials/](Assets/Materials/) — URP/Lit placeholder materials: `Beach`,
  `CourtSand`, `Net` (transparent), `Post`, `Player`, `Ball`.
- `Assets/Settings/` — URP pipeline assets (PC + Mobile renderers), volume profiles.
- `Assets/InputSystem_Actions.inputactions` — Unity template asset, unused (scripts read
  `Keyboard.current` directly).

### Hierarchy (current)

```
Main Camera           (pos (0, 7, -14), euler (25, 0, 0), FOV 50, CameraFollow → Player)
Directional Light
Global Volume
Court
 ├─ Beach            (200×200 sand plane)
 ├─ CourtSand        (8×16 court plane)
 ├─ Net              (translucent dark cube, spans y 1.6–2.4, x ±4.3)
 ├─ NetPostL / NetPostR (x ±4.6, height 2.6)
Player               (blue capsule at (0, 1, -6), PlayerMovement, PlayerPass)
Ball                 (yellow sphere Ø0.42 visual, Ball → Player, LandingMarker)
LandingMarker        (flat yellow disc, no collider, moved by Ball)
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
- ✅ Slice 1 (rebuilt from scratch 2026-11-20): court, net, one player with WASD + jump,
  ball resting on the sand.

Next slices (rough order):
1. ✅ **Timing-based ball** — real-physics flight, landing marker, Space hold-to-charge
   pass with Perfect/Good/Miss, charge bar, camera zoom (2026-11-20). Juggling is done.
   Net collision and ball spin still to do.
2. **Bot teammate** — covers the open half, sets after your pass, goes to the net to block.
3. **Serve**, then **pass / set / spike** (3-touch rally).
4. **Block, dig, dive & pancake.**
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
