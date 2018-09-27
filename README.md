# Tedious Travel

A travel mod for daggerfall unity to make traveling more tedious. Because there's weirdoes like me who find that strangely immersive.

In explicit, the classic daggerfall "click-and-be-there" style of fast travel is removed from the game. 
Instead, you'll actually be traversing daggerfall, with a convienient "autopilot" and adjustable time compression.

A low-level character will require about 4 minutes real time for a good days travel. I made it from Gothway Garden to Daggerfall in about half an hour real time on horseback, 
though at least half of that time was spent chatting people up at ins, which are now suddenly very, very useful landmarks.

## How to build

Check out the daggerfall unity source code, then check out this repository into the Assets/Untracked folder.
In the unity editor, open the mod tools, load in tedioustravel-dfmod.json, and click build.

## How to play

You can open the travel map normally and click on a destination you want to travel to.
Instead of ye olde travel options, you'll see an estimated travel time and a prompt whether to start travelling or not.
If you answer yes, you'll be taken back to the game, where your character starts heading in the right direction.

In the top you'll see a menu where you can adjust your time compression, interrupt your travel (stop, but do not clear the selected destination), 
cancel (stop and clear the destination) or take you back to the map to see where you are. Pressing esc will do the same as the interrupt button.
You can at any time select a new destination in the map. If you exit the map without setting a new destination, any old destination you might have set will be preserved.

If you interrupted your travels, or got interrupted by enemies, and press the button for your travel map, you will first be asked if you want to resume travel to your current destination.
If you answer no, you will be taken to the map as usual, if you answer yes, you will immediately resume travel without going to the map first.

### About travel duration

First off, the estimated travel duration will depend on your base speed when you open the map. 
If you sit on a horse, the duration will be a lot less (usually about half) of what it would be if you opened the map on your feet.
If you are currently on performance enhancing drugs (aka potions) that will wear of in a few minutes, the estimated time will be highly misleading. 
Self-overestimation is a common sideeffect of drug use, after all... ;)

The travel time for the first 24 hours will be shown as hours of *pure travel time* (no rest). Times above 24 hours will be shown in days, but assuming 8 hours of actual travel per day.
This is because...

### Endurance matters!

Daggerfall has an endurance cost for just walking, and it also applies while riding. You don't really notice this during the game, but this mod brings it up rather painfully.
A low-level character with an endurance of 50 will in fact be able to walk just shy above 8 (in-game) hours straight before kissing the ground. 
Plan your trip according to your abilities, and use ins and towns to rest.
Or just travel as the crow flies and rest in the open, where you'll more likely than not get chewed on by a grue.

### Horses matter, too!

No, seriously. If you're starting a new character and plan on using this mod, a Horse should be at the absolute top of your list of things to buy before going anywhere much.

## Known issues

* The framerate at higher time compression is atrocious. Not much I can do about that, but your mileage may vary based on how powerful your PC is.
* Setting time compression too high will eventually make you outrun the loading of the map around you, and you'll fall of the edge of the world.
I have not currently set a hard max, because I want people with various systems to try out and see how far they can take it before deciding on a maximum setting.
I have a lower mid-range laptop, and my journey into the void starts somewhere at 25x on horseback.
* If your travel path takes you straight through another town, it can happen that you get stuck on an L-shaped building. Just interrupt your travel, dislocate yourself from the wall 
(which by now you'll be attempting to climb), walk out of the town and resume your travel.
* The mousecursor will periodically reset to the center of the screen while travelling. It's a major hack to keep an interactible menu open while the game keeps running, so that seems a small price to pay.
