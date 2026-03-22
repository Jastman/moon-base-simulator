# Moon Base Simulator

**A realistic lunar base planning and exploration simulator built in Unity 6.**

Design your moon base using real NASA lunar terrain data, analyze water ice deposits and solar exposure, then hop in a rover and drive it yourself. Built for anyone who's ever wanted to answer the question: *where exactly would you put humanity's first permanent settlement on the Moon?*

---

## Screenshots

![Lunar surface terrain](https://upload.wikimedia.org/wikipedia/commons/c/c9/Moon_nearside_LRO.jpg)
*Lunar terrain visualization (Cesium Moon Terrain tileset / LRO data)*

![Moon south pole region](https://upload.wikimedia.org/wikipedia/commons/9/9a/Rimlight-_Shackleton_Crater_%283929168011%29.jpg)
*Shackleton Crater region — candidate site for base placement near water ice deposits*

![Lunar surface from Apollo](https://upload.wikimedia.org/wikipedia/commons/9/9c/Aldrin_Apollo_11.jpg)
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

---

## Game Design Document

<details>
<summary>Click to expand full GDD</summary>

# 1. Executive Summary

LUNAR OPS: Mission Control Simulator is a realistic 3D lunar base operations platform built on Unity 6 with Cesium for Unity, streaming actual NASA terrain data and integrating live publicly available lunar science datasets. Players — or trainees, or planners — select a real landing site on actual LRO/LOLA topography, design a modular base with physics-grounded constraints, and operate it across simulated lunar day/night cycles where every decision cascades through power, thermal, water, structural, and communications systems driven by real instrument data. This is not a game about an imaginary planet. The Shackleton Crater rim you walk is the one NASA is targeting. The ice beneath your rover's wheels comes from LEND hydrogen maps and LCROSS impact confirmation. The temperature that stresses your habitat at lunar noon is the one Diviner measured.

## Core Experience

Three interlocking modes form the loop: Site Selection and Base Design, where players lay out modules on real terrain under real solar exposure and slope stability constraints; Operations Mode, a mission control panel where a 14-Earth-day lunar day/night cycle advances and live data overlays update continuously; and Rover Exploration, where players drive on actual terrain to investigate anomalies, collect samples, and extend the base's operational picture. Real data is the content. Decisions are the gameplay.

## Target Audience

Space agency mission planners and trainees (NASA, ESA, JAXA, ISRO, CSA)
Aerospace engineers and ISRU researchers needing rapid layout prototyping
University programs in aerospace engineering, planetary science, and mission design
Serious simulation enthusiasts who want factual grounding over fantasy
Public outreach programs — a credible, engaging entry point for space literacy

## Real Utility Angle

Existing planning tools (NASA SETT, analog site trainers) are static. They cannot simulate how a layout decision on day one propagates into a power crisis on day 47 when dust accumulates on a poorly angled array during a thermal excursion event. LUNAR OPS does. That gap represents a genuine training and prototyping use case for agencies with active lunar surface programs. The project is structured with NASA SBIR Phase I/II eligibility in mind, as well as NSF STEM education grants, ESA BIC incubator funding, and direct university research partnerships. All underlying data is publicly available through NASA PDS under open science policies.

# 2. Core Concept & Design Pillars

LUNAR OPS is built on three interlocking design pillars. These are not marketing statements — they are constraints that govern every feature decision. A mechanic that violates any of these three pillars does not ship.

| Pillar | Statement | What It Means in Practice |
|---|---|---|
| Real Data, Real Place | The simulation runs on actual lunar science. | Shackleton Crater terrain via Cesium + LRO. Ice distributions from LEND hydrogen maps and LCROSS confirmation. Surface temps from Diviner radiometer. Mineral deposits from Chandrayaan-1 M3. No invented planet. |
| Living Operations Center | The base responds to real conditions, not invented ones. | Solar exposure is calculated from actual Sun geometry at lunar poles. Temperature swings match instrument records. Ice sublimation events are triggered by real Diviner data thresholds. The simulator is live, not scripted. |
| Meaningful Decisions | Every layout choice has cascading consequences grounded in physics. | Place a landing pad too close: ejecta damages solar panels on every resupply. Route power cables too far: resistance losses reduce available wattage. Build near the crater rim: great ice access, brutal communications blackouts. The environment enforces the tradeoffs. |

## Design Philosophy

The core insight driving this project is that realism and playability are not in tension when the real data is intrinsically interesting. Lunar polar environments are genuinely hostile, genuinely varied, and genuinely consequential for human survival. The permanent shadow of Haworth Crater holds confirmed water ice. The southern rim of Shackleton receives near-continuous sunlight. These are not invented dramatic devices — they are scientific facts that create compelling operational tradeoffs. The designer's job is to surface those tradeoffs clearly, not to invent new ones.

# 3. Real-Time Data Integration

All scientific data used in LUNAR OPS is sourced from publicly available NASA and international space agency archives, primarily through the NASA Planetary Data System (PDS). No proprietary datasets are required. The following table documents each integrated dataset, its content, update cadence, and source.

| Dataset | Content & Usage | Update Cadence | Source |
|---|---|---|---|
| LRO (Lunar Reconnaissance Orbiter) | Surface imagery (LROC NAC/WAC), topography, slope maps, permanently shadowed regions (PSRs), albedo. Primary baseline for all surface visualization. | Static / Periodic updates | NASA PDS / Cesium ion asset 2684829 |
| LOLA (Laser Altimeter) | Elevation at 1m resolution, surface roughness, cross-track slope. Foundation for all terrain rendering and slope stability calculations. | Static (definitive dataset) | NASA PDS LOLA archive |
| Diviner Radiometer | Surface temperature maps by latitude/longitude/time-of-day. Thermal inertia data. Ranges from -170C in permanent shadow to +120C at equatorial noon. Powers all thermal simulation. | Periodic model updates | NASA PDS Diviner archive, LOLA_DIVINER_composite |
| LEND (Neutron Detector) | Hydrogen concentration maps used as water ice proxy. Highest confidence water signal at Shackleton, Haworth, Nobile, Cabeus craters. Drives ISRU site scoring. | Static (Chandrayaan-2 updates) | NASA PDS LEND archive |
| LAMP (UV Spectrograph) | UV mapping of water ice in permanently shadowed regions — surface frost detection at higher spatial resolution than LEND. | Static | NASA PDS LAMP archive |
| Mini-RF (Radar) | Subsurface ice detection via S-band and X-band radar. Circular polarization ratio (CPR) anomaly maps indicate ice-bearing regolith. | Static | NASA PDS Mini-RF archive |
| Chandrayaan-1 M3 (Moon Mineralogy Mapper) | Mineral composition mapping: water/OH absorption, olivine, pyroxene, ilmenite distributions. Drives in-situ resource assessment beyond water. | Static | NASA PDS M3 archive / ISRO PDS |
| LCROSS Impact Data | Confirmed water ice at Cabeus Crater from 2009 impact plume spectrometry. Highest-confidence single-point ice confirmation. Used to anchor LEND calibration. | Static (definitive) | NASA PDS LCROSS archive |
| Chang'e-7 (2026, pending) | Upcoming southern polar lander/hopper with ice-seeking payload. Data expected 2026-2027. Will be integrated when available via CNSA open data release. | Future / TBD | CNSA public release (planned) |
| Cesium ion Terrain | Quantized Mesh terrain tiles streamed in real-time. LRO-derived topography rendered at multiple LOD levels. Seamless zoom from orbital to surface view. | Real-time stream | Cesium ion, asset ID 2684829 |

## Data Pipeline Architecture

Raw NASA PDS data arrives in PDS3/PDS4 format, typically as IMG or TIFF rasters with detached label files. The processing pipeline converts these to Cloud-Optimized GeoTIFF (COG) format using GDAL, then uploads to Cesium ion as raster overlay assets. In-engine, the DataLayerManager component streams these overlays as raster drapes over the quantized mesh terrain, applying color ramps and alpha blending appropriate to each dataset type.
NASA PDS API access via HTTPS — no special credentials required for public datasets
GDAL 3.x for format conversion and reprojection to lunar selenographic coordinates (IAU 2015)
Cesium ion for terrain tile hosting and raster overlay CDN delivery
Update cadence varies: temperature models update seasonally, ice maps are static, terrain is definitive

## Synthetic Data and Gap-Filling

Real data has gaps in spatial coverage and time resolution. Where gaps exist, the simulation falls back gracefully: thermal values between measured grid points are bicubically interpolated from Diviner model grids; ice probability in poorly constrained areas uses a conservative heuristic based on PSR depth and latitude; mineral composition outside M3 coverage defaults to nominal highlands regolith values. All synthetic or interpolated values are visually flagged in-engine with a subtle hatching overlay so users know they are working with estimated data.

# 4. Game Loop & Modes

LUNAR OPS is structured around three sequential but interlocking phases. A player can return to earlier phases at any time — running a new site search, redesigning the base layout, or dispatching a rover — but the simulation clock continues advancing in Operations Mode regardless. The loop is designed to be non-linear in practice while maintaining a clear progression of stakes.

## Phase 1 — Site Selection & Base Design (Builder Mode)

The session begins on a 3D globe rendered from actual LRO imagery and LOLA topography via Cesium for Unity. The player zooms to the lunar south pole region and selects a candidate landing site. The system immediately overlays relevant data layers: slope map (from LOLA), solar exposure projection (calculated from sun geometry at this latitude), ice probability (from LEND hydrogen map), surface temperature range (from Diviner), and communications line-of-sight to Earth relay satellites.
Site candidates are pre-identified (Shackleton rim, Haworth, de Gerlache, Nobile) with agency-relevant metadata, but any slope-stable location is selectable
Slope stability is enforced: terrain slopes above 15 degrees reject foundation placement with a visible warning and engineering explanation
Module placement uses a snap-to-terrain raycaster that reads actual LOLA elevation at the cursor position
Solar exposure simulation runs in real-time using SPICE-derived Sun angles for the selected date and latitude — no approximations
Power cable routing tools show live resistance-loss calculations as cables are drawn between modules
Blast zone enforcement: landing pads generate a visible exclusion radius (derived from ejecta throw models) that blocks solar panel placement
A 'Run 90-Day Preview' button executes a fast simulation of resource flows, equipment degradation, and anomaly probability before the player commits to the design

## Phase 2 — Operations Mode (Mission Control)

Once construction begins, the player transitions to a mission control HUD. Time can advance in real-time (useful for demos and training scenarios) or at 10x/100x/1000x acceleration. A complete lunar day/night cycle spans 29.5 Earth days — approximately 14 days of sunlight followed by 14 days of darkness — and this cycle drives nearly every system in the simulation.
Live data overlays update on the terrain as the simulation clock advances: shadow migration across the crater floor, temperature gradients shifting with sun angle, ice sublimation probability increasing in sun-exposed areas
Resource dashboards display real-time flows for power (generated vs. consumed vs. stored), water (extracted vs. processed vs. consumed), oxygen production, propellant generation, and waste heat rejection
Equipment degradation model tracks dust accumulation on solar panels (reducing output as a function of time and wind proxy), micrometeorite impact probability by surface exposure and time, and seal/bearing wear on rover components
Alert system uses a four-tier escalation: Nominal (green), Caution (yellow), Warning (orange), Critical (red). Each tier has distinct audio and visual signatures
Anomaly events are triggered by data-driven thresholds — a power dip below 15% reserve, a temperature excursion outside module spec, a communications blackout window approaching — requiring player response within a time window
Rover dispatch menu allows player to send rovers on pre-defined mission profiles or manually driven sorties

## Phase 3 — Rover Exploration

The player can drop into rover control at any time from Operations Mode. Rover physics runs at lunar gravity (1.62 m/s2) with wheel-terrain interaction modeled on actual Shackleton rim regolith properties. Navigation uses actual terrain geometry — the crater walls, boulders, and slope breaks the player sees are where the LOLA data says they are.
First-person cockpit view with instrument cluster HUD, third-person chase camera, and overhead tactical map
Onboard spectrometer collects sample data at any surface location, returning mineral composition values drawn from M3 data at that coordinate
Ice prospect drilling: place a drill marker on ice-probable terrain; drill return time and yield are calculated from LEND confidence at that location
Anomaly investigation: Operations Mode may flag a specific grid square with a sensor anomaly; rover must physically traverse to that location to collect diagnostic data
Range is battery-limited with a return-to-base constraint — players must plan routes against power budget, terrain slope, and communication line-of-sight windows
Data collected by rovers feeds back into the Operations Mode resource model: a newly confirmed high-confidence ice vein unlocks a more favorable ISRU extraction rate

# 5. Core Systems & Mechanics

Each system below is a self-contained simulation component that exposes inputs, outputs, and failure modes to the player. Systems interact through shared resource buses (power, water, thermal load). No system operates in isolation. All numeric values below are drawn from published NASA/ESA engineering references unless otherwise noted.

## 5.1 Power System

Solar array output is calculated as: P = A * E * sin(theta) * (1 - d) * (1 - D) where A = panel area (m2), E = solar irradiance at Moon (~1361 W/m2), theta = sun elevation angle (from SPICE ephemeris), d = dust attenuation factor (time-cumulative), D = degradation factor (age + micrometeorite).
Battery storage modeled with realistic charge/discharge efficiency (approximately 90% round-trip for Li-ion)
Power cable routing: resistance loss increases with cable length; player-drawn cables accumulate resistance that reduces delivered power
Power priority tiers enforced automatically: Tier 1 (life support, comms) never shed; Tier 2 (science, processing) shed at 20% reserve; Tier 3 (expansion, non-essential) shed at 40% reserve
Failure cascade: loss of battery storage triggers immediate triage mode — player must manually confirm which Tier 2/3 loads to disconnect
Nuclear power option (RTG or fission surface power) unlocks in V1.0 as a high-mass, high-reliability alternative to solar

## 5.2 Thermal Management

Lunar surface temperatures are pulled directly from the Diviner Radiometer model grid. Module thermal loads are calculated from incident solar flux on exposed surfaces, internal heat generation from electronics and crew metabolic load, and conductive coupling with the regolith through foundations.
Permanently shadowed areas of the crater floor serve as natural cold sinks — heat rejection radiators facing these regions operate more efficiently
Habitat burial depth option reduces thermal swing amplitude significantly (regolith insulation) at the cost of construction complexity and additional excavation mass budget
Thermal runaway failure mode: if primary cooling fails during lunar noon (surface temps up to +120C), module interior temperature climbs at a calculable rate giving the player a timed response window before irreversible damage
Cryogenic storage for water and propellant benefits from proximity to permanent shadow — layout decisions that ignore this incur refrigeration power penalties

## 5.3 Water & ISRU System

Water extraction rate is a function of drill proximity to confirmed ice deposit locations (LEND/LCROSS confidence-weighted), drill power consumption, and thermal environment of the extraction site. The ISRU chain follows the actual planned NASA MOXIE-extended architecture.
Ice extraction rate: function of subsurface ice concentration (from LEND H-proxy), drill depth, and ambient temperature (cold sites extract more efficiently due to lower sublimation loss)
ISRU processing chain: ice regolith -> heating -> water vapor -> condensation -> liquid water storage -> electrolysis -> O2 (life support) + H2 (propellant feedstock)
Water consumption rates: crew (approximately 3 liters/person/day baseline), electrolysis, greenhouse irrigation (if applicable)
Propellant production: H2 + O2 -> liquid propellant for ascent vehicle or surface hoppers — this is the strategic endgame of base development
Storage tank mass budgets are tracked; the player must balance extraction rate against storage capacity and consumption rate to avoid waste or shortage

## 5.4 Structural & Geotechnical

Regolith bearing capacity varies with slope and compaction depth. Foundation placement on slopes above 15 degrees is blocked. The simulation tracks module settlement over time as a low-probability anomaly trigger.
Blast ejecta model: landing pad placement generates a circular exclusion zone based on published ejecta throw velocity models for the expected vehicle class. Solar panels or habitats within this radius will receive damage on every landing event
Micrometeorite flux: surface-exposed components accumulate damage as a function of exposure area and time; buried or shielded components are protected
Regolith shielding: players can spend mass budget to add regolith berms around habitats, reducing radiation dose and micrometeorite probability at the cost of construction time

## 5.5 Communications

Line-of-sight to Earth is calculated from actual LRO terrain geometry and known lunar relay satellite positions. The south polar region has significant terrain-induced blackout windows that are a genuine operational constraint for any real base.
Terrain occlusion computed from LOLA elevation data at each sim timestep — blackout windows are predictable and schedulable, as in real mission operations
Relay antenna placement is a solvable layout optimization: placing a relay on a high ridge can eliminate blackout windows at the cost of cable runs and structural mass
Bandwidth simulation: high-data-rate activities (full telemetry uplink, rover HD video) are deprioritized during marginal link windows
LunaNet architecture (NASA's planned lunar communications infrastructure) unlocked as a scenario option in V1.0

# 6. Layout Consequence System

The Layout Consequence System is the central design innovation of LUNAR OPS. It transforms base-building from a visual/organizational puzzle into an engineering judgment exercise where every placement decision has downstream effects that compound over the simulation timeline. The table below documents the core consequence chains. This is not an exhaustive list — it is the minimum set that must be implemented for the core loop to be meaningful.

| Layout Decision | Consequence | Severity | Quantified Impact |
|---|---|---|---|
| Solar arrays placed far from habitat | Power cable resistance losses; additional cable mass; higher probability of cable failure event | Medium | Power delivered -8% to -22% depending on distance |
| Landing pad within 200m of solar panels | Rocket exhaust ejecta damages panel surface on every landing; dust loading also increases substantially | High | Panel output degrades 3-12% per landing event |
| Habitat on sun-facing equator-ward slope | Good solar exposure year-round; poor proximity to ice deposits; elevated thermal load requires active cooling | Medium | ISRU efficiency -30%; cooling power draw +15% |
| Habitat at crater rim edge | Excellent ice access; frequent communications blackouts; extreme diurnal temperature swings at transition zone | High | Blackout windows up to 40 min/orbit; thermal stress +25% |
| Single power routing path (no redundancy) | Any array damage or cable failure creates a single point of failure — base-wide power loss | Critical | Mean time to cascading failure 40% lower than redundant design |
| Greenhouse on permanently shadowed slope | Low available solar power for grow lights; reduced cooling load; ice sublimation risk is lower nearby | Low-Medium | Lighting power draw +40%; refrigeration power draw -20% |
| ISRU drill near warm sunlit regolith | Higher sublimation rate degrades ice yield; extraction less efficient at elevated temps | Medium | Ice yield -15% to -35% vs. shadowed drill site |
| Rover charging at habitat hub | Convenient but adds to peak power draw during battery-limited lunar night periods | Low | Night power budget reduced 8-12% during rover charging cycles |
| No regolith shielding on habitat | Higher radiation dose accumulation for crew; higher micrometeorite surface damage probability | Medium (long-term) | Radiation dose rate 2.3x higher than shielded equivalent |

## Consequence Propagation Model

Each consequence is implemented as a modifier applied to the relevant system's update function. Modifiers stack multiplicatively. A player who places solar arrays far from the habitat, fails to add redundant routing, and builds near a frequently landed pad can accumulate power delivery penalties in excess of 50% of designed capacity — a scenario that is entirely self-inflicted and entirely explainable. The system never punishes players arbitrarily. Every penalty is traceable to a decision and explainable in engineering terms.
The tutorial system uses the Layout Consequence System as its primary pedagogical tool. Trainees are shown the consequences of a deliberately poor layout before being asked to design an improved one. The contrast between the two simulation runs communicates the engineering principles more effectively than any text description.

# 7. NASA & Agency Utility Angle

LUNAR OPS is designed from the ground up to be useful to space agencies and aerospace organizations, not just entertaining to players. This section documents the specific use cases, differentiators from existing tools, and funding pathways that make the agency utility angle credible and actionable.

## Gap Analysis — Existing Tools

Current lunar surface planning tools fall into two categories. High-fidelity engineering tools (STK, GMAT, custom FEA packages) are accurate but inaccessible to junior planners, require significant training to operate, and are not well-suited to rapid layout iteration. Low-fidelity analog trainers and whiteboard exercises build intuition but cannot simulate dynamic consequence chains. LUNAR OPS occupies the gap between these: higher fidelity than whiteboard planning, dramatically more accessible than professional engineering software, and specifically designed to make consequence chains visible and learnable.
NASA SETT (Surface EVA Training Tool): Focused on EVA procedure simulation, not base layout or resource management. No real terrain data integration.
Moonbase Alpha (NASA, 2010): Educational game on actual lunar terrain (a small area), but no real data integration, no resource simulation, no layout consequence system. Demonstrates the concept but not the depth.
Surviving Mars (Haemimont Games): High-quality resource management sim on a fictional Mars analog. Excellent gameplay loop, no real science data, fictional planet.
KSP2 (Intercept Games): Deep rocket engineering simulation. No surface operations, no ISRU modeling, no base layout consequence system.
LUNAR OPS: Real terrain (LRO/LOLA), real science data (Diviner/LEND/M3), real physics consequences, accessible UI designed for training use.

## Specific Agency Use Cases

The following use cases are grounded in documented needs of active lunar surface program offices. These are not speculative — NASA Artemis campaign planning documents and ESA Moon Village working papers explicitly identify layout optimization and trainee pipeline development as near-term needs.
Mission Planner Onboarding: New hires in mission planning offices typically require 6-12 months before they have reliable layout intuition. A simulator that forces them to experience consequence chains in compressed simulation time could reduce this to weeks.
Rapid Concept Prototyping: Before committing to detailed engineering analysis of a base configuration, a planning team could run a LUNAR OPS simulation to screen out obviously poor layouts. The tool does not replace detailed analysis — it narrows the solution space cheaply.
Public Engagement and Outreach: NASA has a documented history of using games for public engagement (Moonbase Alpha precedent). A credible, visually compelling simulator grounded in real Artemis campaign data would be a high-value outreach asset for congressional briefings, university partnerships, and public science communication.
University Curriculum Integration: Aerospace engineering programs at MIT, Caltech, and Georgia Tech already include lunar habitat design coursework. LUNAR OPS could serve as a practical simulation lab, with scenario packs designed around specific curriculum objectives.

## Funding Pathways

| NASA SBIR Phase I / Phase IITopic areas within NASA SBIR/STTR programs consistently include simulation and training tools for human spaceflight (historically under subtopic H6). Phase I ($150-200K) would fund MVP development and a pilot study with a NASA partner office. Phase II ($750K-1.5M) would fund full V1.0 development and agency integration. Data provenance is entirely PDS-sourced, satisfying open science requirements. |
|---|

| NSF STEM Education & Human Resource DevelopmentThe simulation's utility as a university curriculum tool aligns with NSF's Improving Undergraduate STEM Education (IUSE) program. A partnership with one or more aerospace engineering programs would provide both funding support and a credible user research pipeline. |
|---|

| ESA BIC (Business Incubation Centre)ESA BIC programs in Germany, the Netherlands, and the UK provide funding and mentoring for space-sector startups. The European angle on lunar resource utilization (ESA's PROSPECT drill, planned lunar village concept) aligns directly with ESA BIC's current portfolio priorities. |
|---|

## Data Attribution and Licensing

All NASA PDS datasets are publicly available under NASA's open data policy. Cesium ion's lunar terrain tileset (asset ID 2684829) is available under Cesium's standard commercial license with attribution. Chandrayaan-1 M3 data is available through NASA PDS under ISRO/NASA data sharing agreement. Chang'e-7 data will depend on CNSA's open data release policy, which has historically been more restrictive — this dataset is marked as aspirational pending policy clarification.

# 8. HUD & UI Design

The UI design is inspired by Palantir's Gotham and Anduril's Lattice interfaces — high-information-density tactical displays designed for professionals who need fast situational awareness, not decorative sci-fi aesthetics. The visual language is dark navy and electric blue on near-black backgrounds, with color-coded status indicators and minimal chrome. Every UI element has a functional reason to exist.

## Design Mode (Builder Interface)

Top-down and isometric camera modes with smooth zoom to surface-level perspective
Module palette on left panel: categorized by function (Habitat, Power, ISRU, Comms, Mobility). Each module shows mass, power draw, and footprint before placement
Data layer toggle bar along top: Terrain Slope, Ice Probability, Solar Exposure, Surface Temperature, Mineral Composition, Communications Line-of-Sight, Ejecta Blast Zones
Active layer renders as a translucent raster drape over the terrain surface with a legend and scale bar
Real-time feedback panel on right: as modules are placed, the panel updates projected power balance, ISRU yield estimate, and communications coverage
90-Day Simulation Preview: renders a time-lapse of resource curves with anomaly markers, exportable as a PDF summary for offline review

## Operations Mode (Mission Control HUD)

Four-quadrant layout: top-left (base 3D overview with module status indicators), top-right (resource flow dashboard), bottom-left (alert feed and event queue), bottom-right (rover status and dispatch)
Module status indicators use a color-coded dot system on the 3D view: green (nominal), yellow (caution), orange (warning), red (critical), gray (offline)
Resource flow dashboard shows real-time bar graphs for power, water, O2, and propellant with trend arrows indicating direction of change
Timeline scrubber at the bottom of the screen allows review of the last 72 simulated hours of any system metric
Alert feed uses military-style NOTAM format: time, system, severity, recommended action. Clicking an alert highlights the affected module in the 3D view
Data layer overlays remain active in Operations Mode — the player can see the temperature gradient migrating across the crater floor in real-time as the simulation clock advances

## Rover Mode (Cockpit HUD)

First-person cockpit with physical instrument cluster: battery level, speed, heading, slope indicator, distance from base, communication signal strength
Tactical overlay minimap in top-right corner showing rover position on actual terrain with waypoints and anomaly markers
Spectrometer readout panel activates when rover is stationary: shows M3-derived mineral composition at current location
Camera feed from front-facing and mast-mounted cameras, switchable with no transition animation (mirrors real rover camera architecture)
Sample collection UI: flagging a location for sample collection queues it for display in Operations Mode science dashboard

## Alert System

| Level | Color | Trigger Condition | Required Response |
|---|---|---|---|
| NOMINAL | Green | All systems within design parameters | No action required |
| CAUTION | Yellow | System approaching threshold (e.g., power reserve < 25%) | Player notified; monitoring recommended |
| WARNING | Orange | System at threshold; degraded operations imminent | Player must acknowledge; action recommended within 10 min simulated time |
| CRITICAL | Red | System failure or life-safety threshold breached | Immediate player action required; auto-pause option available |

# 9. Technical Architecture

## Core Stack

Unity 6 (URP) — primary engine. URP provides the performance headroom needed for real-time terrain streaming and complex overlay rendering on mid-range hardware.
Cesium for Unity — terrain streaming layer. Handles quantized mesh tile loading, LOD management, and coordinate system bridging between WGS84/lunar selenographic and Unity's world space.
C# scripting throughout — no third-party game logic frameworks. Clean separation between simulation engine (pure C# logic) and presentation layer (Unity MonoBehaviours).
JSON serialization for save/load — base state serialized as a human-readable JSON document; enables external analysis and future data export features.
Target platform: PC standalone (Windows primary, macOS secondary). Minimum spec: 8GB RAM, dedicated GPU with 4GB VRAM, SSD for terrain tile caching.

## Key Unity Components

MoonBaseManager — singleton orchestrator; owns simulation clock, resource buses, alert queue, and save/load lifecycle
TerrainRaycaster — wraps Cesium's Cartographic coordinates; provides snap-to-terrain placement and slope calculation at cursor position
ModulePlacer — handles module instantiation, constraint checking (slope, blast zone, power routing), and consequence modifier registration
DataLayerManager — manages raster overlay assets; handles layer toggling, color ramp application, and alpha blending between layers
SolarExposureManager — consumes SPICE-derived sun position data; calculates per-panel incidence angles and shadow masks at each simulation timestep
ThermalSimulator — reads Diviner grid values for current sun position; applies thermal load modifiers to each surface-exposed module
ISRUManager — tracks drill sites, ice yield calculations, and ISRU processing chain state
RoverController — handles input mapping, lunar-gravity physics (1.62 m/s2), wheel-terrain interaction, and camera switching
AlertSystem — monitors all system thresholds; generates NOTAM-format alert records; manages escalation and auto-pause logic

## Data Pipeline

The data pipeline from raw NASA PDS archives to in-engine raster overlays runs as an offline preprocessing step. It does not run at game startup — processed assets are pre-baked and shipped with the application or downloaded on first run from a CDN.
Step 1: Download raw PDS data (IMG/TIFF + label files) from NASA PDS via HTTPS
Step 2: Convert to GeoTIFF using GDAL 3.x with projection to IAU 2015 lunar selenographic CRS (EPSG:30200 equivalent)
Step 3: Generate Cloud-Optimized GeoTIFF (COG) with appropriate overviews for streaming
Step 4: Upload to Cesium ion as raster overlay asset; configure color ramp and min/max value normalization
Step 5: Reference Cesium ion asset IDs in DataLayerManager configuration; assets stream at runtime based on camera position and zoom level
Data updates (e.g., new Diviner seasonal model): re-run from Step 1; Cesium ion asset ID remains stable, overlay updates automatically for all clients

## Cesium Configuration

Primary terrain: Cesium ion asset ID 2684829 (CesiumWorldTerrain lunar variant, LRO-derived)
Custom raster overlays: separate Cesium ion assets for each dataset (LEND, Diviner, M3, Mini-RF). Asset IDs documented in DataLayerConfig.json
Coordinate origin: Shackleton Crater center (approximately 89.9 degrees S, 0 degrees E) as Unity world-space origin to minimize floating-point precision loss
Tile cache: 512MB local SSD cache for terrain tiles; allows offline operation after initial load
LOD configuration: full-resolution terrain within 5km of camera; reduced LOD beyond 5km to maintain frame rate

## Performance Targets

1080p/60fps on a mid-range PC (RTX 3060 equivalent) in Operations Mode
1440p/45fps in Rover Mode with full terrain LOD
Simulation timestep: 1 simulated minute resolved in under 2ms on target CPU (Intel Core i7-12th gen equivalent)
Save file size: under 2MB for a fully developed base with 90 days of history

# 10. Scope & Milestones

Development is structured in three phases with clearly differentiated scope gates. Each phase produces a shippable, demonstrable product. No phase depends on aspirational features from a later phase. The MVP is useful as a standalone demo for funding conversations. V1.0 is the minimum viable product for agency evaluation. V2.0 is the commercial and agency licensing target.

| MVP    |    3-4 Months |
|---|
| • Real Shackleton Crater terrain via Cesium for Unity (asset 2684829)• Solar exposure simulation from SPICE sun angles — basic but accurate• Diviner temperature overlay (static model at peak/min values)• 5 placeable module types: Habitat, Solar Array, Battery Bank, Landing Pad, Rover Garage• Power simulation with cable routing and resistance loss• Basic rover with lunar gravity driving on actual terrain• Basic operations dashboard showing power and temperature trends• Slope constraint enforcement on module placement• Blast zone enforcement around landing pad• Simple alert system (2-tier: Nominal / Critical) |

| V1.0    |    6-8 Months |
|---|
| • LEND water ice probability overlay with LCROSS anchor point• Full ISRU simulation: drilling, water processing, electrolysis chain• Mineral deposit overlay from Chandrayaan-1 M3 data• Full thermal simulation using Diviner time-of-day model• Equipment degradation model: dust accumulation, micrometeorite impacts• 90-Day Preview simulation with anomaly markers• Full four-tier alert system with audio signatures• Campaign mode: 3-5 structured mission scenarios with win/loss conditions• Full HUD implementation (all four quadrants)• Data layer toggle system (all 6 layers)• Communications blackout calculation from terrain occlusion• Rover spectrometer with M3 data lookup |

| V2.0 / Agency Edition    |    12+ Months |
|---|
| • Real-time data updates from NASA PDS API (Diviner seasonal models, new instrument data)• Multiplayer collaborative planning mode: two planners, same base, conflicting priorities• Additional landing sites: Haworth, de Gerlache, Nobile Crater• Data export: base state and simulation history to CSV/JSON for external analysis• LunaNet communications architecture scenario pack• Chang'e-7 data integration (pending CNSA public release)• Formal evaluation partnership with a NASA or ESA planning office• Potential commercial/agency licensing for training use |

# 11. Differentiation & Competitive Landscape

LUNAR OPS occupies a specific and currently unoccupied position in the simulation landscape: real planetary science data, real terrain, real physics consequences, and explicit professional utility. The following comparison documents this position clearly.

| Product | Key Strengths | Key Weaknesses | Overlap with Lunar Ops |
|---|---|---|---|
| KSP2 (Kerbal Space Program 2) | Deep rocket engineering, orbital mechanics, physics simulation | No surface operations, no ISRU, fictional solar system, no real terrain data | Simulation genre audience; serious space enthusiasts |
| Surviving Mars | Excellent resource management loop, polished UI, strong commercial success | Fictional Mars analog, no real planetary data, no NASA utility angle | Colony management genre audience |
| Moonbase Alpha (NASA) | Real lunar terrain (small area), free, NASA-branded credibility | 2010 vintage, no real data integration, no resource simulation, no layout consequences | Demonstrates NASA's willingness to fund games for outreach |
| LUNAR OPS (this project) | Real terrain (LRO/LOLA), real science datasets, real physics consequences, professional HUD design, agency utility | Not yet built; less conventional "game" polish than Surviving Mars; niche audience | Serious sim enthusiasts, agency trainees, university programs |

## The Core Differentiating Statement

| What makes LUNAR OPS different in one sentence:Every other lunar simulation either uses fictional data, lacks a layout consequence system, or has no credible agency utility — LUNAR OPS has real NASA terrain and instrument data, a physics-grounded consequence engine, and a direct use case for the organizations actually planning to put humans on the Moon. |
|---|

## Strategic Positioning

The commercial simulation market (Surviving Mars-style) is not the primary target. The primary beachhead is the space agency and university training market, where the bar for data credibility is high but the tolerance for production polish is lower than consumer games. A V1.0 that is modestly polished but scientifically rigorous is more valuable to a NASA program office than a visually spectacular product running on invented data. The consumer audience follows naturally if agency credibility is established first.

</details>