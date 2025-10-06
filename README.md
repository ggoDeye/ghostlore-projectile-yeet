# Projectile Yeet Mod

This is an experimental mod for Ghostlore that attempts to modify or hide game sprites by intercepting sprite loading methods. Specifically designed to hide visual effects like projectiles, explosions, and AoE effects.

## Overview

The Projectile Yeet mod demonstrates how to:

- Use Harmony to patch Unity's sprite loading methods
- Hide specific types of visual effects (projectiles, explosions, AoE)
- Load custom sprites from the mod folder
- Replace game sprites with custom versions
- Handle sprite lifecycle management
- Configure which sprites to hide via JSON configuration

## How It Works

1. **Harmony Patching**: The mod uses Harmony to intercept calls to `Sprite.Create` and `Resources.Load`
2. **Sprite Hiding**: When a sprite is loaded, the mod checks if it should be hidden based on configuration
3. **Transparent Replacement**: Matching sprites are replaced with transparent versions
4. **Automatic Hiding**: The mod automatically hides visual effects based on naming patterns

## Files Structure

```
projectile-yeet/
├── ProjectileYeet.sln              # Visual Studio solution
├── ProjectileYeet/
│   ├── ProjectileYeet.csproj      # Project file
│   ├── ModLoader.cs               # Main mod entry point
│   ├── SpritePatcher.cs           # Sprite hiding logic
│   ├── SpriteLoadingPatch.cs      # Harmony patches for sprite loading
│   ├── SpriteConfig.cs            # Configuration class
│   ├── ModInfo.xml                # Mod metadata
│   └── Mod Folder/
│       ├── sprite_config.json     # Configuration file
│       └── screenshot.png         # Mod screenshot
└── README.md                      # This file
```

## Usage

1. **Build the mod** using Visual Studio or MSBuild
2. **Configure sprite hiding** by editing `sprite_config.json`
3. **Enable the mod** in the game's mod manager
4. **Start the game** and see visual effects hidden based on your configuration

## Configuration

The mod uses `sprite_config.json` to control which sprites are hidden:

```json
{
  "customHiddenSprites": [
    // List of specific sprites to hide
    "fireball_projectile",
    "ice_shard",
    "lightning_bolt"
  ],
  "enableDebugLogging": true, // Enable debug logging
  "logSpriteReplacements": true // Log when sprites are replaced
}
```

### Configuration Options

- **hideProjectiles**: Hides sprites containing "projectile", "bullet", "arrow", "missile"
- **hideExplosions**: Hides sprites containing "explosion", "blast", "boom", "burst"
- **hideAoEEffects**: Hides sprites containing "aoe", "area", "zone", "field", "aura"
- **hideParticleEffects**: Hides sprites containing "particle", "spark", "trail", "smoke"
- **customHiddenSprites**: Array of specific sprite names to hide
- **enableDebugLogging**: Shows debug messages in the console
- **logSpriteReplacements**: Logs when sprites are replaced or hidden

## Technical Details

### Harmony Patches

- **SpriteCreatePatch**: Intercepts `Sprite.Create` calls
- **ResourcesLoadPatch**: Intercepts `Resources.Load` calls for sprites

### Sprite Loading Process

1. Game requests a sprite
2. Harmony patch intercepts the call
3. Mod checks for custom version
4. Returns custom sprite if found, otherwise original sprite

## Limitations

- This is a proof-of-concept implementation
- Sprite replacement may not work for all game sprites
- Some sprites may be loaded through different methods
- Performance impact may vary depending on sprite count

## Development Notes

- The mod uses the shared `libs` folder for game DLLs
- Harmony patches are applied during mod initialization
- Custom sprites are loaded from the mod's Sprites folder
- Sprite cleanup is handled when the mod is unloaded

## Future Improvements

- More sophisticated sprite matching algorithms
- Support for sprite atlases
- Dynamic sprite loading from external sources
- Better error handling and logging
- Performance optimizations for large sprite sets
