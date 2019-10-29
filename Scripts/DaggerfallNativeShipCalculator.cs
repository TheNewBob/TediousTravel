// Project:         Tedious Travel mod for daggerfall unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: Lypyl (lypyl@dfworkshop.net), Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Adapted to TediousTravel needs by Jedidia
// 
// Notes: Use this line in command window to diff with original travel map: Tools.DiffFiles Assets\Scripts\Game\UserInterfaceWindows\DaggerfallTravelMapWindow.cs Assets\Game\Mods\TediousTravel\Scripts\TediousTravelMap.cs
//


using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;

namespace TediousTravel
{
    internal class DaggerfallNativeShipCalculator : ShipTravelCalculator
    {
        private bool useInns;

        public DaggerfallNativeShipCalculator(TediousTravel controller)
        {
            useInns = controller.UseInnsWhenTravelingByShip;
        }

        public TravelInfo CalculateTravelInfo(ContentReader.MapSummary locationSummary, DFPosition destination)
        {
            // travel time calculator (unfortunately) has side-effects. You must calculate the
            // travel time and then calculate the costs using the same instance.
            TravelTimeCalculator travelTimeCalculator = new TravelTimeCalculator();
            var travelTimeMinutes = travelTimeCalculator.CalculateTravelTime(destination,
               false, useInns, true, PlayerOwnsHorse(), PlayerHasCart());

            bool playerOwnsShip = DaggerfallWorkshop.Game.Banking.DaggerfallBankManager.OwnsShip;
            travelTimeCalculator.CalculateTripCost(travelTimeMinutes, useInns, playerOwnsShip, true);
            var tripCost = travelTimeCalculator.TotalCost;
            return new TravelInfo(travelTimeMinutes,tripCost);
        }

        private bool PlayerOwnsHorse()
        {
            DaggerfallWorkshop.Game.Items.ItemCollection inventory = GameManager.Instance.PlayerEntity.Items;
            return inventory.Contains(DaggerfallWorkshop.Game.Items.ItemGroups.Transportation, (int)DaggerfallWorkshop.Game.Items.Transportation.Horse);
        }

        private bool PlayerHasCart()
        {
            DaggerfallWorkshop.Game.Items.ItemCollection inventory = GameManager.Instance.PlayerEntity.Items;
            return inventory.Contains(DaggerfallWorkshop.Game.Items.ItemGroups.Transportation, (int)DaggerfallWorkshop.Game.Items.Transportation.Small_cart);
        }


    }
}