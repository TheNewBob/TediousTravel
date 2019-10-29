// Project:         Tedious Travel mod for daggerfall unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)

using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Utility;

namespace TediousTravel
{
    public struct TravelInfo
    {
        public int travelTimeMinutes;
        public int totalCost;

        public TravelInfo(int travelTimeMinutes, int totalCost)
        {
            this.travelTimeMinutes = travelTimeMinutes;
            this.totalCost = totalCost;
        }
    }

    public interface ShipTravelCalculator
    {
        TravelInfo CalculateTravelInfo(ContentReader.MapSummary locationSummary, DFPosition destination);
    }
}
