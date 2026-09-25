# Ragnavik Gameplay

Ragnavik Gameplay contains modular, server-authoritative gameplay features for
the Ragnavik Valheim server. It is separate from Ragnavik Server Bridge, which
is limited to authenticated reporting and bot integrations.

## Starter chest

The first module places a persistent starter chest beside the existing
`StartTemple` location. Placement happens at runtime, so the module supports
worlds that were created before the mod was installed.

The chest does not expose one shared vanilla inventory. Interacting with the
same visible chest requests a personalized kit from the server. The server
records one claim per platform account in the world's global keys before it
authorizes item delivery.

Default kit:

* Wood: 20
* Stone: 10
* Raspberry: 10
* CookedMeat: 5

This is development work. It has not been added to either pack, published, or
deployed to Production.


## Profession experience

With Professions 1.4.7, professions configured to block unselected experience instead grant 50% skill XP when unselected and 100% when selected. Both can reach the normal maximum level. Other skills and EpicMMO character XP are unchanged. Selecting a profession changes the rate immediately; previously earned levels remain intact.
