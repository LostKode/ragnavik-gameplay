# Starter chest design

## Guarantees

* The placement job runs on the server after the world is loaded.
* `StartTemple` is resolved from the world's location registry rather than
  assumed to be at the origin.
* Existing ZDOs are searched before placement, preventing duplicates across
  restarts.
* Claims are keyed by platform account and `KitId` and persisted as world
  global keys.
* The claim is recorded before the server authorizes delivery.
* Full inventories cause undelivered stacks to drop at the claiming player's
  feet rather than disappear.

## Test gates

1. Build against the current Valheim 1.0 managed assemblies and Jotunn 2.30.1.
2. Create an isolated Gale profile named after the task branch.
3. Test a copied existing world whose StartTemple was previously generated.
4. Confirm one chest ZDO exists after repeated server restarts.
5. Claim on two separate Steam accounts and verify personalized delivery.
6. Reconnect and recreate a character on one account and verify denial.
7. Fill an inventory and verify overflow drops safely.
8. Change `KitId` in Test and verify one new claim becomes available.

No pack update, publication, or Production deployment is authorized by this
design document.
