# Changelog

## 1.0.3

- Make the starter chest indestructible and prevent creatures from selecting it as a target.
- Prevent players from dismantling the starter chest as a normal building piece.
- Restore the starter chest automatically if its networked world object goes missing.

## 1.0.2

- Restore starter chest hover text and interaction by making its Unity behavior instantiable.
- Update starter-kit messages and item delivery for the current Valheim API.

## 1.0.1

- Update Jotunn to 2.30.2 for corrected Deep North asset mocking and legacy build-menu support.

## 1.0.0

- Promote the tested personalized starter chest to its first stable release.
- Keep the existing-world placement, per-player claim tracking, configured Use key, and duplicate cleanup fixes.

## 0.1.4

- Release the tested personalized starter chest for synchronized client and server installation.
- Add the protected Hexium publication workflow for the new gameplay repository.

## 0.1.3

- Preserve starter chest matches across incremental scan batches.
- Remove surplus starter chest ZDOs while retaining one canonical chest.

## 0.1.2

- Rewind local claim responses before reading them so interaction grants the starter kit.
- Complete existing-world chest detection across consecutive frames for prompt placement.

## 0.1.1

- Resolve the chest interaction prompt from the player's current Use binding.
- Prevent local hosts from recursively processing their own claim response.

| Version | Changes |
| --- | --- |
| 0.1.0 | Add the existing-world-safe, per-account starter chest foundation. |

