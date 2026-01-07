# 🔦 Headlamp Mod for My Winter Car

A MelonLoader mod that adds a toggleable headlamp for those dark Finnish winter nights.

## Features

- **Press G** to toggle headlamp on/off
- Smooth feathered light edges with custom cookie texture
- Follows your view direction
- Soft shadows for realistic lighting
- Persists across scene changes

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) for My Winter Car
2. Download `Headlamp.dll` and `headlamp_cookie.png` from the [latest release](https://github.com/c0mparn/headlamp/releases)
3. Place both files in the game's `Mods` folder
4. Launch the game and press **G**!

## Configuration

Edit the constants in `HeadlampMod.cs` to customize:

| Setting | Default | Description |
|---------|---------|-------------|
| `ToggleKey` | G | Key to toggle light |
| `LightRange` | 35m | How far the light reaches |
| `SpotAngle` | 90° | Width of the light cone |
| `LightIntensity` | 1.5 | Brightness |

## Building from Source

```bash
dotnet build -c Release
```

The DLL is automatically copied to the Mods folder.

## License

MIT
