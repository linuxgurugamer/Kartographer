Kartographer is a collection of utilities to make the map view easier to use. 

# Contents
1. Installation
2. Utilities
3. Basic Usage

# Installation
Take the contents of GameData in the archive and copy it into <KSP>/GameData. You should have <KSP>/GameData/Kartogrpaher/ when you
are done.

# Utilities
## WarpTo
Gives you precise control over the "Warp To" capability. You can warp to any point in the future. It will detect maneuvers and SOI 
transitions and add buttons to warp straight to them. This works in the map view, tracking station, and in flight.

## Vessel Select
Gives you a list of vessesl ordered by proximity. You can target or switch to any vessel in the list. You can also clear the current 
target. 

This utility also allows you to "Unleash the Kraken" on you current vessel. It will destroy a random part on your vessel until it is gone. 
This is a far more satisfying way of clearing debris than terminating it in the tracking station.

## Focus Select
Change the map view focus with the click of a button. It will detect bodies on your current or planned trajectory and present those as a
short list. You also have the ability to pick from any celestial body from a list.

## Maneuver Editor
Gives you full control over your maneuver nodes. Precisely control the time and delta-V of your maneuvers. Store your maneuvers,
try something else, and if it doesn't work restore the original set.

# Basic Usage
1. To launch a utility click on the Kartographer icon on the toolbar. It is a satellite with a compass and an quarter planet with an orbit.
If you don't see it you probably aren't in a scene that can use any of the utilities.
2. Click on the utility you want to show or hide.
3. The utility launcher will auto close unless you keep your mouse over it.


## Warp To
When first loaded Warp To is set for the current time, which isn't very useful, but you have to start somewhere. The current time
target time, and delta between the two is at the top of the utility. If there is an active or selected vessel you will see events in the
vessel's path. This includes maneuvers, SOI transitions, and orbits clicking on one will set the warp target to that point in time. 
Orbits add or subtract a full orbital period to the time. Next is the generic time controller which will always be present. It has 
"Finer" and "Coarser" buttons that change the granularity of the next set of buttons. Below this will be a set of buttons that will add a
certain amount of time to the warp time. Below this is a set of buttons to subtract from the warp time. Below this is a button to set the
warp time to 10 minutes from now. Once you've set the time you want to warp to click on "Engage" to start the warp.

## Vessel Select
First you have options to toggle debris and asteroid visibility. Below this is a list of all vessels sorted by proximity to the current
vessel. Beside each vessel is a button to target that vessel or swith to it.

## Focus Select
First you have a button that will take you back to the active vessel. Next is any body on your current trajectory, this will include your
current SOI and any body you will encounter on your projected course. For instance if you are on your way from Kerbin to Duna you will see
Kerbin, the Sun, and Duna. If you have any planned maneuvers you will see the maneuver nodes and any bodies on your planned trajectory
that are not also on your current trajectory. If your current target is a celestial body you will see it as the next option. Below this you
have the option to show an exhaustive list of all celestial bodies in a heirarchy. You have the option to put "Focus Select" in target
mode which will make the celestial body the target instead of changing focus. Click on any button to change your focus to the selected
body or maneuver node.

## Maneuver Editor
You have set of buttons to create a new maneuver, delete the current maneuver, delete all meneuvers, store the current maneuvers, or close
the window. Below this is a set of basic "Warp To" controls. These are not as complete as the "Warp To" utility, but they are convenient.
If you don't have any maneuvers this is all you will see. If there is at least one maneuver you will see buttons to select which maneuver.
The time to the selected maneuver and the delta-v of the current maneuver are shown. Below this is the delta-v granularity. Select one and
then click on + to add it to Prograde, Normal, or Radial or - to remove it. 0 will reset that value back to zero. Below this is the time
controls which work just like the "Warp To" utility. You also have a +/- orbit buttons which add or subtract the orbital period from the
time.

If you have stored a set of maneuvers you will see the "Saved Maneuver" window. It will show a list of stored maneuvers with the combined
delta-v of the set of maneuvers and the time to the first maneuver in the set. You have the option to clear all stored maneuvers, minimize
the window (make it smaller so it takes up less space). Each saved maneuver set can be deleted individually or restored. Restoring will
delete all current maneuvers and re-establish the saved maneuvers.