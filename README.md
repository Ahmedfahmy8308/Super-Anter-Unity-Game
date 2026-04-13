# Super Anter

![Unity](https://img.shields.io/badge/Unity-6000.4.2f1-black?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white)
![Type](https://img.shields.io/badge/Game-2D%20Platformer-2ea44f)
![Course](https://img.shields.io/badge/Course-VR%20Subject-0a66c2)

Super Anter is a professional Unity 2D platformer inspired by classic Super Mario Bros. It features Mario-style enemies (Goombas, Koopas), mystery blocks, pipes, flagpoles, tilemapped levels, a full audio system, and polished menus -- all generated and maintained from a unified editor tool.

## Academic Context
- Course: VR Subject
- Level: Fourth Year
- Faculty: Faculty of Computers and Information
- University: Kafrelsheikh University

## Author and Ownership
- Author: Ahmed Fahmy
- Email: fhmy8308@gmail.com
- GitHub: Ahmedfahmy8308

## Copyright
Copyright (c) Ahmed Fahmy. All rights reserved.

## Project Requirements
- Unity 6 (tested on 6000.4.2f1)
- Windows 10/11 recommended
- .NET/C# support from Unity installation

## Optional Reference Projects
Place these sibling folders next to `Super-Anter/` for automatic asset import:

| Folder | Purpose |
|---|---|
| `Super-Mario/Assets/Sprites/` | Mario-style sprites (Goomba, Koopa, Brick, Pipe, Flag, etc.) are auto-copied into `Assets/Graphics/Mario/` |
| `Super-Anter-Old-Material/` | Pixel Adventure audio and graphics -- only the 16 audio + 25 graphics files actually used are copied into `Assets/Audio/` and `Assets/Graphics/` |

If these folders are not present, the game still works with procedurally generated fallback sprites and placeholder colors.

## Setup After Clone
1. Clone the repository:
   ```
   git clone https://github.com/Ahmedfahmy8308/Super-Anter-Unity-Game
   ```
2. Open Unity Hub.
3. Click **Add project from disk** and select the cloned `Super-Anter-Unity-Game` folder.
4. Open the project in Unity 6 (tested on 6000.4.2f1).
5. Wait for Unity to finish package import and script compilation.

## First Run -- Auto Setup
1. Run the unified setup tool:
   **Tools > Super Anter > Run Setup...**

   In the dialog, choose **Full setup** for a first-time import (or **Data only (safe)** if you only need GameData/audio hooks without regenerating scenes).

   **Full setup** performs all of the following automatically:
   - Creates folder structure (`Assets/Audio`, `Assets/Graphics`, `Assets/Prefabs`, etc.)
   - Copies only needed audio and graphics from `Super-Anter-Old-Material/`
   - Imports Mario sprites from `Super-Mario/Assets/Sprites/`
   - Configures pixel art import settings (Point filter, no compression)
   - Creates all prefabs (Player, Goomba, Koopa, MysteryBlock, BrickBlock, Pipe, FlagPole, Coin, decorations)
   - Builds Systems and UICanvas bootstrap prefabs
   - Generates/maintains scenes: MainMenu, Level1, Level2, Level3
   - Wires all audio clips, sprites, and UI references
   - Updates Build Settings with correct scene order

2. Open Build Profiles (or Build Settings) and verify scene order:
   1. MainMenu
   2. Level1
   3. Level2
   4. Level3
3. Open **MainMenu** scene and press **Play**.

## Build And Distribution

### Unity build
1. Open **Build Profiles** (or **Build Settings**).
2. Platform: **Windows x86_64**.
3. Verify scene order:
   1. MainMenu
   2. Level1
   3. Level2
   4. Level3
4. Build to a folder (for example: `Super-Anter-exe/`).

### Single-file installer (Inno Setup)
To distribute as one installable file (`Setup.exe`) instead of many Unity files:
1. Install **Inno Setup**.
2. Use `superanter.iss` in the project root.
3. Compile script from Inno Setup.
4. Send the generated installer from `InstallerOutput/SuperAnterSetup.exe`.

## Gameplay Features

### Player
- Acceleration/deceleration movement
- Coyote time and jump buffering
- Stomp bounce on enemies

### Enemies
- **Goomba** -- patrols left/right, flattens when stomped from above, killed by shells
- **Koopa** -- patrols left/right, enters shell when stomped, shell can be pushed to kill other enemies
- **Legacy EnemyAI** -- waypoint-based patrol enemy (fallback)

### Level Elements
- **Mystery Blocks** -- hit from below to spawn coins, mushrooms, stars, or 1-UPs
- **Brick Blocks** -- solid blocks (breakable in future updates)
- **Pipes** -- decorative obstacles with colliders
- **Flagpole** -- score based on grab height, flag-slide animation, triggers level complete
- **Coins** -- collectible with score pop and 1-UP at 100 coins
- **Decorations** -- clouds, hills, bushes in background layer
- **Tilemapped terrain** -- ground with gaps, floating platforms, staircases

### UI
- Main menu with character preview, Start / Settings / Quit
- Main menu branding credit at bottom-right: `Developed by Ahmed Fahmy`
- Settings panel with music and SFX volume sliders (persisted via PlayerPrefs)
- HUD: Lives, Score, Coins, Timer (with warning flash)
- Game Over, Level Complete, Win, and Pause panels
- Fade transitions between scenes

### Audio
- Background music with star music override (streaming-friendly playback)
- Short SFX (mostly `.ogg`): jump, coin, death, stomp, kick, power-up, power-down, 1-UP, block bump, block break, pipe, flagpole, button click
- Long **stingers** (`.mp3` in `Assets/Audio/`): **`Congrats A'ntar.mp3`** (level complete), **`Game Over.mp3`** (game over) — played on a dedicated audio source so they are not affected by death slow-motion pitch on normal SFX
- Per-clip pitch variation on gameplay SFX
- Volume control via Settings UI

### Level & camera
- `CameraFollow2D` follows the player horizontally in both directions, with optional **left/right world bounds** so the view does not scroll past the level
- `LevelEdgeWalls` spawns invisible side colliders at those bounds so the player cannot walk off the stage

### Systems
- `GameManager` -- scene flow, lives, score, coins, timer, combo scoring, death routine
- `AudioManager` -- music/SFX/stinger playback, mixer support, volume persistence
- `UIManager` -- panel management, HUD updates, floating text popups
- `LevelCatalog` (ScriptableObject) -- defines scene order without hardcoded build indices
- `GameData` (ScriptableObject) -- tunable game parameters
- `GameDataBootstrap` -- loads runtime overrides from `game_data.json`

## Project Structure

```
Assets/
  Audio/              Music, SFX (.ogg), and long stingers (.mp3) for level complete / game over
  Graphics/           Pixel Adventure sprites (fallback art)
    Mario/            Mario-style sprites (auto-imported)
    Background/       Background tiles
    Menu/Buttons/     UI button icons
    ...
  Data/               GameData.asset, LevelCatalog.asset, game_data.json
  Editor/             SuperAnterMasterSetup.cs (auto-setup tool)
  Generated/          Auto-generated sprites, tiles, animator
  Prefabs/            Player, Goomba, Koopa, Coin, MysteryBlock, Pipe, FlagPole, etc.
   Scenes/             MainMenu, Level1, Level2, Level3
  Scripts/            All gameplay and system scripts
  Systems/            Systems.prefab (GameManager + AudioManager + UIManager)
  UI/                 UICanvas.prefab
```

## Gameplay Validation Checklist
1. Press **Start** from MainMenu.
2. Confirm movement and jumping in Level1.
3. Collect a coin and verify score increases.
4. Stomp a Goomba from above -- it should flatten and disappear.
5. Stomp a Koopa -- it should enter its shell; walk into the shell to push it.
6. Hit a Mystery Block from below -- a coin should pop out.
7. Reach the flagpole to trigger level complete with score bonus.
8. Complete Level3 to see the final win screen.
9. Open Settings and adjust volume sliders.
10. Press Escape during gameplay to pause.

## Notes
- Level complete and game over clips must stay at **`Assets/Audio/Congrats A'ntar.mp3`** and **`Assets/Audio/Game Over.mp3`** (names with space and apostrophe) so `Systems.prefab` and **Tools > Super Anter > Run Setup** can resolve them. If you rename them, update `SuperAnterMasterSetup.cs` and reassign clips on `AudioManager`.
- If sprites appear blurry, re-run **Tools > Super Anter > Run Setup...** (Full setup, or Data only for import settings) to fix import settings.
- Keep pixel art import settings at Filter Mode = Point and Compression = None.
- Canvas Scaler is set to Scale With Screen Size (1920x1080 reference) for responsive UI.
- The `game_data.json` file allows runtime tuning of player speed, jump force, enemy behavior, and audio settings without recompiling.
