# Change Log

All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/).

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