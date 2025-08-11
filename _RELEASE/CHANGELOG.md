# Change Log

All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/).

## [0.4.1]
- Added credits for free unity asset used in VoidRemote
- Various Readme fixes
- Fixed null reference errors in relation to reloading a previous save that had the Keep Items upgrade unlocked
- Fixed hosts getting double the upgrade levels for the keep items upgrade after the first initial upgrade


## [0.4.0] *many fixes and one new item*
- Fixed issues with the void asset having a missing prefab (no idea how/when this happened)
- Added new handling for host-based config items.
	- Each host-based config item has been identified and their values will be networked to other players (if necessary)
	- Config sync will be done once per lobby load and for individual items that are changed by the host mid-game
- General code cleanup in relation to the keep items upgrade.
	- Will now directly get upgrade count from the base game upgrade manager rather than tracking via a separate value
	- Should hopefully fix some of the various issues reported where the upgrade stops working intermitently or entirely.
- Removed custom teleport method for base game's teleport method used by photonTransformView
	- Should make teleportation of items/players more consistent
- Added new item ``Void Remote`` which allows you to lock/unlock the void exit for all players.
	- This item will spawn as an item in the "Secret Shop" (the attic)
	- Like the other items this mod adds, added rarity and price values are configurable.
	- When locked in the void, the player will chat to the truck similar to when the truck is leaving.  

## [0.3.5] *assets rebuilt for beta + various bug fixes*
 - Fixed issues presented by the first beta version of the game.
	- The plus version of the pocket cart has been completely remade.
	- No longer has a different color from the original pocket cart.
 - Various bug fixes from 0.3.0 that I've had to hold onto until I could fix the asset to work with beta
 - Added hintui element for the deposit items keybind
	- will also be used for another keybind that is not ready for this update ~~abandon players~~  

## [0.3.0] *void update + bug fixes*
 - Added new pocket dimension void to transport items to.
	- This should fix the somewhat inconsistent issue of map icons still not being removed from the radar.
	- With ``Player Safety Check`` disabled, players will be transported to the void.
	- With ``Ignore Enemies`` disabled, enemies will be frozen and NOT transported to the void.
	- Pocket dimension is physically located at (999, 0, 0), this can be updated if any level (custom or vanilla) ends up occupying these coordinates
 - Added new config item ``Allow Deposit``
	- When enabled, allows you to hold alt when pocketing a pocket cart to *not* pocket items with the cart.
	- Essentially functions as a deposit items key-bind.
	- This control is not re-mappable currently, that may change in later updates.
	- I may also add a UI element hinting at the control in a future update
	- Added for feature request mentioned in [github issue#9](https://github.com/darmuh/PocketCartPlus/issues/9)
 - Removed map icon patching as it was inconsistent and there's no reason to hide them when the items are located in the void. [github issue#5](https://github.com/darmuh/PocketCartPlus/issues/5)
 - Likely fixed some of the common errors you'd see in console when pocketing an enemy [(github issue#7)](https://github.com/darmuh/PocketCartPlus/issues/7)
 - Fixed player spawn patch to work for clients by changing patch to CameraAimSpawn, the original patch only worked for host client.
 - Added patch for LeaveToMainMenu to reset upgrade progress. This should be loaded when you spawn in on a new save
 - Fixed players losing their clipping and endlessly dying when ``Player Safety Check`` is disabled
	- during initial testing there was one tester who asked for this interaction to stay, however, i've decided to remove this since it essentially breaks a player until they leave and join back
 - Most likely fixed save loading issue where even if the upgrade was shown as enabled it wouldn't enable the required behaviors.
	- Thanks to the multiple github reports on this issue I was able to determine what part of the game patching was failing.
	- While i'm not certain, i'm hopeful this will also fix a similar issue that late-join users were experiencing.
	- Adjusted patching to reset progress when leaving to main menu.
	- Moved methods relating to this unlock out of the PocketCartUpgradeItems class as this class depends on the box being spawned (which on a save reload it will not be spawned)
 - Fixed rarity config item not working
	- Ended up reworking how these values are utilized in-game even though the keep items upgrade rarities were working before.
	- Also now if an item is removed from the list due to the add-on rarity another random item of the same type will spawn in it's place

## [0.2.2] *repolib update*
 - Updated for repolib version 2.0.0
 - Added a reset to the cartsstoringitems counter when a player spawns.
	- Hopefully fixes an uncommon issue of the upgradelevels configitem making the upgrade unusable sometimes

## [0.2.1] *hotfixes*
 - Hopefully fixed issue of ``Upgrade Levels`` detecting the incorrect amount of carts stored and subsequently making it so the upgrade doesn't work
	- This mostly happened in multiplayer lobbies so it's hard to reproduce on my own. Please let me know if you still experience this issue.
 - Fixed ``Unlock without Upgrade`` eating items when the upgrade hasnt been purchased (sorry)
 - Fixed readme formatting issues introduced in last update

## [0.2.0] *Slight Rework*
 - Updated asset bundle.
	- Keep Items upgrade now will glow blue and the upgrade effect will be a blue cloud
	- Added new item "cart small plus"
 - Completely reworked networking to use standard Photon RPCs.
	- There were some underlying issues with the event system i'd rather not continue working around.
	- Since this is a rework, please let me know if you run into any issues during multiplayer testing.
 - Also reworked save integration.
	- Should be backwards compatible with existing saves
 - Updated default configuration items and configuration descriptions to better reflect what is more or less jank/buggy.
	- For example, I have gotten a handful of reported issues with the interaction of storing enemies with the pocket cart.
		- The default for ``IgnoreEnemies`` has been changed to true while I try to resolve some of the more common issues with storing enemies.
 - Added new CartManager class to help my code better reflect when a cart has stored items, is storing items, or is not allowed to store items.
 - Added new config item for keep items upgrade - ``Show On MiniMap`` (with associated patching) that will allow you to hide the minimap dot for this upgrade.
 - Hopefully fixed items showing on the minimap even after they were "stored" with a pocket cart
 - Added new config item for keep items upgrade - ``Upgrade Levels`` that will make it so each upgrade will allow you to store items with *only* that many pocket carts. (slight nerf, adds a point to buying the upgrade multiple times)
 - Added new buyable shop item ``POCKET C.A.R.T. Plus`` *with two rare variants that can spawn sometimes after purchase*
	- This will be a larger, blue variant of the pocket cart.
	- The base version of this item will be a 125% scaled version of the pocket cart.
	- Has different rare variants that can spawn as 150% or even 175% scale
	- The rare variants will *never* be displayed in the shop, as the variant type is calculated each time the object is spawned.
		- This means the rare variant will only last for the current level.

## [0.1.3]
 - Added some handling for NRE that would occur with the ``ClientsUnlocked`` string that tracks which clients have the upgrade unlocked.
	- I'm still not entirely sure why this NRE occured.
	- Added some warning logs that will trigger when a null item is trying to be added as well.
 - Adjusted price config patch
 - Added ``Rarity Percentage (Add-on)`` config item. This is added on to the base-games rng system.
	- If rarity percentage determines the item should not spawn, will be removed from the list of potential upgrades.
 - Added map icon back to item. (May add a config item in the future to remove it)

## [0.1.2]
 - Fixed bundle loading issue where REPOLib would load the item bundle *before* the plugin had finished initializing.

## [0.1.1]
 - Fixed REPOLib manifest version
 - Removed giant satellite icon from upgrade asset
 - Fixed a typo in readme

## [0.1.0]
 - Initial release for public testing.