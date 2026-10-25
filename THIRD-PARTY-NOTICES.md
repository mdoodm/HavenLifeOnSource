# Third-party notices

Haven: Life on Source does not redistribute any third-party code or game content. The release zip contains only `HavenLifeOnSource.dll` and documentation. At runtime, the mod depends on the following, which users install themselves:

| Component | Owner | License | Notes |
| --- | --- | --- | --- |
| *Haven* (game, code, assets, story, characters) | [The Game Bakers](https://www.thegamebakers.com/) | Proprietary | Not included. Users need their own copy. This project is not affiliated with or endorsed by The Game Bakers. |
| [BepInEx](https://github.com/BepInEx/BepInEx) 5.x | BepInEx team | [LGPL-2.1](https://github.com/BepInEx/BepInEx/blob/master/LICENSE) | Not included. Installed separately by the user. |
| [HarmonyX](https://github.com/BepInEx/HarmonyX) | BepInEx team / Andreas Pardeike (Harmony) | [MIT](https://github.com/BepInEx/HarmonyX/blob/master/LICENSE) | Not included. Ships with BepInEx. |
| [Ink runtime](https://github.com/inkle/ink) | inkle Ltd | [MIT](https://github.com/inkle/ink/blob/master/LICENSE.txt) | Not included. Ships with the game. Referenced at build time only. |

The mod's source refers to the game's internal class names, story knot names and IDs. It does that only so the mod can work alongside the game.
