# Third-party notices

Haven: Life on Source is released as two zips:

- `HavenLifeOnSource-x.y.z.zip`: only the mod (`HavenLifeOnSource.dll`) and its documentation.
- `HavenLifeOnSource-x.y.z-with-BepInEx.zip`: the same, plus an **unmodified** copy of [BepInEx 5.4.23.2](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2) for Windows x64, so players can install everything in one step.

The bundled BepInEx files are exactly as published in the official `BepInEx_win_x64_5.4.23.2.zip` (SHA-256 `f752ce4e838f4c305b9da1404b6745f2cff23b8bfd494f79f0c84d0a01f59b46`). They are not stored in this repository; the packaging script downloads and verifies them when building a release. Full license texts are included in the bundle under `HavenLifeOnSource/licenses/` and in this repository under [`third_party/licenses/`](third_party/licenses/).

| Component | Files in the bundle | Owner | License | Source code |
| --- | --- | --- | --- | --- |
| BepInEx | `BepInEx/core/BepInEx*.dll`, `HarmonyXInterop.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt` | BepInEx team | [LGPL-2.1](third_party/licenses/BepInEx-LGPL-2.1.txt) | [github.com/BepInEx/BepInEx](https://github.com/BepInEx/BepInEx/tree/v5.4.23.2) |
| UnityDoorstop | `winhttp.dll` | NeighTools | [LGPL-2.1](third_party/licenses/UnityDoorstop-LGPL-2.1.txt) | [github.com/NeighTools/UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) |
| HarmonyX | `BepInEx/core/0Harmony.dll`, `0Harmony20.dll` | BepInEx team, based on Harmony by Andreas Pardeike | [MIT](third_party/licenses/HarmonyX-MIT.txt) | [github.com/BepInEx/HarmonyX](https://github.com/BepInEx/HarmonyX) |
| MonoMod | `BepInEx/core/MonoMod.*.dll` | 0x0ade and contributors | [MIT](third_party/licenses/MonoMod-MIT.txt) | [github.com/MonoMod/MonoMod](https://github.com/MonoMod/MonoMod) |
| Mono.Cecil | `BepInEx/core/Mono.Cecil*.dll` | Jb Evain, Novell | [MIT](third_party/licenses/Mono.Cecil-MIT.txt) | [github.com/jbevain/cecil](https://github.com/jbevain/cecil) |

Not included in any release:

| Component | Owner | License | Notes |
| --- | --- | --- | --- |
| *Haven* (game, code, assets, story, characters) | [The Game Bakers](https://www.thegamebakers.com/) | Proprietary | Users need their own copy. This project is not affiliated with or endorsed by The Game Bakers. |
| [Ink runtime](https://github.com/inkle/ink) | inkle Ltd | MIT | Ships with the game. Referenced at build time only. |

The mod's own code is MIT-licensed (see [LICENSE](LICENSE)). It uses BepInEx as a separate library and does not modify it. The mod's source refers to the game's internal class names, story knot names and IDs only so the mod can work alongside the game.
