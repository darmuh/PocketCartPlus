# Change Log

All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/).

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