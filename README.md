# Corridor Commander

Unity 3D corridor-defense MVP for portfolio review.

The player moves through connected corridor rooms, installs defensive objects, survives enemy waves, opens gates, and reaches a mission clear / final settlement flow.

## Portfolio Links

- Windows build zip: [Google Drive download](https://drive.google.com/file/d/1dHVtDKgETCLsThsMmeswfj3E63z64yZ-/view?usp=drivesdk)
- Gameplay video: [Google Drive watch link](https://drive.google.com/file/d/1GyZJLb9kSIVN1iCcQnK5zuSc-nNSLZnu/view?usp=drive_link)
- Presentation 1: [Google Slides / PPT](https://docs.google.com/presentation/d/1579NT0hhQuaNCDzXyZhSzl37Eex6q3nc/edit?usp=drive_link&ouid=102732633125904936006&rtpof=true&sd=true)
- Presentation 2: [Google Slides](https://docs.google.com/presentation/d/1NOByJ79yPvUn_pBVQOPu-5o4Vlr-AZosu3-prOZ6jY8/edit?usp=drive_link)

The build zip includes a controls README text file with basic controls and play-flow notes.

## Controls

- Move: `WASD`
- Look / aim: mouse
- Interact / open install menu: `E`
- Select / confirm: left mouse button
- Cancel / back / pause: `ESC`

## Project Focus

- Gameplay/client programming in Unity
- Scene and prefab-driven UI wiring
- Placement/buildable object flow
- Wave, enemy route, reward, shop, gate, and extraction flow
- Portfolio-ready documentation and build packaging

## Key Systems

- Build placement: `PlacementPoint`, `BuildableDefinitionSO`, `IBuildableInstallable`, `BuildContext`
- UI flow: `MainCanvas`, support truck shop, reward popup, pause/options, mission clear settlement
- Stage flow: gate activation, room progression, final extraction objective
- Combat flow: wave director, enemy spawn/route flow, turret/projectile behavior

## Repository Policy

This repository is kept portfolio-focused. Generated Unity folders and local-only files are excluded:

- `Library/`
- `Temp/`
- `obj/`
- `Logs/`
- `UserSettings/`
- generated build folders
- local captures, recovery files, and editor cache

Large executable builds are kept outside Git and linked from Google Drive or GitHub Releases so the repository stays light.

## Unity

- Project folder in this repository: `Corridor_Commander/`
- Main development scope: `Assets/hansol`
- Unity version used locally: `6000.3.9f1`
