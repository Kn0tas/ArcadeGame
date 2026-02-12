# 🦇 The Echo-Thief

**A Stealth-Action Arcade Game Built in Unity**

> _You are invisible, but you are also blind._

---

## 📖 Table of Contents

1. [Concept Overview](#-concept-overview)
2. [Core Pillars](#-core-pillars)
3. [Game Design Document](#-game-design-document)
4. [Technical Architecture](#-technical-architecture)
5. [The Sonar Shader System](#-the-sonar-shader-system)
6. [AI & Guard System](#-ai--guard-system)
7. [Level Design Guidelines](#-level-design-guidelines)
8. [Audio Design](#-audio-design)
9. [UI/UX Design](#-uiux-design)
10. [Project Structure](#-project-structure)
11. [Development Roadmap](#-development-roadmap)
12. [Tools & Setup](#-tools--setup)
13. [Art Style Reference](#-art-style-reference)
14. [Contributing](#-contributing)
15. [Documentation](#-documentation)

---

## 🚦 Current Status

> **Phase 1 — Prototype** is ready to begin in Unity.

All core scripts and the sonar shader have been scaffolded. The codebase compiles and is ready to be imported into a Unity URP project.

| System                 | Status      | Key Files                                                                                |
| ---------------------- | ----------- | ---------------------------------------------------------------------------------------- |
| **Project Setup**      | ✅ Complete | `.gitignore`                                                                             |
| **Core Event System**  | ✅ Complete | `NoiseEventBus.cs`, `GameManager.cs`, `ScoreManager.cs`                                  |
| **Sonar System**       | ✅ Complete | `SonarPulse.cs`, `SonarManager.cs`, `SonarRendererFeature.cs`, `SonarPostProcess.shader` |
| **Player**             | ✅ Complete | `PlayerController.cs`, `NoiseEmitter.cs`                                                 |
| **Guard AI**           | ✅ Complete | `GuardStateMachine.cs`, `GuardHearing.cs`, `GuardPatrol.cs`                              |
| **Environment**        | ✅ Complete | `AmbientNoiseSource.cs`, `Collectible.cs`                                                |
| **UI**                 | ✅ Complete | `HUDController.cs`                                                                       |
| **Unity Project Init** | ✅ Complete | Compiles in Unity 6 (RenderGraph API)                                                    |
| **Test Scene**         | ✅ Complete | Basic TestRoom scene created                                                             |
| **Art / Audio**        | ⬜ Pending  | Phase 3                                                                                  |

> 📄 **For full setup instructions and development history, see [documentation.md](documentation.md).**

---

## 🎯 Concept Overview

**Genre:** Stealth-Action Arcade  
**Engine:** Unity (URP — Universal Render Pipeline)  
**Perspective:** Top-down 2.5D  
**Art Style:** Neon-Noir — minimal, heavy on shadows and pulse-waves  
**Target Platforms:** PC (Windows/Mac), with potential for WebGL

### The Elevator Pitch

You're a thief breaking into a pitch-black museum. The world is invisible to you — **until you make noise**. Every clap, every footstep, every thrown object sends out a **sonar pulse** that reveals the environment for a brief moment. But the guards hear you too. Balance _seeing_ with _staying hidden_.

### What Makes It Unique

| Mechanic                   | Description                                                                               |
| -------------------------- | ----------------------------------------------------------------------------------------- |
| **Sonar Vision**           | The world is only visible through expanding pulse-wave rings triggered by sound           |
| **Noise = Sight = Danger** | Every action that lets you see also alerts enemies                                        |
| **Risk-Reward Loop**       | Players constantly choose between navigating blind or revealing themselves                |
| **Minimalist Aesthetic**   | Pure black world with neon sonar outlines — no textures needed, just geometry and shaders |

---

## 🏛️ Core Pillars

1. **Tension Through Blindness** — The default state is total darkness. Players _earn_ vision by accepting risk.
2. **Elegant Simplicity** — Few mechanics, deep interactions. No skill trees, no inventory management.
3. **Audiovisual Synesthesia** — Sound _is_ sight. Every audio event has a visual counterpart.
4. **Fair AI** — Guards follow clear, readable rules. The player should always understand _why_ they were caught.

---

## 🎮 Game Design Document

### Player Actions

| Action                | Input                        | Noise Level                | Sonar Radius       | Cooldown |
| --------------------- | ---------------------------- | -------------------------- | ------------------ | -------- |
| **Sneak (Move Slow)** | Left Stick / WASD            | 🔇 Silent                  | None               | —        |
| **Run**               | Left Stick + Sprint / Shift  | 🔊 Loud                    | Large (auto-pulse) | —        |
| **Clap / Ping**       | Button A / Spacebar          | 🔉 Medium                  | Medium             | 1.5s     |
| **Throw Object**      | Button B / E (aim + release) | 🔊 Loud (at landing point) | Large (at impact)  | —        |
| **Interact / Steal**  | Button X / F                 | 🔇 Silent                  | Tiny glow          | 0.5s     |

### Objects & Pickups

| Object           | Purpose                                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------------------------- |
| **Coins / Gems** | Primary collectible. Score-based. Each level has a set number to find.                                  |
| **Noise Makers** | Throwable objects (stones, marbles). Create a sonar ping at the point of impact — used as distractions. |
| **Soft Shoes**   | Temporary power-up. Running no longer creates noise for 10 seconds.                                     |
| **Echo Bomb**    | Rare item. Creates a massive sonar pulse that reveals the entire room but also stuns guards briefly.    |
| **Key Cards**    | Required to open certain locked exhibit rooms.                                                          |

### Win / Lose Conditions

- **Win:** Steal the target artifact(s) and reach the exit.
- **Lose:** Get caught by a guard (they touch you or shine a flashlight on you while you're in range).
- **Bonus:** Collect all optional gems, complete under a par time, or finish with zero pings (ghost run).

### Scoring System

```
Final Score = Artifacts Stolen × 1000
            + Gems Collected × 100
            - Pings Used × 10
            + Time Bonus (if under par)
            + Ghost Bonus (if 0 pings — only movement-based sonar)
```

### Difficulty Progression

| Level Range | New Mechanic Introduced                                     |
| ----------- | ----------------------------------------------------------- |
| **1–3**     | Basic movement, clap/ping, stationary guards                |
| **4–6**     | Patrolling guards, throwable noise makers                   |
| **7–9**     | Guard flashlights (cone vision), locked doors + key cards   |
| **10–12**   | Security cameras, laser tripwires (make noise if triggered) |
| **13–15**   | Multiple floors, elevators (make noise), boss-level heist   |

---

## 🔧 Technical Architecture

### High-Level System Diagram

```
┌─────────────────────────────────────────────────────┐
│                    GAME MANAGER                      │
│  (Game State, Score, Level Loading, Pause/Resume)    │
└─────────────┬───────────────────────┬───────────────┘
              │                       │
    ┌─────────▼─────────┐   ┌────────▼────────────┐
    │   PLAYER SYSTEM    │   │   LEVEL SYSTEM       │
    │  - Movement        │   │  - TileMap / Layout   │
    │  - Input Handler   │   │  - Object Placement   │
    │  - Noise Emitter   │   │  - Spawn Points       │
    │  - Inventory       │   │  - Exit / Objectives  │
    └─────────┬──────────┘   └────────┬──────────────┘
              │                       │
    ┌─────────▼──────────────────────▼───────────────┐
    │              SONAR SYSTEM                       │
    │  - Pulse Manager (spawn, expand, fade)          │
    │  - Sonar Shader (URP Shader Graph / HLSL)       │
    │  - Noise Event Bus (decoupled event system)     │
    └─────────┬──────────────────────┬───────────────┘
              │                       │
    ┌─────────▼─────────┐   ┌────────▼────────────┐
    │    AUDIO SYSTEM    │   │    AI / GUARD SYSTEM │
    │  - SFX Manager     │   │  - State Machine     │
    │  - Spatial Audio   │   │  - Patrol Paths      │
    │  - Music Manager   │   │  - Hearing System    │
    │                    │   │  - Alert Propagation  │
    └────────────────────┘   └──────────────────────┘
```

### Core Unity Packages Required

| Package                             | Purpose                                              |
| ----------------------------------- | ---------------------------------------------------- |
| **Universal Render Pipeline (URP)** | Required for custom sonar shader and post-processing |
| **Shader Graph**                    | Visual shader authoring for the sonar effect         |
| **Input System (New)**              | Modern input handling for cross-platform support     |
| **Cinemachine**                     | Smooth camera following and screen shake             |
| **TextMeshPro**                     | UI text rendering                                    |
| **2D Tilemap** (optional)           | If using tile-based level design                     |
| **ProBuilder** (optional)           | If building 3D level geometry                        |

### Scene Structure

```
📁 Scenes/
├── MainMenu.unity
├── LevelSelect.unity
├── Level_01.unity
├── Level_02.unity
├── ...
├── GameOver.unity
└── Credits.unity
```

---

## 🌊 The Sonar Shader System

This is the **signature visual** of the game. Everything hinges on this system working beautifully.

### How It Works

1. Player triggers a **Noise Event** (clap, run, throw).
2. The **Sonar Manager** spawns a new **Sonar Pulse** at the noise origin.
3. The pulse expands outward as a ring (defined by inner radius, outer radius, and expansion speed).
4. A **full-screen post-processing shader** (or per-object shader) checks each pixel's world position against all active pulses.
5. If a pixel falls _inside_ the ring band of any active pulse → **render it** with a neon outline. Otherwise → **render black**.
6. The pulse fades over time and is destroyed.

### Shader Pseudocode (HLSL / Shader Graph)

```hlsl
// Sonar Post-Process Shader (simplified)
float4 SonarEffect(float3 worldPos, SonarPulse[] activePulses)
{
    float visibility = 0;

    for (int i = 0; i < activePulses.Length; i++)
    {
        float dist = distance(worldPos, activePulses[i].origin);
        float radius = activePulses[i].currentRadius;
        float thickness = activePulses[i].ringThickness;

        // Check if pixel is inside the sonar ring band
        float ring = smoothstep(radius - thickness, radius, dist)
                   - smoothstep(radius, radius + thickness, dist);

        // Fade based on pulse age
        float fade = 1.0 - (activePulses[i].age / activePulses[i].maxAge);

        visibility += ring * fade;
    }

    // Clamp and apply neon color
    visibility = saturate(visibility);
    float4 neonColor = float4(0.0, 0.8, 1.0, 1.0); // Cyan neon
    return lerp(float4(0,0,0,1), neonColor, visibility);
}
```

### Sonar Pulse Properties

| Property         | Type    | Default | Description                                  |
| ---------------- | ------- | ------- | -------------------------------------------- |
| `origin`         | Vector3 | —       | World-space position of the noise event      |
| `currentRadius`  | float   | 0       | Expands over time                            |
| `maxRadius`      | float   | 15      | Maximum reach of this pulse                  |
| `expansionSpeed` | float   | 12      | Units per second                             |
| `ringThickness`  | float   | 1.5     | Width of the visible ring band               |
| `age`            | float   | 0       | Time since spawn                             |
| `maxAge`         | float   | 2.5     | Seconds before full fade-out                 |
| `color`          | Color   | Cyan    | Neon tint of this pulse (can vary by source) |

### Sonar Color Palette by Source

| Source                     | Color      | Hex       |
| -------------------------- | ---------- | --------- |
| Player Clap                | Cyan       | `#00E5FF` |
| Player Footstep (run)      | Blue       | `#2979FF` |
| Thrown Object Impact       | Magenta    | `#FF00FF` |
| Guard Footstep             | Red/Orange | `#FF3D00` |
| Echo Bomb                  | White      | `#FFFFFF` |
| Ambient Drip / Environment | Dim Green  | `#00E676` |

### Implementation Steps

1. **Create a `SonarPulse` C# class** — holds origin, radius, speed, age, color.
2. **Create a `SonarManager` MonoBehaviour** — maintains a list of active pulses, updates them each frame, removes expired ones.
3. **Create a `NoiseEmitter` component** — attached to anything that makes noise. Fires an event on the `NoiseEventBus`.
4. **Create the URP Sonar Shader** via Shader Graph or hand-written HLSL:
   - Receives pulse data via a structured buffer or material property arrays.
   - Applied as a **post-process Renderer Feature** in URP.
5. **Fallback approach:** If post-processing is too complex initially, apply the shader **per-object** using a shared material that receives pulse data as global shader variables (`Shader.SetGlobalFloatArray`, etc.).

### Sonar Shader — Shader Graph Approach

```
Nodes Overview:
──────────────
[World Position] → [Distance to Pulse Center]
                         │
                    [Smoothstep Ring]
                         │
                   [Multiply by Fade]
                         │
                  [Multiply by Neon Color]
                         │
                    [Output to Emission]
```

> **Tip:** Unity's Shader Graph supports custom functions via the **Custom Function** node, which lets you embed the HLSL loop above.

---

## 🤖 AI & Guard System

### Guard States (Finite State Machine)

```
                ┌──────────┐
         ┌──────│  PATROL   │◄─────────────────┐
         │      └─────┬─────┘                   │
         │            │ Hears noise              │ Timer expires
         │            ▼                          │ (no new noise)
         │      ┌───────────┐                   │
         │      │ SUSPICIOUS │──────────────────┘
         │      └─────┬──────┘
         │            │ Hears more noise /
         │            │ reaches noise source
         │            ▼
         │      ┌───────────┐
         │      │  ALERTED   │
         │      └─────┬──────┘
         │            │ Finds player /
         │            │ Sees player in flashlight
         │            ▼
         │      ┌───────────┐
         └──────│  CHASING   │
                └───────────┘
                      │
                      │ Catches player
                      ▼
                ┌───────────┐
                │ GAME OVER  │
                └───────────┘
```

### Guard Behavior Details

| State          | Behavior                                              | Visual Indicator                                 |
| -------------- | ----------------------------------------------------- | ------------------------------------------------ |
| **Patrol**     | Follows a predefined waypoint path at normal speed    | Guard's own footsteps create dim red sonar pings |
| **Suspicious** | Stops, turns toward last heard noise, waits 3s        | Yellow `?` icon pulses above head                |
| **Alerted**    | Walks toward the noise source location                | Orange `!` icon, faster red pings                |
| **Chasing**    | Runs directly toward the player's last known position | Bright red `!!`, rapid pings, flashlight ON      |

### Guard Hearing System

```csharp
// Pseudocode for guard hearing
void OnNoiseEvent(NoiseEvent noise)
{
    float distance = Vector3.Distance(transform.position, noise.origin);
    float hearingRange = baseHearingRange * noise.loudness;

    if (distance <= hearingRange)
    {
        // Guards hear noise with reduced precision at distance
        float accuracy = 1.0f - (distance / hearingRange);
        Vector3 perceivedOrigin = noise.origin + Random.insideUnitSphere * (1 - accuracy) * 3f;

        ReactToNoise(perceivedOrigin, noise.loudness);
    }
}
```

### Guard Hearing Ranges

| Noise Type    | Loudness | Guard Hearing Range |
| ------------- | -------- | ------------------- |
| Sneak         | 0        | Not heard           |
| Clap / Ping   | 0.5      | 10 units            |
| Running       | 0.7      | 14 units            |
| Thrown Object | 0.8      | 16 units            |
| Echo Bomb     | 1.0      | Entire level        |

### Guard Patrol — Waypoint System

- Each guard has a list of `Transform` waypoints.
- They walk between waypoints in order (loop or ping-pong).
- At each waypoint, they optionally pause and "look around" (rotate briefly).
- Patrol speed: `2 units/sec`. Chase speed: `5 units/sec`.

### Guard Flashlight (Introduced Level 7+)

- Guards in `CHASING` state activate a flashlight.
- The flashlight is a **cone-shaped trigger collider** extending from the guard.
- If the player is inside the cone **and** the cone ray isn't blocked by a wall → **CAUGHT**.
- The flashlight also acts as a permanent sonar source — anything inside the cone is visible.

---

## 🗺️ Level Design Guidelines

### Level Structure

Each level is a self-contained museum floor:

```
┌──────────────────────────────────────────────┐
│  ┌────────┐    ┌────────┐    ┌────────┐      │
│  │ EXHIBIT │    │ EXHIBIT │    │ EXHIBIT │     │
│  │  ROOM   │    │  ROOM   │    │  ROOM   │    │
│  │  (gem)  │    │(target) │    │  (gem)  │    │
│  └───┬─────┘    └───┬─────┘    └───┬─────┘   │
│      │              │              │          │
│  ════╪══════════════╪══════════════╪════════  │
│              MAIN CORRIDOR                    │
│  ════╪══════════════╪══════════════╪════════  │
│      │              │              │          │
│  ┌───┴─────┐    ┌───┴─────┐    ┌──┴──────┐  │
│  │ EXHIBIT  │    │ SECURITY │    │  ENTRY / │ │
│  │  ROOM    │    │  OFFICE  │    │  EXIT    │ │
│  │  (gem)   │    │ (guard)  │    │  [START] │ │
│  └──────────┘    └──────────┘    └──────────┘ │
└──────────────────────────────────────────────┘
```

### Design Rules

1. **Every room should have at least 2 entrances/exits** — no dead ends (reduces frustration in the dark).
2. **Place ambient noise sources** — dripping pipes, ticking clocks — that periodically emit small sonar pings for free. This gives the player tiny "freebies" to orient themselves.
3. **Guard patrol paths should be visible** — guards' own footsteps create dim red pings, so the player can "see" where guards walk if they wait patiently.
4. **Critical paths should be solvable silently** — the player should always be able to reach the exit using only ambient sonar + memorization, even if it's extremely difficult.
5. **Reward exploration** — optional gems placed in risky corners. High risk, high reward.

### Environment Objects

| Object               | Sonar Behavior                                                                     | Gameplay Purpose        |
| -------------------- | ---------------------------------------------------------------------------------- | ----------------------- |
| **Walls**            | Reflect sonar (hard edges, bright outlines)                                        | Core navigation         |
| **Glass Cases**      | Semi-transparent in sonar (dimmer outlines)                                        | Contain artifacts/gems  |
| **Pillars**          | Solid in sonar, provide cover                                                      | Hide behind them        |
| **Dripping Pipes**   | Emit tiny periodic pings (green)                                                   | Free ambient visibility |
| **Ticking Clocks**   | Emit rhythmic pings                                                                | Free ambient visibility |
| **Laser Tripwires**  | Invisible until sonar reveals them; crossing triggers loud alarm                   | Obstacles               |
| **Doors**            | Open/close silently if unlocked; locked doors require key cards                    | Gating progression      |
| **Security Cameras** | Sweep back and forth; if they "see" a sonar reflection of the player, alert guards | Advanced obstacle       |

---

## 🔊 Audio Design

Audio is **central** to this game — it's literally the core mechanic.

### Sound Categories

#### Player Sounds

| Sound            | Trigger                   | Notes                                      |
| ---------------- | ------------------------- | ------------------------------------------ |
| Footstep (sneak) | Moving slowly             | Very quiet, no sonar. Soft pad sound.      |
| Footstep (run)   | Moving fast               | Louder, triggers sonar pulse. Echoey slap. |
| Clap / Ping      | Ping button               | Sharp, satisfying clap with reverb trail   |
| Object throw     | Throw button              | Whoosh + distant impact/crash              |
| Item pickup      | Interact with collectible | Subtle chime                               |
| Echo Bomb        | Use special item          | Deep BWAAAAM with lingering reverb         |

#### Guard Sounds

| Sound            | Trigger                  | Notes                                     |
| ---------------- | ------------------------ | ----------------------------------------- |
| Footstep         | Guard walking            | Distinct from player (heavier, boot-like) |
| Radio chatter    | Random idle              | Faint, atmospheric                        |
| "Huh?"           | Suspicious state entered | Alert bark                                |
| "Over there!"    | Alerted state entered    | Aggressive bark                           |
| Flashlight click | Chase state entered      | Tense                                     |

#### Ambient Sounds

| Sound           | Source          | Notes                                          |
| --------------- | --------------- | ---------------------------------------------- |
| Water drip      | Pipes           | Periodic, synchronized with green sonar pulses |
| Clock tick      | Exhibit clocks  | Rhythmic, can be used for timing               |
| Distant thunder | Outside windows | Occasional, creates large ambient pulses       |
| HVAC hum        | Ventilation     | Constant low drone, sets mood                  |

### Music System

- **Stealth State:** Minimal ambient drone, deep bass notes, occasional high-pitched strings.
- **Suspicious State:** Subtle tension layer fades in — faster heartbeat-like bass.
- **Chase State:** Aggressive synth-wave track kicks in. Fast tempo, driving beat.
- **Victory:** Satisfying synth fanfare.
- **Caught:** Sharp dissonant sting, then silence.

> **Implementation:** Use Unity's Audio Mixer with snapshot blending to seamlessly transition between music states based on the highest alert level among all guards.

---

## 🖥️ UI/UX Design

### HUD (Minimal — In Line with Aesthetic)

The HUD should be nearly invisible. Information is communicated through the game world, not UI overlays.

```
┌──────────────────────────────────────────┐
│ [Ping Cooldown Ring]          [Gem: 3/7] │
│                                          │
│                                          │
│                                          │
│              GAMEPLAY AREA               │
│                                          │
│                                          │
│                                          │
│ [Noise Makers: ●●●○]    [Alert: ░░░░░░] │
└──────────────────────────────────────────┘
```

| Element               | Style                                                                |
| --------------------- | -------------------------------------------------------------------- |
| **Ping Cooldown**     | Circular ring around a small icon, fills up like a radial timer      |
| **Gem Counter**       | Small, top-right, fades in when a gem is collected                   |
| **Noise Maker Count** | Small dots showing remaining throwables                              |
| **Alert Meter**       | Only appears when any guard is suspicious or higher. Fills with red. |

### Menus

- **Main Menu:** Dark background with slow ambient sonar pulses revealing a museum silhouette. Title in neon cyan.
- **Level Select:** Museum floor plan layout. Completed levels glow. Star ratings shown.
- **Pause Menu:** Frosted glass overlay. Resume, Restart, Settings, Quit.
- **Game Over:** Screen goes dark. Single red sonar pulse from the guard that caught you. "CAUGHT" in red neon. Retry / Quit.

---

## 📁 Project Structure

```
📁 Assets/
├── 📁 Animations/
│   ├── Player/
│   └── Guards/
├── 📁 Audio/
│   ├── Music/
│   ├── SFX/
│   │   ├── Player/
│   │   ├── Guards/
│   │   └── Environment/
│   └── Mixers/
├── 📁 Materials/
│   ├── SonarMaterial.mat
│   └── EnvironmentMaterials/
├── 📁 Prefabs/
│   ├── Player.prefab
│   ├── Guards/
│   ├── Collectibles/
│   ├── Environment/
│   │   ├── Wall.prefab
│   │   ├── Pillar.prefab
│   │   ├── GlassCase.prefab
│   │   ├── DrippingPipe.prefab
│   │   └── LaserTripwire.prefab
│   └── UI/
├── 📁 Scenes/
│   ├── MainMenu.unity
│   ├── LevelSelect.unity
│   └── Levels/
│       ├── Level_01.unity
│       ├── Level_02.unity
│       └── ...
├── 📁 Scripts/
│   ├── 📁 Core/
│   │   ├── GameManager.cs
│   │   ├── LevelManager.cs
│   │   ├── ScoreManager.cs
│   │   └── AudioManager.cs
│   ├── 📁 Player/
│   │   ├── PlayerController.cs
│   │   ├── PlayerInput.cs
│   │   ├── PlayerInventory.cs
│   │   └── NoiseEmitter.cs
│   ├── 📁 Sonar/
│   │   ├── SonarPulse.cs
│   │   ├── SonarManager.cs
│   │   └── NoiseEventBus.cs
│   ├── 📁 AI/
│   │   ├── GuardAI.cs
│   │   ├── GuardStateMachine.cs
│   │   ├── GuardHearing.cs
│   │   ├── GuardPatrol.cs
│   │   ├── GuardFlashlight.cs
│   │   └── GuardAlertManager.cs
│   ├── 📁 Environment/
│   │   ├── AmbientNoiseSource.cs
│   │   ├── Collectible.cs
│   │   ├── Door.cs
│   │   ├── LaserTripwire.cs
│   │   └── SecurityCamera.cs
│   └── 📁 UI/
│       ├── HUDController.cs
│       ├── MainMenuController.cs
│       ├── PauseMenuController.cs
│       └── GameOverController.cs
├── 📁 Shaders/
│   ├── SonarPostProcess.shader        (HLSL)
│   ├── SonarPostProcess.shadergraph   (Shader Graph version)
│   └── SonarRendererFeature.cs        (URP Renderer Feature)
├── 📁 Settings/
│   ├── URP_Renderer.asset
│   ├── URP_PipelineAsset.asset
│   └── InputActions.inputactions
└── 📁 Sprites/ (if using 2D)
    ├── Player/
    ├── Guards/
    └── UI/
```

---

## 🗓️ Development Roadmap

### Phase 1 — Prototype (Week 1–2) 🟢

**Goal:** Prove the sonar mechanic is fun.

- [ ] Set up Unity project with URP
- [ ] Implement basic player movement (WASD, top-down)
- [ ] Create the **Sonar Shader** (even a basic version)
- [ ] Implement `SonarPulse` + `SonarManager` (spawn, expand, fade)
- [ ] Wire clap/ping input to spawn sonar pulses
- [ ] Create a simple test room with walls and objects
- [ ] **Playtest milestone:** Move around a dark room using only sonar pings

### Phase 2 — Core Gameplay (Week 3–4) 🟡

**Goal:** Add guards and the core stealth loop.

- [ ] Implement the `NoiseEventBus` (decoupled event system)
- [ ] Create guard AI with basic state machine (Patrol → Suspicious → Alerted)
- [ ] Implement guard hearing system (react to noise events)
- [ ] Add guard patrol waypoint system
- [ ] Make guards produce their own sonar pings (red) when walking
- [ ] Add collectible gems
- [x] Add noise-maker throwables
- [ ] **Playtest milestone:** Sneak past one guard, steal a gem, reach the exit

### Phase 3 — Polish & Juice (Week 5–6) 🟠

**Goal:** Make it _feel_ amazing.

- [ ] Refine sonar shader (add glow, bloom, edge detection)
- [ ] Add screen shake on loud events (Cinemachine Impulse)
- [ ] Implement audio system with spatial sound
- [ ] Add music system with state-based transitions
- [ ] Create particle effects (sonar ring particles, dust motes)
- [ ] Implement the alert meter UI
- [ ] Add the HUD elements (ping cooldown, gem counter)
- [ ] Camera system refinement (smooth follow, zoom on ping)

### Phase 4 — Content & Levels (Week 7–8) 🔴

**Goal:** Build the actual game.

- [ ] Design and build Level 1–5 (tutorial + basic stealth)
- [ ] Introduce guard flashlights (Level 7+)
- [ ] Introduce laser tripwires and security cameras
- [ ] Add locked doors + key card system
- [ ] Create the main menu and level select screen
- [ ] Implement scoring system
- [ ] Add game over screen and retry flow

### Phase 5 — Final Polish & Ship (Week 9–10) 🟣

**Goal:** Ship it.

- [ ] Full playtest and difficulty tuning
- [ ] Performance optimization (shader, AI, pooling)
- [ ] Add settings menu (volume, controls)
- [ ] Bug fixing
- [ ] Build for target platforms
- [ ] Create trailer / screenshots
- [ ] **RELEASE** 🚀

---

## 🛠️ Tools & Setup

### Required Software

| Tool                            | Purpose         | Link                                         |
| ------------------------------- | --------------- | -------------------------------------------- |
| **Unity 2022.3 LTS** (or newer) | Game Engine     | [unity.com](https://unity.com)               |
| **Visual Studio / VS Code**     | C# IDE          | [visualstudio.com](https://visualstudio.com) |
| **Git**                         | Version Control | [git-scm.com](https://git-scm.com)           |

### Recommended Unity Settings

1. **Create a new URP 3D project** (or URP 2D if going fully 2D).
2. **Enable Post-Processing** in the URP Renderer settings.
3. **Set the default background to pure black** (`Camera > Background > Color #000000`).
4. **Disable global illumination** — we don't need it. All lighting comes from the sonar shader.
5. **Install packages:** Input System, Cinemachine, TextMeshPro.

### Git Setup

```bash
# Clone the repo
git clone <repo-url>
cd ArcadeGame

# Unity .gitignore is essential — use:
# https://github.com/github/gitignore/blob/main/Unity.gitignore

# Branch strategy:
# main         — stable, playable builds only
# develop      — integration branch
# feature/*    — individual features (e.g., feature/sonar-shader)
```

---

## 🎨 Art Style Reference

### Color Palette

| Name          | Hex       | Usage                          |
| ------------- | --------- | ------------------------------ |
| Void Black    | `#000000` | Background — the default state |
| Sonar Cyan    | `#00E5FF` | Player sonar pulses            |
| Alert Red     | `#FF3D00` | Guard sonar & alert states     |
| Neon Magenta  | `#FF00FF` | Thrown object impacts          |
| Ambient Green | `#00E676` | Environmental ambient pings    |
| Cold Blue     | `#2979FF` | Player running footsteps       |
| Pure White    | `#FFFFFF` | Echo bomb, UI highlights       |

### Visual Principles

1. **The screen is BLACK by default.** No room lights, no ambient lighting. Just void.
2. **Sonar outlines only.** Objects are rendered as neon wireframe/edge outlines when hit by sonar. No filled surfaces.
3. **Everything glows.** Use bloom post-processing liberally. The neon outlines should bleed light.
4. **Minimal geometry.** Levels use clean, geometric shapes — rectangles, circles, clean lines. Think _Tron_ meets _Limbo_.
5. **Color = information.** Cyan = player-safe. Red = danger. Magenta = distraction. Green = freebie.

---

## 🤝 Contributing

### Workflow

1. Pick a task from the [Development Roadmap](#-development-roadmap).
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Implement, test, commit.
4. Push and open a Pull Request against `develop`.
5. Get a code review from your partner.
6. Merge!

### Coding Conventions

- **C# Style:** Follow Microsoft's [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- **Naming:** `PascalCase` for classes/methods, `camelCase` for local variables, `_camelCase` for private fields.
- **Comments:** Comment _why_, not _what_. The code should be self-explanatory.
- **Prefabs over scene objects:** Build things as prefabs. Keep scenes clean.
- **ScriptableObjects for data:** Use them for guard configs, level configs, sonar settings, etc.

### Task Division Suggestion

| Person A                  | Person B                   |
| ------------------------- | -------------------------- |
| Sonar Shader + Visual FX  | Guard AI + State Machine   |
| Player Controller + Input | Level Design + Environment |
| Audio System              | UI + Menus                 |
| Camera System             | Scoring + Game Flow        |

---

## 📜 License

TBD — Decide on a license when ready to publish.

---

> _"In the dark, every sound is a double-edged sword."_
>
> — The Echo-Thief

---

**Happy building! 🦇🎮**
