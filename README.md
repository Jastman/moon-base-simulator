# Moon Base Simulator

**A realistic lunar base planning and exploration simulator built in Unity 6.**

Design your moon base using real NASA lunar terrain data, analyze water ice deposits and solar exposure, then hop in a rover and drive it yourself. Built for anyone who's ever wanted to answer the question: *where exactly would you put humanity's first permanent settlement on the Moon?*

---

## Screenshots

![Lunar surface terrain](https://upload.wikimedia.org/wikipedia/commons/thumb/1/12/Lunar_surface_seen_by_LRO.jpg/1280px-Lunar_surface_seen_by_LRO.jpg)
*Lunar terrain visualization (Cesium Moon Terrain tileset / LRO data)*

![Moon south pole region](https://upload.wikimedia.org/wikipedia/commons/thumb/e/e1/Shackleton_crater_LROC.jpg/1280px-Shackleton_crater_LROC.jpg)
*Shackleton Crater region — candidate site for base placement near water ice deposits*

![Lunar surface from Apollo](https://upload.wikimedia.org/wikipedia/commons/thumb/2/27/AS11-40-5931.jpg/1280px-AS11-40-5931.jpg)
*Surface-level reference for rover mode terrain and lighting*

> **Note:** Screenshots above are NASA/LRO public domain reference imagery. In-game screenshots coming soon.

---

## Features

### Real Lunar Terrain
- Streams the **NASA LRO-based Cesium Moon Terrain** tileset via [Cesium ion](https://cesium.com/platform/cesium-ion/) (asset ID `2684829`)
- Full 3D terrain at any location on the Moon — not a flat heightmap

### Data Layers
- **Water/Ice Deposits** — overlays concentrations from NASA LCROSS/LRO findings
- **Solar Exposure** — shows average illumination hours to identify permanently shadowed regions and solar-optimal ridge lines
- **Satellite Imagery** — high-res ortho imagery draped over terrain

### Base Module Placement
Drag and drop modular structures onto the terrain in **Design Mode**:
| Module | Purpose |
|---|---|
| Habitat | Crew living quarters |
| Solar Array | Power generation |
| Power Storage | Battery bank / fuel cell backup |
| Greenhouse | Food production and O₂ generation |
| Airlock | Surface EVA access point |

### Rover Mode
- Switch to **Rover Mode** and drive a pressurized rover across the terrain you just designed
- Physics tuned for lunar gravity (**1.62 m/s²**) — feels meaningfully different from Earth driving
- Interact with placed modules from the surface

### Tactical HUD
- Space Force-style heads-up display showing coordinates, elevation, slope angle, and active data layer readouts
- Design Mode UI with module palette, placement grid, and resource budget tracker

---

## Getting Started

### Requirements
- **Unity 6** (6000.0.x or later)
- A free [Cesium ion account](https://cesium.com/platform/cesium-ion/) with an access token

### Setup

**1. Clone the repo**
```bash
git clone https://github.com/Jastman/moon-base-simulator.git
```

**2. Open in Unity 6**

Open the project folder in Unity Hub. Make sure you're on Unity 6 (6000.0.x) — earlier versions won't work.

**3. Install Cesium for Unity**

In the Unity Package Manager, add the Cesium for Unity package:
- Go to **Window → Package Manager**
- Click **+** → **Add package by name**
- Enter: `com.cesium.unity`

Or follow the [official Cesium for Unity quickstart](https://cesium.com/learn/unity/unity-quickstart/).

**4. Connect your Cesium ion token**

- In the Unity menu, go to **Cesium → Cesium ion**
- Paste your ion access token
- The LRO Moon Terrain tileset (asset ID `2684829`) will start streaming automatically

**5. Run the Module Asset Bootstrapper**

In the Unity menu, go to **Tools → Moon Base → Bootstrap Module Assets**. This generates the prefab references needed for the drag-and-drop placement system.

**6. Set Moon Ellipsoid in CesiumGeoreference**

Select the `CesiumGeoreference` object in the scene hierarchy. In the Inspector, set the **Ellipsoid** to **Moon (1737.4 km)** to ensure correct coordinate math.

**7. Hit Play**

You're on the Moon.

---

## Controls

### Design Mode
| Key / Input | Action |
|---|---|
| `1–5` | Select module (Habitat, Solar Array, Storage, Greenhouse, Airlock) |
| Left Click | Place selected module |
| Right Click | Cancel / deselect |
| `R` | Rotate module 45° |
| `Del` | Delete hovered module |
| `Tab` | Cycle data layers (off / water ice / solar / imagery) |
| `Esc` | Open menu |

### Rover Mode
| Key / Input | Action |
|---|---|
| `W / S` | Throttle forward / brake |
| `A / D` | Steer left / right |
| `Space` | Handbrake |
| `C` | Toggle camera (chase / cockpit) |
| `F` | Interact with nearby module |
| `Tab` | Switch back to Design Mode |

---

## Tech Stack

| Component | Technology |
|---|---|
| Engine | Unity 6 (6000.0.x) |
| Terrain streaming | Cesium for Unity |
| Lunar terrain data | NASA LRO — Cesium Moon Terrain tileset (ion asset 2684829) |
| Data layers | NASA LCROSS water ice, LRO illumination maps |
| Language | C# |
| Physics | Unity PhysX, tuned to 1.62 m/s² lunar gravity |

---

## License

MIT License — see [LICENSE](LICENSE) for details.

NASA imagery and terrain data used under NASA's [Open Data Policy](https://www.nasa.gov/open/). Cesium for Unity is used under the [Apache 2.0 license](https://github.com/CesiumGS/cesium-unity/blob/main/LICENSE).
