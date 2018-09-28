using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace TediousTravel
{
    public class PlayerAutoPilot
    {
        private ContentReader.MapSummary destinationSummary;
        private DFPosition destinationMapPixel = null;
        private Rect destinationWorldRect;
        private PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
        private DFPosition lastPlayerMapPixel = new DFPosition(int.MaxValue, int.MaxValue);
        // used to allow mouselock in the next frame, without which yaw changes will not be updated.
        private bool allowSetYaw = false;
        private bool inDestinationMapPixel = false;
        private InputManager inputManager = InputManager.Instance;


        // some reflection-fu to get access to a private function. Don't judge me, if there was another way I'd use it.
        private MethodInfo applyHorizontalForce = InputManager.Instance.GetType().GetMethod("ApplyVerticalForce",
        BindingFlags.NonPublic | BindingFlags.Instance);

        public PlayerAutoPilot(ContentReader.MapSummary destinationSummary)
        {
            this.destinationSummary = destinationSummary;
            Init();
        }

        private void Init()
        {
            destinationMapPixel = MapsFile.GetPixelFromPixelID(destinationSummary.ID);
            Debug.Log("destination map pixel: " + destinationMapPixel.X + ", " + destinationMapPixel.Y);

            // get exact coordinates of destination
            destinationWorldRect = GetLocationRect(destinationSummary);
            //grow the rect a bit so fast travel cancels shortly before entering the location
            destinationWorldRect.xMin -= 1000;
            destinationWorldRect.xMax += 1000;
            destinationWorldRect.yMin -= 1000;
            destinationWorldRect.yMax += 1000;

            Debug.Log("destination rect: left:"
                + (int)destinationWorldRect.xMin + " right: " + (int)destinationWorldRect.xMax
                + " bottom: " + (int)destinationWorldRect.yMin + " top: " + (int)destinationWorldRect.yMax);
        }

        public void Update()
        {
            if (inDestinationMapPixel)
            {
                if (isPlayerInArrivalRect())
                {
                    // note that event will be raised whenever player is inside destination rect when update is called.
                    RaiseOnArrivalEvent();
                    return;
                }
            }

            // get the players current map position and reorient if it changed. Just  to make sure we're staying on track.
            var playerPos = GameManager.Instance.PlayerGPS.CurrentMapPixel;
            if (playerPos.X != lastPlayerMapPixel.X || playerPos.Y != lastPlayerMapPixel.Y)
            {
                Debug.Log("Player entered new map pixel, reorienting");
                lastPlayerMapPixel = playerPos;
                OrientPlayer();

                inDestinationMapPixel = lastPlayerMapPixel.X == destinationMapPixel.X && lastPlayerMapPixel.Y == destinationMapPixel.Y;
            }

            // disable player mouselook, except if new yaw was set.
            if (allowSetYaw)
            {
                GameManager.Instance.PlayerMouseLook.enableMouseLook = true;
                GameManager.Instance.PlayerMouseLook.lockCursor = true;
                allowSetYaw = false;
            }
            else
            {
                GameManager.Instance.PlayerMouseLook.simpleCursorLock = true;
                GameManager.Instance.PlayerMouseLook.enableMouseLook = false;
            }
            // make the player move forward
            applyHorizontalForce.Invoke(inputManager, new object[] { 1 });

        }

        /// <summary>
        /// Checks if player is in arrival rect.
        /// Does not use the playerGPS function because the arrival rect is a bit bigger than the location rect,
        /// so the player stops a bit outside it.
        /// </summary>
        /// <returns></returns>
        private bool isPlayerInArrivalRect()
        {
            return (destinationWorldRect.Contains(new Vector2(playerGPS.WorldX, playerGPS.WorldZ)));
        }

 

        private void OrientPlayer()
        {
            var playerPos = new DFPosition(playerGPS.WorldX, playerGPS.WorldZ);
            var yaw = CalculateYaw(playerPos,
                new DFPosition(
                    (int)destinationWorldRect.center.x,
                    (int)destinationWorldRect.center.y));

            GameManager.Instance.PlayerMouseLook.Yaw = yaw;
            GameManager.Instance.PlayerMouseLook.Pitch = 0f;
            allowSetYaw = true;
        }


        private float CalculateYaw(DFPosition fromWorldPos, DFPosition toWorldPos)
        {
            double angleRad = Math.Atan2(fromWorldPos.X - toWorldPos.X, fromWorldPos.Y - toWorldPos.Y);
            double angleDeg = angleRad * 180.0 / Math.PI + 180;
            Debug.Log((float)angleDeg);
            return (float)angleDeg;
        }


        public static Rect GetLocationRect(ContentReader.MapSummary mapSummary)
        {
            DFLocation targetLocation;
            if (!DaggerfallUnity.Instance.ContentReader.GetLocation(
                    mapSummary.RegionIndex, mapSummary.MapIndex, out targetLocation))
                throw new ArgumentException("TediousTravel destination not found!");
            return GetLocationRect(targetLocation);
        }

        // TODO: Will be a member of DaggerfallLocation in a future build, remove when released.
        /// <summary>
        /// Helper to get location rect in world coordinates.
        /// </summary>
        /// <param name="location">Target location.</param>
        /// <returns>Location rect in world space. xMin,yMin is SW corner. xMax,yMax is NE corner.</returns>
        public static Rect GetLocationRect(DFLocation location)
        {
            // This finds the absolute SW origin of map pixel in world coords
            DFPosition mapPixel = MapsFile.LongitudeLatitudeToMapPixel(location.MapTableData.Longitude, location.MapTableData.Latitude);
            DFPosition worldOrigin = MapsFile.MapPixelToWorldCoord(mapPixel.X, mapPixel.Y);

            // Find tile offset point using same logic as terrain helper
            DFPosition tileOrigin = TerrainHelper.GetLocationTerrainTileOrigin(location);

            // Adjust world origin by tileorigin*2 in world units
            worldOrigin.X += (tileOrigin.X * 2) * MapsFile.WorldMapTileDim;
            worldOrigin.Y += (tileOrigin.Y * 2) * MapsFile.WorldMapTileDim;

            // Get width and height of location in world units
            int width = location.Exterior.ExteriorData.Width * MapsFile.WorldMapRMBDim;
            int height = location.Exterior.ExteriorData.Height * MapsFile.WorldMapRMBDim;

            // Create location rect in world coordinates
            Rect locationRect = new Rect()
            {
                xMin = worldOrigin.X,
                xMax = worldOrigin.X + width,
                yMin = worldOrigin.Y,
                yMax = worldOrigin.Y + height,
            };

            return locationRect;
        }


        // events
        public delegate void OnArrivalHandler();
        public event OnArrivalHandler OnArrival;
        void RaiseOnArrivalEvent()
        {
            if (OnArrival != null)
                OnArrival();
        }
    }

}
