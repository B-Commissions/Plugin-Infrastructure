# BlueBeard.Zones Wiki

BlueBeard.Zones is an advanced zone management system for Unturned servers running RocketMod. It provides spatial zones with trigger-based player detection, persistent storage, a flag system for enforcing rules (no-build, no-damage, PvP protection, etc.), block lists, and a full command tree for in-game administration.

It works both as a **standalone plugin** and as a **library** for other plugins to build on.

---

## User Guide

- [Installation](Installation)
- [Getting Started](Getting-Started)
- [Commands](Commands)
- [Flags](Flags)
- [Block Lists](Block-Lists)
- [Permissions](Permissions)
- [Configuration](Configuration)

## Developer Guide

- [Developer Quick Start](Developer-Quick-Start)
- [Zone Definitions](Zone-Definitions)
- [Shapes](Shapes)
- [Events](Events)
- [Player Tracking](Player-Tracking)
- [Storage](Storage)
- [Flag System Internals](Flag-System-Internals)

---

## Features

- **Radius and polygon zones** with Unity trigger colliders
- **Persistent storage** via JSON files or MySQL
- **26 flags** covering damage, access, building, items, environment, notifications, effects, and group management
- **Block lists** for fine-grained item/build restrictions
- **Height bounds** for vertical zone limits
- **Zone priority** for resolving overlapping zones
- **Permission overrides** per-flag and per-zone
- **Interactive polygon builder** for creating complex zone shapes in-game
- **Console and in-game commands** for full administration
- **Vehicle support** with automatic passenger detection
- **Library API** for other plugins to create and manage zones programmatically
