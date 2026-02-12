# 📄 The Echo-Thief — Development Documentation

> This document tracks every step taken during development, setup instructions, and technical decisions. It serves as a living log for the team.

---

## Table of Contents

1. [Project Setup History](#-project-setup-history)
2. [File Structure](#-file-structure)
3. [System Architecture](#-system-architecture)
4. [Script Documentation](#-script-documentation)
5. [Unity Setup Guide](#-unity-setup-guide)
6. [Sonar Shader Setup](#-sonar-shader-setup)
7. [Testing Checklist](#-testing-checklist)
8. [Future Work & TODOs](#-future-work--todos)

---

## 📝 Project Setup History

### Step 1 — Repository Initialization

- Created the GitHub repository `ArcadeGame`
- Initialized with a basic `README.md` on `main` branch
- Branch `feat/initial-gameplay` created for all initial development work

### Step 2 — Game Design Document (README.md)

- Wrote a comprehensive game design document covering:
  - **Concept overview** and elevator pitch
  - **Core pillars** (Tension Through Blindness, Elegant Simplicity, Audiovisual Synesthesia, Fair AI)
  - **Player actions** with noise levels, sonar radii, and cooldowns
  - **Objects & pickups** (gems, noise makers, soft shoes, echo bombs, key cards)
  - **Win/lose conditions** and scoring formula
  - **Difficulty progression** across 15 planned levels
  - **Technical architecture** with system diagram
  - **Sonar shader** pseudocode (HLSL) and Shader Graph approach
  - **Guard AI** finite state machine design
  - **Level design guidelines** with layout templates
  - **Audio design** (SFX categories, music state system)
  - **UI/UX** minimal HUD design
  - **10-week development roadmap** (5 phases)
  - **Art style reference** with full color palette
  - **Contributing guidelines** with task division between team members

### Step 3 — Unity .gitignore

- Added a comprehensive `.gitignore` for Unity projects
- Excludes: `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `UserSettings/`
- Excludes: Visual Studio / Rider cache, `.csproj`, `.sln`, `.suo` files
- Excludes: OS-generated files (`.DS_Store`, `Thumbs.db`)

### Step 4 — Core Event System

Created three foundational scripts in `Assets/Scripts/Core/`:

1. **`NoiseEventBus.cs`** — Static event bus that decouples all noise producers from consumers
2. **`GameManager.cs`** — Singleton managing game state, level transitions, and win/lose flow
3. **`ScoreManager.cs`** — Tracks scoring components and computes final score per level

### Step 5 — Sonar System

Created the signature visual system in `Assets/Scripts/Sonar/` and `Assets/Shaders/`:

1. **`SonarPulse.cs`** — Plain data class for a single expanding ring
2. **`SonarManager.cs`** — Manages all active pulses, updates them each frame, pushes data to GPU
3. **`SonarRendererFeature.cs`** — URP ScriptableRendererFeature that injects the sonar post-process pass
4. **`SonarPostProcess.shader`** — Full HLSL shader with depth-based edge detection, expanding neon rings, and fade trails

### Step 6 — Player System

Created player scripts in `Assets/Scripts/Player/`:

1. **`PlayerController.cs`** — Handles sneak/run movement, ping (clap), running auto-pulses, and throwable noise makers
2. **`NoiseEmitter.cs`** — Generic noise component for anything that produces sound (player, throwables, triggers)

### Step 7 — Guard AI System

Created AI scripts in `Assets/Scripts/AI/`:

1. **`GuardStateMachine.cs`** — FSM with states: Patrol → Suspicious → Alerted → Chasing
2. **`GuardHearing.cs`** — Subscribes to NoiseEventBus, calculates hearing range based on distance and loudness
3. **`GuardPatrol.cs`** — Waypoint-based patrol with pause-and-look behavior, supports loop and ping-pong modes

### Step 8 — Environment & UI

Created remaining scripts:

1. **`AmbientNoiseSource.cs`** — Periodic sonar pings from environmental objects (drips, clocks)
2. **`Collectible.cs`** — Gem/artifact/keycard pickup with type enum and scoring integration
3. **`HUDController.cs`** — Minimal HUD: ping cooldown ring, gem counter, noise maker count, alert meter
   - **Note:** Updated to use `UnityEngine.UI` instead of `TMPro` for Unity 6 compatibility.

### Step 9 — Unity 6 Migration & Compilation Fixes

Addressed API changes in **Unity 6 (6000.0+)**:

1.  **SonarRendererFeature.cs**:
    - **Issue:** Using internal `RenderGraphUtils` and missing `AddBlitPass`.
    - **Fix:** Rewrote to use the standard public `RenderGraph` API with `AddRasterRenderPass` and `Blitter.BlitTexture`.
    - **Fix:** Removed explicit `AccessFlags` from `UseTexture`/`SetRenderAttachment` calls as Unity 6 infers them.
2.  **SonarPostProcess.shader**:
    - **Issue:** Redefinition of `_BlitTexture_TexelSize`.
    - **Fix:** Removed manual declaration (already provided by `Blit.hlsl`).
3.  **HUDController.cs**:
    - **Issue:** `TMPro` namespace missing (Unity 6 package restructuring).
    - **Fix:** Replaced `TextMeshProUGUI` with standard `UnityEngine.UI.Text` to resolve compilation without extra dependencies.
4.  **Input System**:
    - **Issue:** `PlayerController` uses legacy `Input` class.
    - **Fix:** Project settings documentation updated to require "Active Input Handling: Both".

### Step 10 — Noise Maker Implementation

Implemented the throwable distraction mechanic:

1.  **`NoiseMaker.cs`** — Script for the physical object. Emits a loud magenta noise event on collision and destroys itself.
2.  **`PlayerController.cs`** — Added `AddNoiseMaker()` for inventory management.
3.  **`Collectible.cs`** — Wired up `CollectibleType.NoiseMaker` to the player's inventory.

---

## 📁 File Structure

```
ArcadeGame/
├── .gitignore
├── README.md                               ← Game design document & roadmap
├── documentation.md                        ← This file
└── Assets/
    ├── Scripts/
    │   ├── Core/
    │   │   ├── NoiseEventBus.cs            ← Static event bus for all noise
    │   │   ├── GameManager.cs              ← Game state + level management
    │   │   └── ScoreManager.cs             ← Scoring logic
    │   ├── Sonar/
    │   │   ├── SonarPulse.cs               ← Single pulse data class
    │   │   ├── SonarManager.cs             ← Pulse lifecycle + GPU data push
    │   │   └── SonarRendererFeature.cs     ← URP renderer feature + render pass
    │   ├── Player/
    │   │   ├── PlayerController.cs         ← Movement, ping, throw
    │   │   ├── NoiseEmitter.cs             ← Generic noise emitter component
    │   │   └── NoiseMaker.cs               ← Thrown distraction object
    │   ├── AI/
    │   │   ├── GuardStateMachine.cs        ← Guard FSM (4 states)
    │   │   ├── GuardHearing.cs             ← Noise detection + perception
    │   │   └── GuardPatrol.cs              ← Waypoint patrol behavior
    │   ├── Environment/
    │   │   ├── AmbientNoiseSource.cs       ← Free ambient sonar pings
    │   │   └── Collectible.cs              ← Collectible items
    │   └── UI/
    │       └── HUDController.cs            ← In-game HUD
    └── Shaders/
        └── SonarPostProcess.shader         ← Sonar HLSL post-process shader
```

---

## 🏗️ System Architecture

### How Systems Connect

```
                    ┌─────────────────┐
                    │  Player Input   │
                    │  (WASD/Space/E) │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │PlayerController │
                    │  - Sneak/Run    │
                    │  - Ping (clap)  │
                    │  - Throw        │
                    └────────┬────────┘
                             │ emits noise
              ┌──────────────▼──────────────┐
              │       NoiseEventBus         │
              │  (static event Action)      │
              └──────┬────────────┬─────────┘
                     │            │
          ┌──────────▼──┐   ┌────▼──────────┐
          │SonarManager │   │ GuardHearing  │
          │ - spawns     │   │ - distance    │
          │   pulses     │   │   check       │
          │ - updates    │   │ - perception  │
          │   GPU data   │   │   error       │
          └──────┬───────┘   └────┬──────────┘
                 │                │
       ┌─────────▼────┐   ┌──────▼───────────┐
       │SonarPostProc │   │GuardStateMachine │
       │   .shader    │   │ Patrol →         │
       │ (renders     │   │ Suspicious →     │
       │  neon rings) │   │ Alerted →        │
       └──────────────┘   │ Chasing          │
                          └──────┬───────────┘
                                │ catches player
                          ┌─────▼─────┐
                          │GameManager│
                          │ GameOver  │
                          └───────────┘
```

### Event Flow for a Player Ping

1. Player presses **Spacebar**
2. `PlayerController.Ping()` creates a `NoiseEvent` (loudness: 0.5, radius: 10, color: cyan)
3. `NoiseEventBus.EmitNoise()` broadcasts to all subscribers
4. **`SonarManager`** receives it → spawns a new `SonarPulse` → pushes data to shader arrays
5. **`SonarPostProcess.shader`** renders the expanding cyan ring against the black void
6. **`GuardHearing`** receives it → checks if distance < hearing range (20 × 0.5 = 10 units) → if yes, feeds perceived position (with error) to `GuardStateMachine`
7. **`GuardStateMachine`** transitions state (e.g., Patrol → Suspicious)
8. **`ScoreManager`** increments ping counter (scoring penalty)

---

## 📜 Script Documentation

### NoiseEventBus (Core)

| Member                  | Type                       | Description                                   |
| ----------------------- | -------------------------- | --------------------------------------------- |
| `OnNoise`               | `event Action<NoiseEvent>` | Subscribe to receive all noise events         |
| `EmitNoise(NoiseEvent)` | static method              | Broadcast a noise event to all subscribers    |
| `ClearAll()`            | static method              | Remove all subscribers (call on scene unload) |

**`NoiseEvent` struct fields:**
| Field | Type | Description |
|---|---|---|
| `Origin` | `Vector3` | World-space position of the noise |
| `Loudness` | `float` | 0–1, affects guard hearing range |
| `SonarRadius` | `float` | Max radius of the sonar ring |
| `SonarColor` | `Color` | Neon color tint of the ring |
| `Source` | `GameObject` | Who produced the noise |

---

### GameManager (Core)

**Singleton.** Manages game state transitions and level flow.

| State           | `Time.timeScale` | Triggered By       |
| --------------- | ---------------- | ------------------ |
| `MainMenu`      | 1                | `LoadMainMenu()`   |
| `Playing`       | 1                | `LoadLevel(int)`   |
| `Paused`        | 0                | `TogglePause()`    |
| `GameOver`      | 0                | `PlayerCaught()`   |
| `LevelComplete` | 1                | `LevelCompleted()` |

---

### SonarManager (Sonar)

**Singleton.** Manages all active `SonarPulse` objects.

- Subscribes to `NoiseEventBus.OnNoise`
- Each frame: updates all pulses, removes expired ones, pushes data to GPU
- GPU data is set via `Shader.SetGlobal*()` — arrays of origins, radii, thickness, fades, colors
- Max 20 simultaneous pulses (configurable)

---

### PlayerController (Player)

| Action | Key          | Noise         | Sonar                  |
| ------ | ------------ | ------------- | ---------------------- |
| Sneak  | WASD         | Silent        | None                   |
| Run    | WASD + Shift | 0.7 loudness  | Blue pulses every 0.4s |
| Ping   | Space        | 0.5 loudness  | Cyan, radius 10        |
| Throw  | E            | 0.8 at impact | Magenta at landing     |

---

### NoiseMaker (Player)

Component on the throwable object prefab.

- **Trigger:** `OnCollisionEnter` (if impact velocity > 2.0)
- **Effect:** Emits a noise event (`Loudness: 0.8`, `Radius: 15`, `Color: Magenta`)
- **Lifecycle:** Destroys itself 0.1s after impact

---

### GuardStateMachine (AI)

| State          | Speed   | Behavior                | Transition                                         |
| -------------- | ------- | ----------------------- | -------------------------------------------------- |
| **Patrol**     | 2 u/s   | Follow waypoints        | → Suspicious (on noise)                            |
| **Suspicious** | 1.5 u/s | Stop, look toward noise | → Alerted (more noise) or → Patrol (4s timeout)    |
| **Alerted**    | 3 u/s   | Walk to noise source    | → Chasing (player nearby) or → Patrol (6s timeout) |
| **Chasing**    | 5 u/s   | Run at player           | → Game Over (catch distance 1.5)                   |

Guards emit their own dim **red-orange sonar pings** when walking, so the player can see them too.

---

### GuardHearing (AI)

- Effective hearing range = `baseRange (20) × noise.Loudness`
- Perception error increases with distance: `error = maxError × (1 - accuracy)`
- Guards ignore their own footstep noise (`Source == gameObject` check)

---

## 🎮 Unity Setup Guide

### Prerequisites

- **Unity 2022.3 LTS** or newer
- **Universal Render Pipeline (URP)** template

### Step-by-Step Setup

#### 1. Create Unity Project

```
Unity Hub → New Project → 3D (URP) → Name: "EchoThief" → Create
```

#### 2. Import Scripts

Close Unity, then copy the folders into the Unity project:

```
Copy: ArcadeGame/Assets/Scripts/  →  UnityProject/Assets/Scripts/
Copy: ArcadeGame/Assets/Shaders/  →  UnityProject/Assets/Shaders/
```

Reopen Unity — scripts will auto-compile.

> **Alternative:** Initialize the Unity project directly inside this repo so files are already in place.

#### 3. Install Packages

Go to **Window → Package Manager**:

- ✅ Universal RP (included with URP template)
- ✅ Cinemachine
- ✅ TextMeshPro (Unity prompts on first use)

#### 4. Configure URP Renderer

1. Find your **URP Renderer** asset (usually in `Settings/`)
2. Click **Add Renderer Feature**
3. Select **Sonar Renderer Feature**
4. Create a new **Material** with shader `EchoThief/SonarPostProcess`
5. Assign the material to the renderer feature

#### 5. Project Settings (Crucial for Unity 6)

1.  **Input Handling**: Go to **Edit → Project Settings → Player → Other Settings**.
    - Set **Active Input Handling** to **Both** (or **Input Manager (Old)**).
    - _Reason:_ `PlayerController.cs` currently uses the legacy `Input` class.
2.  **Tags**: Add a Tag named **Player** and assign it to your Player object.

#### 6. Camera Setup

1. Select the **Main Camera**
2. Set **Background** → Solid Color → `#000000` (pure black)
3. Ensure **Post Processing** is enabled on the camera

#### 6. Build a Test Scene

**Minimum viable test:**

1. Create a **Plane** (floor) — scale to (5, 1, 5)
2. Add some **Cubes** (walls)
3. Create an **empty GameObject** → add `SonarManager` component
4. Create a **Capsule** → tag it `Player`:
   - Add `PlayerController` component
   - Add `Rigidbody` (freeze rotation X, Y, Z)
   - Add `NoiseEmitter` component
5. Position camera above, looking down (top-down view)
6. **Play → Spacebar to ping!**

**Add a guard (optional):**

1. Create another **Capsule**
2. Add: `GuardStateMachine`, `GuardHearing`, `GuardPatrol`, `NavMeshAgent`
3. **Window → AI → Navigation → Bake** the NavMesh
4. Create empty GameObjects as waypoints → assign to guard's patrol list

---

## ✅ Testing Checklist

### Phase 1 — Sonar Prototype

- [ ] Unity project created with URP
- [ ] All scripts compile with zero errors
- [ ] `SonarManager` spawns pulses when `NoiseEventBus.EmitNoise()` is called
- [ ] `SonarPostProcess.shader` renders neon rings expanding from pulse origin
- [ ] Player can move with WASD and ping with Spacebar
- [ ] Sonar rings expand, fade, and disappear correctly
- [ ] Edge detection creates wireframe/outline look on geometry

### Phase 2 — Stealth Loop

- [ ] Guards follow waypoints
- [ ] Guards react to noise (Patrol → Suspicious → Alerted)
- [ ] Guards chase player when close enough
- [ ] Player gets caught → Game Over state triggered
- [ ] Guards emit their own dim red footstep pings
- [ ] Throwable noise makers work as distractions

### Phase 3 — Polish

- [ ] Bloom post-processing applied (neon glow)
- [ ] Screen shake on loud events
- [ ] Audio system with spatial sound
- [ ] Music transitions based on alert state
- [ ] HUD elements functional

---

## 🔮 Future Work & TODOs

### Code TODOs

These are marked with `// TODO:` in the codebase:

| File                      | TODO                                                         | Priority |
| ------------------------- | ------------------------------------------------------------ | -------- |
| `Collectible.cs`          | Wire `NoiseMaker` type to `PlayerController.AddNoiseMaker()` | Medium   |
| `Collectible.cs`          | Implement `KeyCard` inventory system                         | Medium   |
| `Collectible.cs`          | Implement `SoftShoes` temporary buff                         | Low      |
| `Collectible.cs`          | Implement `EchoBomb` special item                            | Low      |
| `PlayerController.cs`     | Migrate from legacy `Input` to new `InputSystem`             | Medium   |
| `SonarRendererFeature.cs` | Consider upgrading to `RTHandle` API for newer URP versions  | Low      |

### Systems Not Yet Built

| System               | Description                                            | Phase   |
| -------------------- | ------------------------------------------------------ | ------- |
| **Laser Tripwires**  | Invisible until sonar reveals; crossing triggers alarm | Phase 4 |
| **Security Cameras** | Sweeping cone; alerts guards if player detected        | Phase 4 |
| **Locked Doors**     | Require key cards to open                              | Phase 4 |
| **Guard Flashlight** | Cone-shaped trigger in chase state                     | Phase 4 |
| **Main Menu**        | Dark background with ambient sonar pulses              | Phase 4 |
| **Level Select**     | Museum floor plan layout                               | Phase 4 |
| **Pause Menu**       | Frosted glass overlay                                  | Phase 4 |
| **Settings Menu**    | Volume, controls                                       | Phase 5 |

---

> _Last updated: February 10, 2026_
