# Projectile Yeet Mod

This is an experimental mod for Ghostlore that hides visual effects of projectiles that scale with the Increased Projectile Radius affix. The mod specifically targets projectiles with the `ScaleToMatchProjectileRadius` component and moves their visual sprites off-screen while preserving their damage functionality.

## How It Works

1. **Harmony Patching**: The mod uses Harmony to intercept Transform position setter calls
2. **Component Detection**: When a Transform's position is set, the mod checks if the GameObject has a `ScaleToMatchProjectileRadius` component
3. **Visual Hiding**: For matching projectiles, the mod moves visual sprite children off-screen (Y: -1000)
4. **Performance Optimization**: Uses caching to avoid repeated processing of the same transforms

## Files Structure

```
projectile-yeet/
├── ProjectileYeet.sln              # Visual Studio solution
├── ProjectileYeet/
│   ├── ProjectileYeet.csproj      # Project file
│   ├── ModLoader.cs               # Main mod entry point
│   ├── ProjectileYeetPatch.cs     # Harmony patches for Transform position
│   ├── ModInfo.xml                # Mod metadata
│   └── Mod Folder/
│       └── screenshot.png         # Mod screenshot
└── README.md                      # This file
```

## Usage

1. **Build the mod** using Visual Studio or MSBuild
2. **Enable the mod** in the game's mod manager
3. **Start the game** and projectiles with `ScaleToMatchProjectileRadius` component will have their visual sprites hidden automatically

## How It Works

The mod automatically detects and hides visual effects for projectiles that scale with the Increased Projectile Radius affix. No configuration is needed - it works automatically by:

1. **Monitoring Transform positions**: Intercepts when Transform positions are set
2. **Component detection**: Checks if the GameObject has a `ScaleToMatchProjectileRadius` component
3. **Visual hiding**: Moves visual sprite children off-screen while preserving damage functionality
4. **Performance optimization**: Uses caching to avoid repeated processing

## Technical Details

### Harmony Patches

- **TransformPositionPatch**: Intercepts `Transform.position` setter calls
- **Component Detection**: Checks for `ScaleToMatchProjectileRadius` component on GameObjects

### Visual Hiding Process

1. Transform position is set on a GameObject
2. Harmony patch intercepts the position setter
3. Mod checks if GameObject has `ScaleToMatchProjectileRadius` component
4. If found, moves visual sprite children off-screen (Y: -1000)
5. Preserves damage functionality while hiding visuals

## Limitations

- This is a proof-of-concept implementation
- Only affects projectiles with `ScaleToMatchProjectileRadius` component
- Visual sprites are moved off-screen but may still consume some resources
- Performance impact depends on the number of projectiles with scaling components
- May not work with all projectile types or visual effects
- May hide unintended projectiles

## Development Notes

- The mod uses the shared `libs` folder for game DLLs
- Harmony patches are applied during mod initialization
- Transform caching prevents repeated processing of the same objects
- Cache cleanup is handled when the mod is unloaded

## Future Improvements

- More sophisticated component detection algorithms
- Support for additional scaling components
- Better performance optimization for high projectile counts
- Enhanced error handling and logging
- Dynamic configuration options for different hiding strategies
