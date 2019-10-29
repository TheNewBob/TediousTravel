// Project:         Tedious Travel mod for daggerfall unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: Lypyl (lypyl@dfworkshop.net), Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Adapted to TediousTravel needs by Jedidia
// 
// Notes: Use this line in command window to diff with original travel map: Tools.DiffFiles Assets\Scripts\Game\UserInterfaceWindows\DaggerfallTravelMapWindow.cs Assets\Game\Mods\TediousTravel\Scripts\TediousTravelMap.cs
//


using DaggerfallConnect;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using System;

namespace TediousTravel
{
    internal class TediousTravelShipCalculator : ShipTravelCalculator
    {
        TravelTimeCalculator travelTimeCalculator = new TravelTimeCalculator();

        public TravelInfo CalculateTravelInfo(ContentReader.MapSummary locationSummary, DFPosition destination)
        {
            bool playerOwnsShip = DaggerfallWorkshop.Game.Banking.DaggerfallBankManager.OwnsShip;
            var travelTime = travelTimeCalculator.CalculateTravelTime(destination, false, false, true, false, false);

            if (!playerOwnsShip)
            {
                travelTime = (int)Math.Round((float)travelTime * GetLocationTraveltimeModifier(
                    GameManager.Instance.PlayerGPS.CurrentLocationType,
                    locationSummary.LocationType));
            }

            var days = (int)Math.Ceiling((float)travelTime / 1440);

            var tripCost = 0;
            if (!playerOwnsShip && !GameManager.Instance.GuildManager.FreeShipTravel())
                tripCost = days * 15;

            return new TravelInfo(travelTime, tripCost);
        }

        /// <summary>
        /// Returns a modifier on travel time based on the quality of starting or arrival port.
        /// The reasoning here is that traveling between two major ports will be fast, because a lot of ships
        /// are sailing that route directly. Meanwhile, to sail from or to some forgotten fishing hamlet
        /// will necessarily include waiting for ships to leave and jumping ship once or twice because nobody
        /// is serving the route directly or regularly.
        /// </summary>
        /// <param name="startLocation"></param>
        /// <param name="endLocation"></param>
        /// <returns></returns>
        float GetLocationTraveltimeModifier(DFRegion.LocationTypes startLocation, DFRegion.LocationTypes endLocation)
        {
            var modifier = 1f;

            if (startLocation == DFRegion.LocationTypes.Tavern ||
                endLocation == DFRegion.LocationTypes.Tavern)
                modifier += 0.1f;

            if (startLocation == DFRegion.LocationTypes.TownVillage ||
                endLocation == DFRegion.LocationTypes.TownVillage ||
                startLocation == DFRegion.LocationTypes.ReligionTemple ||
                endLocation == DFRegion.LocationTypes.ReligionTemple)
                modifier += 0.2f;

            if (startLocation == DFRegion.LocationTypes.TownHamlet ||
                endLocation == DFRegion.LocationTypes.TownHamlet ||
                startLocation == DFRegion.LocationTypes.HomeWealthy ||
                endLocation == DFRegion.LocationTypes.HomeWealthy)
                modifier += 0.3f;

            return modifier;
        }


    }
}