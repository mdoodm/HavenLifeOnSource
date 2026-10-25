# Changelog

## 1.4.2
- The cockpit's ship launch option no longer appears after the ending. The ship has already taken off, and the option fell through to the zone 6 take-off and repair dialogue. New setting: `DisableShipLaunch`.

## 1.4.1
- Fixed Kay and Yu staying in the epilogue's couch (or bed) pose in the Nest after the ending. The game's normal, context-based idle animations are back. New setting: `RestoreNestIdles`.

## 1.4.0 – named "Haven: Life on Source"
- The plugin is `HavenLifeOnSource.dll`, with GUID `lifeonsource.haven` and config `lifeonsource.haven.cfg`.
- Saves stay compatible. A Flow Bridge Stabilizer you've already built stays built.
- Added a clear note that this is an unofficial mod, not affiliated with The Game Bakers.
- Once the flow bridge is severed, the roaming chatter about Ozias coming to Source is skipped and the next variant plays instead. The temporary text-only replacement lines are gone.

## 1.3.0
- First version of the Ozias chatter change (text-only replacement lines).

## 1.2.0
- New **Flow Bridge Stabilizer** at the Enhance bench (1× Hyperrust) to cross flow bridges after the ending. You get 1 Hyperrust if you have none.
- `BridgeAccess` setting: `RequireStabilizer`, `AlwaysOpen` or `Vanilla`.
- Optional bridge-crossing checkpoint (`YuBridgeHesitance`) for future content.

## 1.1.0
- Nest activities work again after the ending.
- The epilogue's save block (`Epilogue_PreventGameSave`) is lifted after the ending, so the game saves again.

## 1.0.0
- You stay in the world after the epilogue's final scene and credits instead of returning to the main menu.
