# Corridor Commander Portfolio Notes

## One-Line Pitch

Corridor Commander is a Unity 3D corridor-defense MVP where the player installs defensive objects, survives waves, opens gates, and reaches a mission clear settlement screen.

## What To Show First

1. Start menu to main scene.
2. Player movement, aiming, and interaction.
3. Install menu from placement points.
4. Turret, mortar, and barricade placement.
5. Enemy wave defense.
6. Reward or shop UI.
7. Gate opening and next-room progression.
8. Mission clear and final settlement screen.

## Engineering Highlights

- `IBuildableInstallable` and `BuildContext` separate buildable object install contracts.
- `BuildableDefinitionSO` connects install menu data to actual prefabs.
- `MainCanvas` owns in-game UI flows such as shop, reward, pause/options, and settlement.
- Scene/prefab wiring is prioritized over code-only UI generation.
- Build packaging includes a controls README for reviewers.

## Important Paths

- Unity project: `Corridor_Commander/`
- Main scope: `Corridor_Commander/Assets/hansol`
- Scenes: `Corridor_Commander/Assets/hansol/01_Scenes`
- Gameplay scripts: `Corridor_Commander/Assets/hansol/02_Scripts`
- UI prefabs: `Corridor_Commander/Assets/hansol/03_Prefabs/UI/InGame`

## Validation Baseline

- Open with Unity `6000.3.9f1`.
- Confirm scenes and prefabs under `Assets/hansol`.
- Run the included Windows build from `PortfolioBuild/`.
- Confirm the zip contains `조작법_README.txt`.
