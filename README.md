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

### Tedious Travel

You can open the travel map normally and click on a destination you want to travel to.
Instead of ye olde travel options, you'll see an estimated travel time and a prompt whether to start travelling or not.
If you answer yes, you'll be taken back to the game, where your character starts heading in the right direction.

In the top you'll see a menu where you can adjust your time compression, interrupt your travel (stop, but do not clear the selected destination), 
cancel (stop and clear the destination) or take you back to the map to see where you are. Pressing esc will do the same as the interrupt button.
You can at any time select a new destination in the map. If you exit the map without setting a new destination, any old destination you might have set will be preserved.

If you interrupted your travels, or got interrupted by enemies, and press the button for your travel map, you will first be asked if you want to resume travel to your current destination.
If you answer no, you will be taken to the map as usual, if you answer yes, you will immediately resume travel without going to the map first.

#### About travel duration

First off, the estimated travel duration will depend on your base speed when you open the map. 
If you sit on a horse, the duration will be a lot less (usually about half) of what it would be if you opened the map on your feet.
If you are currently on performance enhancing drugs (aka potions) that will wear of in a few minutes, the estimated time will be highly misleading. 
Self-overestimation is a common sideeffect of drug use, after all... ;)

The travel time for the first 24 hours will be shown as hours of *pure travel time* (no rest). Times above 24 hours will be shown in days, but assuming 8 hours of actual travel per day.
This is because...

#### Endurance matters!

Daggerfall has an endurance cost for just walking, and it also applies while riding. You don't really notice this during the game, but this mod brings it up rather painfully.
A low-level character with an endurance of 50 will in fact be able to walk just shy above 8 (in-game) hours straight before kissing the ground. 
Plan your trip according to your abilities, and use ins and towns to rest.
Or just travel as the crow flies and rest in the open, where you'll more likely than not get chewed on by a grue.

#### Horses matter, too!

No, seriously. If you're starting a new character and plan on using this mod, a Horse should be at the absolute top of your list of things to buy before going anywhere much.

### Fast Travel by ship

As of 0.2.0, Tedious Travel has a limited form of fast-travel. Fast-Travel by ship is now offered between port towns.

#### Port Towns
In case you didn't know, Daggerfall, while not having any visual representation for ports, does actually know which ports have a port. 
I'm not sure what exactly that is used for other than letting you buy ships only in port towns.
A bt counterintuitively, there are actually many port towns inland. I'll just assume that those are mostly located at navigable rivers, since Daggerfall has absolutely 
no representation or awareness of those. Also, there's not only towns having that feature. Some farmsteads and homes, even some dungeons, appear to have a port, though the DFUnity 
sourcecode tells me that this flag is also used for some as of yet unknown purpose. So it's possible I'm counting some locations as having a port that were not intended to, but it's fine,
since that allowed me to put a little bit more complexity in (see Travel times and accessability).

#### Finding and traveling between port towns
There is a new filter button labeled "ports" on the *world* map. It will vanish as soon as you zoom into a location, because it takes up the space some locations need for the navigation buttons.
So if you want to see port towns, press that button in the world map, *then* zoom in to a desired location. The other filter buttons will vanish, as this is a global override.
You will see all ports on the map, regardless of their type, and no locations without a port, also regardless of their type. Those types still have some significant influence though, see 
"Travel times and accessability".

In any case, when you click on a port town *when* the port filter is active, and *when* you are currently in a location with a port yourself, you will get a popup with a travel time and a fare.
If you click yes, and have the money, you will travel to the targeted port instantly (in-game time will still pass, of course). If you are not in a location with a port, you will get the normal
travel popup and continue your journey over land. If you are in a port town, and your destination is a port town, but the ports filter is not active, tedious travel will assume that you want 
to travel there the standard, tedious way.

#### Travel times and accessability
Travel times depend on how "accessible" both your start and destination port are. Which right now is just estimated by their type and size.
This is to emulate the fact that from a small village, there might not be many ships available sailing to or from it. You might have to wait a while, or lay over and jump ship along the way.
So your journey will take longer, and be more expensive. Both start and destination modify the time. So your voyage will take longest when traveling from a hamlet to a hamlet, and fastest when traveling from city to city.

But not all types of ports even have regular traffic. Homes (except wealthy) and dungeons are not visited by merchant ships regularly, so you won't find any ships sailing for these ports or leaving them. For all intents and purposes, they are useless to you. That is, unless you have your own ship, with which you can sail wherever the winds can take you. Time penalties for inaccessible locations as well as fares
also don't apply in this case. I thought this was a nice opportunity to give ship owners a bit more advantages for their considerable investment.

Port accessibility is as follows (obviously, this only applies to locations of this type that *have* a port):

* Cities: Highly accessible, no time penalties.
* Inns: Quite accessible, only slight time penalties.
* Villages and temples: Reasonably accessible, medium time penalties.
* Hamlets and wealthy homes: Only sporadic traffic, high time penalties.
* Other homes, dungeons, cults: No regular shipping, only accessible with own ship.

Travel times are otherwise still calculated by the default travel time calculator. I'm not yet sure how smart this is, but it does lead to travel to and from locations far inland taking longer
than between locations at the coast, or at least appears to be, which seems reasonable. If you've ever navigated rivers, you know why (Yes, I have a few times, even with a sailing boat).

#### Blackhead on the Isle of Balfiera
For some weird reason, the main island in the Isle of Balfiera region doesn't have a single port town, while all other (smaller) islands in the game have several. 
I had to promote one, or the island would be inaccessible when using tedious travel. So I promoted Blackhead. I'm mentioning this because Blackhead is a port town only in the context of Tedious Travel.
Daggerfall doesn't know anything about the promotion, so you won't be able to buy a ship there, for example.

## Known issues

* The framerate at higher time compression is atrocious. Not much I can do about that, but your mileage may vary based on how powerful your PC is.
* Setting time compression too high will eventually make you outrun the loading of the map around you, and you'll fall of the edge of the world.
I have not currently set a hard max, because I want people with various systems to try out and see how far they can take it before deciding on a maximum setting.
I have a lower mid-range laptop, and my journey into the void starts somewhere at 25x on horseback.
* If your travel path takes you straight through another town, it can happen that you get stuck on an L-shaped building. Just interrupt your travel, dislocate yourself from the wall 
(which by now you'll be attempting to climb), walk out of the town and resume your travel.
* The mod is not water-aware. If you run into a lake, you'll just keep going.
* Your vitals on the hud will only update when travel is interrupted, so be careful when sick, and don't get too cocky about your travel distances.