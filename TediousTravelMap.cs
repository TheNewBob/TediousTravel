// Project:         Tedious Travel mod for daggerfall unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Original Author: Lypyl (lypyl@dfworkshop.net), Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    Adapted to TediousTravel needs by Jedidia
// 
// Notes:
//


using UnityEngine;
using System;
using System.Linq;
using System.IO;
using DaggerfallConnect;
using DaggerfallConnect.Utility;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using System.Collections.Generic;
using Wenzil.Console;
using Wenzil.Console.Commands;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Items;

namespace TediousTravel
{
    /// <summary>
    /// Implements Daggerfall's travel map.
    /// </summary>
    public class TediousTravelMap : DaggerfallPopupWindow
    {
        private class ShipTravelData
        {
            public int minutes;
            public DFPosition destination;
            public int cost;

            public ShipTravelData(int minutes, DFPosition destination, int cost)
            {
                this.minutes = minutes;
                this.destination = destination;
                this.cost = cost;
            }
        }

        #region Fields

        FilterMode filterMode = FilterMode.Point;

        const int betonyIndex = 19;

        const string nativeImgName = "TRAV0I00.IMG";
        const string regionPickerImgName = "TRAV0I01.IMG";
        const string findAtButtonImgName = "TRAV0I03.IMG";
        const string locationFilterButtonEnabledImgName = "TRAV01I0.IMG";
        const string locationFilterButtonDisabledImgName = "TRAV01I1.IMG";
        const string downArrowImgName = "TRAVAI05.IMG";
        const string upArrowImgName = "TRAVBI05.IMG";
        const string rightArrowImgName = "TRAVCI05.IMG";
        const string leftArrowImgName = "TRAVDI05.IMG";
        const string regionBorderImgName = "MBRD00I0.IMG";
        const string colorPaletteColName = "FMAP_PAL.COL";
        const int regionPanelOffset = 12;
        const int identifyFlashCount = 4;
        const float identifyFlashInterval = 0.5f;

        bool portsFilter = false;
        ShipTravelData shipTravelDestination = null;
        TediousData tediousData = TediousData.Instance;

        Dictionary<string, Vector2> offsetLookup = new Dictionary<string, Vector2>();
        string[] selectedRegionMapNames;    //different maps for selected region

        string gotoLocation = null;
        int gotoRegion;

        DFBitmap regionPickerBitmap;
        ImgFile loadedImg;
        DFRegion currentDFRegion;
        ContentReader.MapSummary locationSummary;

        KeyCode toggleClosedBinding;

        Panel borderPanel;
        Panel regionTextureOverlayPanel;

        TextLabel regionLabel;

        Texture2D findButtonTexture;
        Texture2D atButtonTexture;
        Texture2D dungeonFilterButtonEnabled;
        Texture2D dungeonFilterButtonDisabled;
        Texture2D templesFilterButtonEnabled;
        Texture2D templesFilterButtonDisabled;
        Texture2D homesFilterButtonEnabled;
        Texture2D homesFilterButtonDisabled;
        Texture2D townsFilterButtonEnabled;
        Texture2D townsFilterButtonDisabled;
        Texture2D upArrowTexture;
        Texture2D downArrowTexture;
        Texture2D leftArrowTexture;
        Texture2D rightArrowTexture;
        Texture2D borderTexture;

        Button findButton;
        Button atButton;
        Button exitButton;
        Button horizontalArrowButton = new Button();
        Button verticalArrowButton = new Button();
        Button portButton = new Button();
        Button dungeonsFilterButton = new Button();
        Button templesFilterButton = new Button();
        Button homesFilterButton = new Button();
        Button townsFilterButton = new Button();

        Rect regionTextureOverlayPanelRect = new Rect(0, regionPanelOffset, 320, 160);
        Rect dungeonsFilterButtonSrcRect = new Rect(0, 0, 99, 11);
        Rect templesFilterButtonSrcRect = new Rect(0, 11, 99, 11);
        Rect homesFilterButtonSrcRect = new Rect(99, 0, 80, 11);
        Rect townsFilterButtonSrcRect = new Rect(99, 11, 80, 11);
        Rect findButtonRect = new Rect(0, 0, 45, 11);
        Rect atButtonRect = new Rect(0, 11, 45, 11);

        Color32[] pixelBuffer;
        Color32[] locationPixelColors;                      //pixel colors for different location types
        Color identifyFlashColor;

        int zoomfactor = 2;
        int width = 0;
        int height = 0;
        int mouseOverRegion = -1;
        int selectedRegion = -1;
        int mapIndex = 0;    //current index of loaded map from selectedRegionMapNames
        float scale = 1.0f;
        float identifyLastChangeTime = 0;
        float identifyChanges = 0;

        bool identifyState = false;
        bool identifying = false;
        bool locationSelected = false;
        bool findingLocation = false;
        bool zoom = false;    //toggles zoom mode
        bool draw = true;     //draws textures to panel
        bool loadNewImage = true;     //loads current map image
        bool isShowing = false;

        static bool revealUndiscoveredLocations; // flag used to indicate cheat/debugging mode for revealing undiscovered locations

        static bool filterDungeons = false;
        static bool filterTemples = false;
        static bool filterHomes = false;
        static bool filterTowns = false;

        Vector2 lastMousePos = Vector2.zero;
        Vector2 zoomOffset = Vector2.zero;
        Vector2 zoomPosition = Vector2.zero;

        TediousTravel controller = null;

        //TextLabel coordsLabel = new TextLabel();

        #endregion

        #region Properties

        string RegionImgName { get; set; }

        public bool IsShowing { get; set; }

        bool HasMultipleMaps
        {
            get { return (selectedRegionMapNames.Length > 1) ? true : false; }
        }

        bool HasVerticalMaps
        {
            get { return (selectedRegionMapNames.Length > 2) ? true : false; }
        }

        bool RegionSelected
        {
            get { return selectedRegion != -1; }
        }

        bool MouseOverRegion
        {
            get { return mouseOverRegion != -1; }
        }

        bool MouseOverOtherRegion
        {
            get { return RegionSelected && (selectedRegion != mouseOverRegion); }
        }

        bool FindingLocation
        {
            get { return identifying && findingLocation && RegionSelected; }
        }

        public void GotoLocation(string placeName, int region)
        {
            gotoLocation = placeName;
            gotoRegion = region;
        }

        #endregion


        #region Constructors

        public TediousTravelMap(IUserInterfaceManager uiManager, TediousTravel controller)
            : base(uiManager)
        {
            this.controller = controller;
            // register console commands
            try
            {
                TravelMapConsoleCommands.RegisterCommands();
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Error Registering Travelmap Console commands: {0}", ex.Message));

            }
        }

        #endregion

        #region User Interface

        protected override void Setup()
        {
            ParentPanel.BackgroundColor = Color.black;

            //NativePanel.Components.Add(coordsLabel);

            // Set location pixel colors and identify flash color from palette file
            DFPalette colors = new DFPalette();
            if (!colors.Load(Path.Combine(DaggerfallUnity.Instance.Arena2Path, colorPaletteColName)))
                throw new Exception("DaggerfallTravelMap: Could not load color palette.");

            locationPixelColors = new Color32[]
            {
                new Color32(colors.GetRed(237), colors.GetGreen(237), colors.GetBlue(237), 255),  //dunglab (R215, G119, B39)
                new Color32(colors.GetRed(240), colors.GetGreen(240), colors.GetBlue(240), 255),  //dungkeep (R191, G87, B27)
                new Color32(colors.GetRed(243), colors.GetGreen(243), colors.GetBlue(243), 255),  //dungruin (R171, G51, B15)
                new Color32(colors.GetRed(246), colors.GetGreen(246), colors.GetBlue(246), 255),  //graveyards (R147, G15, B7)
                new Color32(colors.GetRed(0), colors.GetGreen(0), colors.GetBlue(0), 255),        //coven (R15, G15, B15)
                new Color32(colors.GetRed(37), colors.GetGreen(37), colors.GetBlue(37), 255),     //farms (R165, G100, B70)
                new Color32(colors.GetRed(35), colors.GetGreen(35), colors.GetBlue(35), 255),     //wealthy (R193, G133, B100)
                new Color32(colors.GetRed(39), colors.GetGreen(39), colors.GetBlue(39), 255),     //poor (R140, G86, B55)
                new Color32(colors.GetRed(96), colors.GetGreen(96), colors.GetBlue(96), 255),     //temple (R176, G205, B255)
                new Color32(colors.GetRed(101), colors.GetGreen(101), colors.GetBlue(101), 255),  //cult (R68, G124, B192)
                new Color32(colors.GetRed(55), colors.GetGreen(55), colors.GetBlue(55), 255),     //tavern (R126, G81, B89)
                new Color32(colors.GetRed(49), colors.GetGreen(49), colors.GetBlue(49), 255),     //city (R220, G177, B177)
                new Color32(colors.GetRed(51), colors.GetGreen(51), colors.GetBlue(51), 255),     //hamlet (R188, G138, B138)
                new Color32(colors.GetRed(53), colors.GetGreen(53), colors.GetBlue(53), 255),     //village (R155, G105, B106)
            };

            identifyFlashColor = new Color32(colors.GetRed(244), colors.GetGreen(244), colors.GetBlue(244), 255); // (R163, G39, B15)

            // Populate the offset dict
            PopulateRegionOffsetDict();

            // Load picker colours
            regionPickerBitmap = DaggerfallUI.GetImgBitmap(regionPickerImgName);

            // Add region label
            regionLabel = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, new Vector2(0, 2), string.Empty, NativePanel);
            regionLabel.HorizontalAlignment = HorizontalAlignment.Center;

            // Handle clicks
            NativePanel.OnMouseClick += ClickHandler;

            // Setup buttons for first time
            LoadButtonTextures();
            SetupButtons();
            UpdateSearchButtons();

            // Region overlay panel
            regionTextureOverlayPanel = DaggerfallUI.AddPanel(regionTextureOverlayPanelRect, NativePanel);
            regionTextureOverlayPanel.Enabled = false;

            //borders around the region maps
            borderTexture = DaggerfallUI.GetTextureFromImg(regionBorderImgName);
            borderPanel = DaggerfallUI.AddPanel(new Rect(new Vector2(0, regionTextureOverlayPanelRect.position.y), regionTextureOverlayPanelRect.size), NativePanel);
            borderPanel.BackgroundTexture = borderTexture;
            borderPanel.Enabled = false;

            selectedRegionMapNames = GetRegionMapNames(GetPlayerRegion());
            loadNewImage = true;
            draw = true;
            StartIdentify();
        }

        public override void OnPush()
        {
            base.OnPush();
            isShowing = true;

            if (base.IsSetup)
            {
                SetPlayerRegionOverlay();
                CloseRegionPanel();
            }
        }

        public override void OnPop()
        {
            isShowing = false;
            findingLocation = false;
            gotoLocation = null;
            base.OnPop();
        }

        public override void Update()
        {
            base.Update();

            if (shipTravelDestination != null)
            {
                DaggerfallUI.Instance.FadeBehaviour.SmashHUDToBlack();
                performShipTravel(shipTravelDestination);
                shipTravelDestination = null;
                return;
            }

            // Toggle window closed with same hotkey used to open it
            if (Input.GetKeyUp(toggleClosedBinding))
            {
                if (RegionSelected)
                    CloseRegionPanel();
                else
                    CloseWindow();
            }

            //input handling

            Vector2 currentMousePos = new Vector2((NativePanel.ScaledMousePosition.x), (NativePanel.ScaledMousePosition.y));

            if (currentMousePos != lastMousePos)
            {
                lastMousePos = currentMousePos;
                if (RegionSelected == true)
                    UpdateMouseOverLocation();
                else
                    UpdateMouseOverRegion();
            }

            UpdateRegionLabel();


            if (RegionSelected)
            {
                if (Input.GetKeyUp(KeyCode.Mouse1))
                {
                    zoomPosition = currentMousePos;
                    zoom = !zoom;
                    borderPanel.Enabled = !borderPanel.Enabled;
                    draw = true;
                }
                else if (Input.GetKey(KeyCode.LeftShift) && zoom)   //scrolling while zoomed in
                {
                    zoomPosition = currentMousePos;
                    draw = true;
                }
                if (Input.GetKeyDown(KeyCode.L))
                    ShowLocationPicker();
                else if (Input.GetKeyDown(KeyCode.F))
                    FindlocationButtonClickHandler(null, Vector2.zero);
            }
            else
            {

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (identifying)
                        OpenRegionPanel(GetPlayerRegion());
                }

            }

            if (loadNewImage) //loads image file if true
            {

                if (RegionSelected)
                    LoadMapImage(selectedRegionMapNames[mapIndex]);
                else
                    LoadMapImage(nativeImgName);

                loadNewImage = false;
            }


            if (draw)    //updates textures if true
            {
                SetColorsFromImg();

                if (RegionSelected)
                {
                    SetLocationPixels();

                    //draw x-hair over player loc or find result location
                    if (identifyState)
                    {
                        if (FindingLocation)
                            CreateCrossHair(MapsFile.GetPixelFromPixelID(locationSummary.ID), locationSummary.RegionIndex);
                        else
                            CreateCrossHair(GetPlayerMapPosition(), selectedRegion);
                    }

                    Draw(regionTextureOverlayPanel);
                }
                else
                {
                    if (identifyState)
                        SetPlayerRegionOverlay();

                    Draw(NativePanel);
                }

                draw = false;

            }

            //identifiying
            AnimateIdentify();

            // If a goto location specified, find it and ask if player wants to travel.
            if (!string.IsNullOrEmpty(gotoLocation))
            {
                OpenRegionPanel(gotoRegion);
                HandleLocationFindEvent(null, gotoLocation);
                gotoLocation = null;
            }


        }

        #endregion

        #region Setup

        // Initial button setup
        void SetupButtons()
        {
            // Exit button
            exitButton = DaggerfallUI.AddButton(new Rect(278, 175, 39, 22), NativePanel);
            exitButton.OnMouseClick += ExitButtonClickHandler;

            // Find button
            findButton = DaggerfallUI.AddButton(new Rect(3, 175, findButtonTexture.width, findButtonTexture.height), NativePanel);
            findButton.BackgroundTexture = findButtonTexture;
            findButton.OnMouseClick += FindlocationButtonClickHandler;

            // I'm At button
            atButton = DaggerfallUI.AddButton(new Rect(3, 186, atButtonTexture.width, atButtonTexture.height), NativePanel);
            atButton.BackgroundTexture = atButtonTexture;
            atButton.OnMouseClick += AtButtonClickHandler;

            // Dungeons filter button
            //dungeonsFilterButton.Position = new Vector2(50, 175);
            //dungeonsFilterButton.Size = new Vector2(dungeonsFilterButtonSrcRect.width, dungeonsFilterButtonSrcRect.height);
            dungeonsFilterButton = DaggerfallUI.AddButton(
                new Rect(50, 175, dungeonsFilterButtonSrcRect.width, dungeonsFilterButtonSrcRect.height), NativePanel);
            dungeonsFilterButton.Name = "dungeonsFilterButton";
            dungeonsFilterButton.OnMouseClick += FilterButtonClickHandler;

            // Temples filter button
            templesFilterButton = DaggerfallUI.AddButton(
                new Rect(50, 186, templesFilterButtonSrcRect.width, templesFilterButtonSrcRect.height), NativePanel);    
            templesFilterButton.Name = "templesFilterButton";
            templesFilterButton.OnMouseClick += FilterButtonClickHandler;

            // Homes filter button
            homesFilterButton = DaggerfallUI.AddButton(
                new Rect(149, 175, homesFilterButtonSrcRect.width, homesFilterButtonSrcRect.height), NativePanel);
            homesFilterButton.Name = "homesFilterButton";
            homesFilterButton.OnMouseClick += FilterButtonClickHandler;

            // Towns filter button
            townsFilterButton = DaggerfallUI.AddButton(
                new Rect(149, 186, townsFilterButtonSrcRect.width, townsFilterButtonSrcRect.height), NativePanel);
            townsFilterButton.Name = "townsFilterButton";
            townsFilterButton.OnMouseClick += FilterButtonClickHandler;

            // Horizontal arrow button
            horizontalArrowButton = DaggerfallUI.AddButton(
                new Rect(231, 176, leftArrowTexture.width, leftArrowTexture.height), NativePanel);
            horizontalArrowButton.Enabled = false;
            horizontalArrowButton.Name = "horizontalArrowButton";
            horizontalArrowButton.OnMouseClick += ArrowButtonClickHandler;

            // Vertical arrow button
            verticalArrowButton = DaggerfallUI.AddButton(
                new Rect(254, 176, upArrowTexture.width, upArrowTexture.height), NativePanel);
            verticalArrowButton.Enabled = false;
            verticalArrowButton.Name = "verticalArrowButton";
            verticalArrowButton.OnMouseClick += ArrowButtonClickHandler;

            // Ports filter
            portButton = DaggerfallUI.AddButton(
                new Rect(231, 175, 45, 22), NativePanel);
            portButton.BackgroundColor = new Color(0.0f, 0.5f, 0.0f, 0.4f);
            portButton.Label.Text = "Ports";
            portButton.OnMouseClick += PortButtonClickHandler;
            portButton.Enabled = true;

            // Store toggle closed binding for this window
            toggleClosedBinding = InputManager.Instance.GetBinding(InputManager.Actions.TravelMap);

        }

        void SetupArrowButtons()
        {
            // Vertical arrow
            if (selectedRegionMapNames.Length > 2)
            {
                verticalArrowButton.Enabled = true;
                verticalArrowButton.BackgroundTexture = (mapIndex > 1) ? upArrowTexture : downArrowTexture;
            }
            else
                verticalArrowButton.Enabled = false;

            // Horizontal arrow
            if (selectedRegionMapNames.Length > 1)
            {
                horizontalArrowButton.Enabled = true;
                horizontalArrowButton.BackgroundTexture = (mapIndex % 2 == 0) ? rightArrowTexture : leftArrowTexture;
            }
            else
                horizontalArrowButton.Enabled = false;
        }

        // Loads textures for buttons
        void LoadButtonTextures()
        {
            Texture2D baselocationFilterButtonEnabledText = ImageReader.GetTexture(locationFilterButtonEnabledImgName);
            Texture2D baselocationFilterButtonDisabledText = ImageReader.GetTexture(locationFilterButtonDisabledImgName);
            DFSize baseSize = new DFSize(179, 22);

            // Dungeons toggle button
            dungeonFilterButtonEnabled = ImageReader.GetSubTexture(baselocationFilterButtonEnabledText, dungeonsFilterButtonSrcRect, baseSize);
            dungeonFilterButtonDisabled = ImageReader.GetSubTexture(baselocationFilterButtonDisabledText, dungeonsFilterButtonSrcRect, baseSize);

            // Dungeons toggle button
            templesFilterButtonEnabled = ImageReader.GetSubTexture(baselocationFilterButtonEnabledText, templesFilterButtonSrcRect, baseSize);
            templesFilterButtonDisabled = ImageReader.GetSubTexture(baselocationFilterButtonDisabledText, templesFilterButtonSrcRect, baseSize);

            // Homes toggle button
            homesFilterButtonEnabled = ImageReader.GetSubTexture(baselocationFilterButtonEnabledText, homesFilterButtonSrcRect, baseSize);
            homesFilterButtonDisabled = ImageReader.GetSubTexture(baselocationFilterButtonDisabledText, homesFilterButtonSrcRect, baseSize);

            // Towns toggle button
            townsFilterButtonEnabled = ImageReader.GetSubTexture(baselocationFilterButtonEnabledText, townsFilterButtonSrcRect, baseSize);
            townsFilterButtonDisabled = ImageReader.GetSubTexture(baselocationFilterButtonDisabledText, townsFilterButtonSrcRect, baseSize);

            DFSize buttonsFullSize = new DFSize(45, 22);

            findButtonTexture = ImageReader.GetTexture(findAtButtonImgName);
            findButtonTexture = ImageReader.GetSubTexture(findButtonTexture, findButtonRect, buttonsFullSize);

            atButtonTexture = ImageReader.GetTexture(findAtButtonImgName);
            atButtonTexture = ImageReader.GetSubTexture(atButtonTexture, atButtonRect, buttonsFullSize);


            // Arrows
            upArrowTexture = ImageReader.GetTexture(upArrowImgName);//DaggerfallUI.GetTextureFromImg(upArrowImgName);
            downArrowTexture = ImageReader.GetTexture(downArrowImgName);//DaggerfallUI.GetTextureFromImg(downArrowImgName);
            leftArrowTexture = ImageReader.GetTexture(leftArrowImgName);//DaggerfallUI.GetTextureFromImg(leftArrowImgName);
            rightArrowTexture = ImageReader.GetTexture(rightArrowImgName);//DaggerfallUI.GetTextureFromImg(rightArrowImgName);

            UnityEngine.Object.Destroy(baselocationFilterButtonEnabledText);
            UnityEngine.Object.Destroy(baselocationFilterButtonDisabledText);
        }

        //loads img file
        void LoadMapImage(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("LoadMapImage found name null or empty - defaulting to region selection map");
                name = nativeImgName;
            }
            loadedImg = new ImgFile(Path.Combine(DaggerfallUnity.Instance.Arena2Path, name), FileUsage.UseMemory, true);
            loadedImg.LoadPalette(Path.Combine(DaggerfallUnity.Instance.Arena2Path, loadedImg.PaletteName));
            width = loadedImg.GetDFBitmap().Width;
            height = loadedImg.GetDFBitmap().Height;
        }

        //fills buffer w/ colors from current loaded img
        void SetColorsFromImg()
        {
            if (loadedImg == null)
            {
                Debug.LogWarning("SetColors() imgFile was null");
                return;
            }
            if (pixelBuffer == null)
            {
                DFBitmap bitmap = loadedImg.GetDFBitmap();
                pixelBuffer = new Color32[bitmap.Width * bitmap.Height];
            }

            loadedImg.LoadPalette(Path.Combine(DaggerfallUnity.Instance.Arena2Path, loadedImg.PaletteName));
            pixelBuffer = loadedImg.GetColor32(loadedImg.GetDFBitmap(), 0);
        }

        void CreateCrossHair(DFPosition pos, int regionIndex = -1)
        {
            if (regionIndex == -1)
                regionIndex = GetPlayerRegion();
            CreateCrossHair(pos.X, pos.Y, regionIndex);
        }

        /// <summary>
        /// sets up crosshair texture & panel used to by "at" and "find buttons
        /// </summary>
        /// <param name="mapPixelX"></param>
        /// <param name="mapPixelY"></param>
        void CreateCrossHair(int mapPixelX, int mapPixelY, int regionIndex)
        {
            if (RegionSelected == false)
                return;
            try
            {
                string mapName = selectedRegionMapNames[mapIndex];
                Vector2 origin = offsetLookup[mapName];
                float scale = GetRegionMapScale(regionIndex);

                // Manually adjust Betony vertical offset
                int yAdjust = 0;
                if (regionIndex == betonyIndex)
                    yAdjust = -477;

                int scaledX = (int)((mapPixelX - origin.x) * scale);
                int scaledY = (int)((mapPixelY - origin.y) * scale) + regionPanelOffset + yAdjust;

                if (pixelBuffer == null)
                {
                    Debug.LogWarning("CreateCrosshair() found pixelBuffer null");
                    return;
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (x == scaledX || y + regionPanelOffset == scaledY)
                        {
                            pixelBuffer[(height - y - 1) * width + x] = identifyFlashColor;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        // Creates the region overlay for current player region
        void SetPlayerRegionOverlay()
        {
            try
            {
                // Player must be inside a valid region
                int playerRegion = GetPlayerRegion();
                if (playerRegion == -1)
                    return;

                if (regionPickerBitmap == null)
                    regionPickerBitmap = DaggerfallUI.GetImgBitmap(regionPickerImgName);

                // Create a texture map overlay for the region area
                int width = regionPickerBitmap.Width;
                int height = regionPickerBitmap.Height;

                if (pixelBuffer == null)
                    pixelBuffer = new Color32[width * height];

                // note by Nystul: check necessary to prevent exception which could happen if pixelBuffer is 320x160 instead of 320x200 -
                // otherwise marked line below will throw exception (e.g. after fast travel to a location in Wrothgarian Mountains and reopening map)
                // not sure why this happens, but it happens, maybe Lypyl can take a look
                if (pixelBuffer.GetLength(0) != width * height)
                    return;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcOffset = y * width + x;
                        int dstOffset = ((height - y - 1) * width) + x;
                        int sampleRegion = regionPickerBitmap.Data[srcOffset] - 128;
                        if (sampleRegion == playerRegion)
                            pixelBuffer[dstOffset] = identifyFlashColor; // this is the line that might throw exception sometimes
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0}\n{1}", ex.TargetSite, ex.Message));
                return;
            }
        }

        void Draw(Panel target, Texture2D texture = null)
        {
            if (target == null)
                return;

            if (texture != null)
                UnityEngine.Object.Destroy(texture);

            if (pixelBuffer.Length != (width * height))
            {
                Debug.LogError(string.Format("DrawToPanel() - pixel buffer invalid size {0} {1} {2} {3}", loadedImg.FileName, RegionSelected, pixelBuffer.Length, (width * height)));
                SetColorsFromImg();
            }

            texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.SetPixels32(pixelBuffer);

            if (RegionSelected && zoom)
            {
                //center cropped porition over mouse
                int zoomWidth = width / (zoomfactor * 2);
                int zoomHeight = height / (zoomfactor * 2);
                int startX = (int)zoomPosition.x - zoomWidth;
                int startY = (int)(height + (-zoomPosition.y - zoomHeight)) + regionPanelOffset;

                if (startX < 0)
                {
                    startX = 0;
                }
                else if (startX + width / zoomfactor >= width)
                {
                    startX = width - width / zoomfactor;
                }
                if (startY < 0)
                {
                    startY = 0;
                }
                else if (startY + height / zoomfactor >= height)
                {
                    startY = height - height / zoomfactor;
                }

                zoomOffset = new Vector2(startX, startY);

                Color[] temp = texture.GetPixels(startX, startY, width / zoomfactor, height / zoomfactor);
                texture = new Texture2D(width / zoomfactor, height / zoomfactor, TextureFormat.ARGB32, false);
                texture.SetPixels(temp);
            }

            texture.filterMode = filterMode;
            texture.Apply();
            target.BackgroundTexture = texture;

        }

        // Populates offset dictionary for aligning top-left of map to map pixel coordinates.
        // Most maps have a 1:1 pixel ratio with map cells. A couple of maps have a larger scale.
        void PopulateRegionOffsetDict()
        {
            offsetLookup = new Dictionary<string, Vector2>();
            offsetLookup.Add("FMAPAI00.IMG", new Vector2(212, 340));
            offsetLookup.Add("FMAPBI00.IMG", new Vector2(322, 340));
            offsetLookup.Add("FMAPAI01.IMG", new Vector2(583, 279));
            offsetLookup.Add("FMAPBI01.IMG", new Vector2(680, 279));
            offsetLookup.Add("FMAPCI01.IMG", new Vector2(583, 340));
            offsetLookup.Add("FMAPDI01.IMG", new Vector2(680, 340));
            offsetLookup.Add("FMAP0I05.IMG", new Vector2(381, 4));
            offsetLookup.Add("FMAP0I09.IMG", new Vector2(525, 114));
            offsetLookup.Add("FMAP0I11.IMG", new Vector2(437, 340));
            offsetLookup.Add("FMAPAI16.IMG", new Vector2(578, 0));
            offsetLookup.Add("FMAPBI16.IMG", new Vector2(680, 0));
            offsetLookup.Add("FMAPCI16.IMG", new Vector2(578, 52));
            offsetLookup.Add("FMAPDI16.IMG", new Vector2(680, 52));
            offsetLookup.Add("FMAP0I17.IMG", new Vector2(39, 106));
            offsetLookup.Add("FMAP0I18.IMG", new Vector2(20, 29));
            offsetLookup.Add("FMAP0I19.IMG", new Vector2(80, 123));        // Betony scale different
            offsetLookup.Add("FMAP0I20.IMG", new Vector2(217, 293));
            offsetLookup.Add("FMAP0I21.IMG", new Vector2(263, 79));
            offsetLookup.Add("FMAP0I22.IMG", new Vector2(548, 219));
            offsetLookup.Add("FMAP0I23.IMG", new Vector2(680, 146));
            offsetLookup.Add("FMAP0I26.IMG", new Vector2(680, 80));
            offsetLookup.Add("FMAP0I32.IMG", new Vector2(41, 0));
            offsetLookup.Add("FMAP0I33.IMG", new Vector2(660, 101));
            offsetLookup.Add("FMAP0I34.IMG", new Vector2(578, 40));
            offsetLookup.Add("FMAP0I35.IMG", new Vector2(525, 3));
            offsetLookup.Add("FMAP0I36.IMG", new Vector2(440, 40));
            offsetLookup.Add("FMAP0I37.IMG", new Vector2(448, 0));
            offsetLookup.Add("FMAP0I38.IMG", new Vector2(366, 0));
            offsetLookup.Add("FMAP0I39.IMG", new Vector2(300, 8));
            offsetLookup.Add("FMAP0I40.IMG", new Vector2(202, 0));
            offsetLookup.Add("FMAP0I41.IMG", new Vector2(223, 6));
            offsetLookup.Add("FMAP0I42.IMG", new Vector2(148, 76));
            offsetLookup.Add("FMAP0I43.IMG", new Vector2(15, 340));
            offsetLookup.Add("FMAP0I44.IMG", new Vector2(61, 340));
            offsetLookup.Add("FMAP0I45.IMG", new Vector2(86, 338));
            offsetLookup.Add("FMAP0I46.IMG", new Vector2(132, 340));
            offsetLookup.Add("FMAP0I47.IMG", new Vector2(344, 309));
            offsetLookup.Add("FMAP0I48.IMG", new Vector2(381, 251));
            offsetLookup.Add("FMAP0I49.IMG", new Vector2(553, 255));
            offsetLookup.Add("FMAP0I50.IMG", new Vector2(661, 217));
            offsetLookup.Add("FMAP0I51.IMG", new Vector2(672, 275));
            offsetLookup.Add("FMAP0I52.IMG", new Vector2(680, 256));
            offsetLookup.Add("FMAP0I53.IMG", new Vector2(680, 340));
            offsetLookup.Add("FMAP0I54.IMG", new Vector2(491, 340));
            offsetLookup.Add("FMAP0I55.IMG", new Vector2(293, 340));
            offsetLookup.Add("FMAP0I56.IMG", new Vector2(263, 340));
            offsetLookup.Add("FMAP0I57.IMG", new Vector2(680, 157));
            offsetLookup.Add("FMAP0I58.IMG", new Vector2(17, 53));
            offsetLookup.Add("FMAP0I59.IMG", new Vector2(0, 0));        // Glenumbra Moors correct at 0,0
            offsetLookup.Add("FMAP0I60.IMG", new Vector2(107, 11));
            offsetLookup.Add("FMAP0I61.IMG", new Vector2(255, 275));    // Cybiades
        }

        #endregion


        #region Event Handlers

        // Handle clicks on the main panel.
        void ClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            position.y -= regionPanelOffset;

            if (position.x < 0 || position.x > regionTextureOverlayPanelRect.width || position.y < 0 || position.y > regionTextureOverlayPanelRect.height) //make sure clicks are inside region texture
                return;

            if (RegionSelected == false)
            {
                if (MouseOverRegion)
                    OpenRegionPanel(mouseOverRegion);
            }
            else if (locationSelected)
            {
                if (FindingLocation)
                    StopIdentify(true);
                else
                {
                    CreateConfirmTravelWindow();
                }
            }
            else if (MouseOverOtherRegion)      //if clicked while mouse over other region & not a location, switch to that region
                OpenRegionPanel(mouseOverRegion);
        }

        void ExitButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            CloseTravelWindows();
        }

        void AtButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            // Identify region or map location
            findingLocation = false;
            if (RegionSelected == false)
                StartIdentify();
            else
                StartIdentify();
        }

        void FindlocationButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (RegionSelected == false)           // Do nothing
                return;
            else                                // Open find location pop-up
            {
                //StopIdentify();
                DaggerfallInputMessageBox findPopUp = new DaggerfallInputMessageBox(uiManager, null, 31, HardStrings.findLocationPrompt, true, this);
                findPopUp.TextPanelDistanceY = 5;
                findPopUp.TextBox.WidthOverride = 308;
                findPopUp.TextBox.MaxCharacters = 32;
                findPopUp.OnGotUserInput += HandleLocationFindEvent;
                findPopUp.Show();
            }
        }

         /// <summary>
        /// Handles click events for the arrow buttons in the region view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="position"></param>
        void ArrowButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (RegionSelected == false || !HasMultipleMaps)
                return;
            int newIndex = mapIndex;

            if (sender.Name == "horizontalArrowButton")
            {
                if (newIndex % 2 == 0)
                    newIndex += 1;          // Move right
                else
                    newIndex -= 1;          // Move left
            }
            else if (sender.Name == "verticalArrowButton")
            {
                if (newIndex > 1)
                    newIndex -= 2;          // Move up
                else
                    newIndex += 2;          // Move down
            }
            else
            {
                return;
            }

            mapIndex = newIndex;
            SetupArrowButtons();
            loadNewImage = true;
            draw = true;
        }

        void PortButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (!portsFilter)
            {
                portsFilter = true;
                dungeonsFilterButton.Enabled = false;
                filterDungeons = false;
                townsFilterButton.Enabled = false;
                filterTowns = false;
                templesFilterButton.Enabled = false;
                filterTemples = false;
                homesFilterButton.Enabled = false;
                filterHomes = false;
            }
            else
            {
                portsFilter = false;
                dungeonsFilterButton.Enabled = true;
                filterDungeons = false;
                townsFilterButton.Enabled = true;
                filterTowns = false;
                templesFilterButton.Enabled = true;
                filterTowns = false;
                homesFilterButton.Enabled = true;
                filterTowns = false;

                dungeonsFilterButton.BackgroundTexture = dungeonFilterButtonEnabled;
                templesFilterButton.BackgroundTexture = templesFilterButtonEnabled;
                homesFilterButton.BackgroundTexture = homesFilterButtonEnabled;
                townsFilterButton.BackgroundTexture = townsFilterButtonEnabled;
            }

        }

        /// <summary>
        /// Handles click events for the filter buttons in the region view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="position"></param>
        void FilterButtonClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            if (sender.Name == "dungeonsFilterButton")
            {
                filterDungeons = !filterDungeons;
            }
            else if (sender.Name == "templesFilterButton")
            {
                filterTemples = !filterTemples;
            }
            else if (sender.Name == "homesFilterButton")
            {
                filterHomes = !filterHomes;
            }
            else if (sender.Name == "townsFilterButton")
            {
                filterTowns = !filterTowns;
            }
            else
            {
                return;
            }

            if (filterDungeons)
                dungeonsFilterButton.BackgroundTexture = dungeonFilterButtonDisabled;
            else
                dungeonsFilterButton.BackgroundTexture = dungeonFilterButtonEnabled;
            if (filterTemples)
                templesFilterButton.BackgroundTexture = templesFilterButtonDisabled;
            else
                templesFilterButton.BackgroundTexture = templesFilterButtonEnabled;
            if (filterHomes)
                homesFilterButton.BackgroundTexture = homesFilterButtonDisabled;
            else
                homesFilterButton.BackgroundTexture = homesFilterButtonEnabled;
            if (filterTowns)
                townsFilterButton.BackgroundTexture = townsFilterButtonDisabled;
            else
                townsFilterButton.BackgroundTexture = townsFilterButtonEnabled;

            draw = true;
        }

        #endregion

        #region Methods

        //opemn region panel
        void OpenRegionPanel(int region)
        {
            string[] mapNames = GetRegionMapNames(region);
            if (mapNames == null || mapNames.Length == 0)
                return;

            mapIndex = 0;
            selectedRegion = region;
            selectedRegionMapNames = mapNames;
            regionTextureOverlayPanel.Enabled = true;
            portButton.Enabled = false;
            borderPanel.Enabled = true;
            pixelBuffer = null;
            loadNewImage = true;
            draw = true;
            findButton.Enabled = true;
            currentDFRegion = DaggerfallUnity.ContentReader.MapFileReader.GetRegion(region);
            StartIdentify();
            SetupArrowButtons();
        }

        // Close region panel and reset values
        void CloseRegionPanel()
        {
            selectedRegion = -1;
            mouseOverRegion = -1;
            locationSelected = false;
            mapIndex = 0;
            regionTextureOverlayPanel.Enabled = false;
            portButton.Enabled = true;
            borderPanel.Enabled = false;
            horizontalArrowButton.Enabled = false;
            verticalArrowButton.Enabled = false;
            findButton.Enabled = false;
            loadNewImage = true;
            draw = true;
            zoom = false;
            pixelBuffer = null;
            StartIdentify();
        }

        // checks if location with MapSummary summary is already discovered
        bool checkLocationDiscovered(ContentReader.MapSummary summary)
        {
            if (GameManager.Instance.PlayerGPS.HasDiscoveredLocation(summary.ID) ||
                summary.Discovered ||
                revealUndiscoveredLocations == true)
            {
                return true;
            }
            return false;
        }

        // Sets pixels for selected region
        void SetLocationPixels()
        {
            try
            {
                if (pixelBuffer == null || pixelBuffer.Length != (width * height))
                {
                    Debug.LogError("invalid pixelBuffer in SetLocationPixels()");
                    return;
                }

                string mapName = selectedRegionMapNames[mapIndex];
                Vector2 origin = offsetLookup[mapName];
                int originX = (int)origin.x;
                int originY = (int)origin.y;

                // Find locations within this region

                scale = GetRegionMapScale(selectedRegion);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = (int)((((height - y - 1) * width) + x) * scale);
                        if (offset >= (width * height))
                            continue;
                        int sampleRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetPoliticIndex(originX + x, originY + y) - 128;

                        // Set location pixel if inside region area
                        if (sampleRegion == selectedRegion)
                        {
                            ContentReader.MapSummary summary;
                            if (DaggerfallUnity.Instance.ContentReader.HasLocation(originX + x, originY + y, out summary))
                            {
                                if (!checkLocationDiscovered(summary))
                                    continue;

                                int index = GetPixelColorIndex(summary.LocationType);
                                if (index == -1)
                                    continue;

                                else if (portsFilter && !tediousData.IsPortTown(summary.RegionIndex, summary.MapIndex))
                                    continue;

                                else
                                    pixelBuffer[offset] = locationPixelColors[index];
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("{0}\n{1}", ex.TargetSite, ex.Message));
            }
        }

        Vector2 GetCoordinates()
        {
            string mapName = selectedRegionMapNames[mapIndex];
            Vector2 origin = offsetLookup[mapName];

            Vector2 results = Vector2.zero;
            Vector2 pos = regionTextureOverlayPanel.ScaledMousePosition;

            if (zoom)
            {
                results.x = (int)Math.Floor(pos.x / zoomfactor + zoomOffset.x + origin.x);
                float diffy = height / zoomfactor - pos.y;
                results.y = (int)Math.Floor(height - pos.y / zoomfactor - zoomOffset.y - diffy + origin.y);
            }
            else
            {
                results.x = (int)Math.Floor(origin.x + pos.x);
                results.y = (int)Math.Floor(origin.y + pos.y);
            }

            //coordsLabel.Text = string.Format("{0}, {1}", results.x, results.y);

            return results;
        }


        //checks if player mouse over valid location while region selected & not finding location
        void UpdateMouseOverLocation()
        {
            if (RegionSelected == false || FindingLocation)
                return;

            locationSelected = false;
            mouseOverRegion = selectedRegion;

            if (lastMousePos.x < 0 || lastMousePos.x > regionTextureOverlayPanelRect.width || lastMousePos.y < regionPanelOffset || lastMousePos.y > regionTextureOverlayPanel.Size.y + regionPanelOffset)
                return;

            float scale = GetRegionMapScale(selectedRegion);
            Vector2 coordinates = GetCoordinates();
            int x = (int)(coordinates.x / scale);
            int y = (int)(coordinates.y / scale);

            if (selectedRegion == betonyIndex) // Manually correct Betony offset
            {
                x += 60;
                y += 212;
            }

            if (selectedRegion == 61) // Fix for Cybiades zoom-in map. Map is more zoomed in than for other regions but the pixel coordinates are not scaled to match.
                                      // The upper right corner of Cybiades (about x=440 y=340) is the same for both Cybiades's zoomed-in map and Sentinel's less zoomed in map,
                                      // so that is being used as the base for this fix.
            {
                int xDiff = x - 440;
                int yDiff = y - 340;
                xDiff /= 4;
                yDiff /= 4;
                x = 440 + xDiff;
                y = 340 + yDiff;
            }

            int sampleRegion = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetPoliticIndex(x, y) - 128;

            if (sampleRegion != selectedRegion && sampleRegion >= 0 && sampleRegion < DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount)
            {
                mouseOverRegion = sampleRegion;
                return;
            }

            if (DaggerfallUnity.ContentReader.HasLocation(x, y) && !FindingLocation)
            {
                DaggerfallUnity.ContentReader.HasLocation(x, y, out locationSummary);

                if (locationSummary.MapIndex < 0 || locationSummary.MapIndex >= currentDFRegion.MapNames.Length)
                    return;
                else
                {
                    int index = GetPixelColorIndex(locationSummary.LocationType);
                    if (index == -1)
                        return;

                    // only make location selectable if it is already discovered
                    if (!checkLocationDiscovered(locationSummary))
                        return;

                    if (portsFilter && !tediousData.IsPortTown(locationSummary.RegionIndex, locationSummary.MapIndex))
                        return;

                    locationSelected = true;
                }
            }
        }

        //check if mouse over a region
        void UpdateMouseOverRegion()
        {
            mouseOverRegion = -1;

            int x = 0;
            int y = 0;

            if (zoom)
            {
                var zoomCoords = GetCoordinates();
                x = (int)zoomCoords.x;
                y = (int)zoomCoords.y;
            }
            else
            {
                x = (int)lastMousePos.x;
                y = (int)lastMousePos.y;
            }

            // Get offset into region picker bitmap
            int offset = y * regionPickerBitmap.Width + x;
            if (offset < 0 || offset >= regionPickerBitmap.Data.Length)
                return;

            // Get region from bitmap, if any
            int region = regionPickerBitmap.Data[offset] - 128;
            if (region < 0 || region >= DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount)
                return;

            // Store valid region
            mouseOverRegion = region;
        }

        // Updates the text label at top of screen
        void UpdateRegionLabel()
        {
            if (RegionSelected == false)
                regionLabel.Text = GetRegionName(mouseOverRegion);
            else if (locationSelected)
                regionLabel.Text = string.Format("{0} : {1}", DaggerfallUnity.ContentReader.MapFileReader.GetRegionName(mouseOverRegion), currentDFRegion.MapNames[locationSummary.MapIndex]);
            else if (MouseOverOtherRegion)
                regionLabel.Text = string.Format("Switch To: {0} Region", DaggerfallUnity.ContentReader.MapFileReader.GetRegionName(mouseOverRegion));
            else
                regionLabel.Text = GetRegionName(mouseOverRegion);
        }

        // Closes windows based on context
        public void CloseTravelWindows(bool forceClose = false)
        {
            if (RegionSelected == false || forceClose)
                CloseWindow();
            else            // Close region panel
                CloseRegionPanel();
        }

        // Updates search button toggle state based on current flags
        void UpdateSearchButtons()
        {
            // Dungeons
            if (!filterDungeons)
                dungeonsFilterButton.BackgroundTexture = dungeonFilterButtonEnabled;
            else
                dungeonsFilterButton.BackgroundTexture = dungeonFilterButtonDisabled;

            // Temples
            if (!filterTemples)
                templesFilterButton.BackgroundTexture = templesFilterButtonEnabled;
            else
                templesFilterButton.BackgroundTexture = templesFilterButtonDisabled;

            // Homes
            if (!filterHomes)
                homesFilterButton.BackgroundTexture = homesFilterButtonEnabled;
            else
                homesFilterButton.BackgroundTexture = homesFilterButtonDisabled;

            // Towns
            if (!filterTowns)
                townsFilterButton.BackgroundTexture = townsFilterButtonEnabled;
            else
                townsFilterButton.BackgroundTexture = townsFilterButtonDisabled;
        }

        /**
         * A popup window informing the player that there is no regular shipping to or from a chosen port.
         */ 
        void CreateNoShipAvailablePopup(bool noLeavingShip = true)
        {
            var message = "";
            if (noLeavingShip)
                message = "There is no regular shipping at this port.\nIf yonly you had your own boat...";
            else
                message = "No ships are sailing for this port.\n If you had your own, it would be another matter...";

            DaggerfallMessageBox noShipAvailableMessageBox = new DaggerfallMessageBox(
                    uiManager,
                    DaggerfallMessageBox.CommonMessageBoxButtons.Nothing,
                    message,
                    this);
            noShipAvailableMessageBox.ClickAnywhereToClose = true;
            noShipAvailableMessageBox.Show();
        }

        void CreateShipTravelPopup()
        {
            var travelTimeCalculator = new TravelTimeCalculator();
            var destinationPosition = MapsFile.GetPixelFromPixelID(locationSummary.ID);
            int minutes = travelTimeCalculator.CalculateTravelTime(
                destinationPosition,
                false, false, true, false, false);

            if (!PlayerOwnsShip())
            {
                minutes = (int)Math.Round((float)minutes * GetLocationTraveltimeModifier(
                    GameManager.Instance.PlayerGPS.CurrentLocationType,
                    locationSummary.LocationType));
            }

            var days = (int)Math.Ceiling((float)minutes / 1440);

            var tripCost = 0;
            if (!PlayerOwnsShip() && !GameManager.Instance.GuildManager.FreeShipTravel())
            {
                tripCost = days * 15;
            }

            if (GameManager.Instance.PlayerEntity.GoldPieces < tripCost)
            {
                DaggerfallMessageBox noMoneyMessageBox = new DaggerfallMessageBox(
                    uiManager,
                    DaggerfallMessageBox.CommonMessageBoxButtons.Nothing,
                    "Unfortunately you do not have the " + tripCost + " gold pieces required for the " + days + " days journey");
                noMoneyMessageBox.ClickAnywhereToClose = true;
                noMoneyMessageBox.Show();
            }
            else
            {
                DaggerfallMessageBox confirmShipTravelBox = new DaggerfallMessageBox(
                    uiManager,
                    DaggerfallMessageBox.CommonMessageBoxButtons.YesNo,
                    "The journey by boat will take  " + days + " days and cost " + tripCost + " gold pieces. Begin journey?",
                    this);

                confirmShipTravelBox.OnButtonClick += (_sender, button) =>
                {
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                    {
                        shipTravelDestination = new ShipTravelData(minutes, destinationPosition, tripCost);
                    }
                    uiManager.PopWindow();
                };
                confirmShipTravelBox.Show();
            }
        }

        void CreateConfirmTravelWindow()
        {

            if (portsFilter &&
                (GameManager.Instance.PlayerGPS.HasCurrentLocation &&
                 TediousData.Instance.IsPortTown(
                     GameManager.Instance.PlayerGPS.CurrentLocation.RegionIndex,
                     GameManager.Instance.PlayerGPS.CurrentLocation.LocationIndex)))
            {
                // player wants to travel to a port town AND is currently in a port town. do fast travel by ship.
                if (!PlayerOwnsShip())
                {

                    if (!MayHaveActivePort(GameManager.Instance.PlayerGPS.CurrentLocation.MapTableData.LocationType))
                    {
                        CreateNoShipAvailablePopup(true);
                    }
                    else if (!MayHaveActivePort(locationSummary.LocationType))
                    {
                        CreateNoShipAvailablePopup(false);
                    }
                    else
                    {
                        CreateShipTravelPopup();
                    }
                }
                else
                {
                    CreateShipTravelPopup();
                }
            }
            else
            {
                var destinationRect = PlayerAutoPilot.GetLocationRect(locationSummary);
                var destinationWorldPos = destinationRect.center;
                var playerPos = new Vector2(GameManager.Instance.PlayerGPS.WorldX, GameManager.Instance.PlayerGPS.WorldZ);
                var distance = (playerPos - destinationWorldPos).magnitude;
                // 3.5 units per second in-game (not realtime second) seems to approximate 1 unit of speed...
                var seconds = distance / (3.5 * GameManager.Instance.SpeedChanger.GetBaseSpeed());



                var travelDuration = "";
                // if estimated time is less than 24 hours, show total hours.
                // if it is more, show estimated time in days, assuming 8 hours of travel and 16 hours of rest per day.
                if (seconds < 86400) travelDuration = ((int)Math.Ceiling(seconds / 3600)).ToString() + " hours.";
                else travelDuration = ((int)Math.Ceiling(seconds / 28800)).ToString() + " days";

                DaggerfallMessageBox confirmTravelBox = new DaggerfallMessageBox(
                    uiManager,
                    DaggerfallMessageBox.CommonMessageBoxButtons.YesNo,
                    "Estimated travel time is " + travelDuration + ". Begin journey?",
                    this);

                confirmTravelBox.OnButtonClick += (_sender, button) =>
                {
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                    {
                        uiManager.PopWindow();
                        InitTravel();
                    }
                    else
                    {
                        uiManager.PopWindow();
                    }
                };
                confirmTravelBox.Show();
            }
        }

        void InitTravel()
        {
            this.CloseWindow();
            controller.StartFastTravel(locationSummary);
        }

        private void performShipTravel(ShipTravelData destination)
        {
            GameManager.Instance.StreamingWorld.TeleportToCoordinates(
                destination.destination.X, destination.destination.Y, StreamingWorld.RepositionMethods.RandomStartMarker);

            GameManager.Instance.PlayerEntity.CurrentHealth = GameManager.Instance.PlayerEntity.MaxHealth;
            GameManager.Instance.PlayerEntity.CurrentFatigue = GameManager.Instance.PlayerEntity.MaxFatigue;
            GameManager.Instance.PlayerEntity.CurrentMagicka = GameManager.Instance.PlayerEntity.MaxMagicka;
            GameManager.Instance.PlayerEntity.GoldPieces -= shipTravelDestination.cost;

            DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(destination.minutes * 60);

            // Halt random enemy spawns for next playerEntity update so player isn't bombarded by spawned enemies at the end of a long trip
            GameManager.Instance.PlayerEntity.PreventEnemySpawns = true;

            // Raise arrival time to just after 7am 
            if ((DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour < 7)
                || ((DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour == 7) && (DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute < 10)))
            {
                float raiseTime = (((7 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour) * 3600)
                                    + ((10 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute) * 60)
                                    - DaggerfallUnity.WorldTime.DaggerfallDateTime.Second);
                DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(raiseTime);
            }
            else if (DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour > 17)
            {
                float raiseTime = (((31 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Hour) * 3600)
                + ((10 - DaggerfallUnity.WorldTime.DaggerfallDateTime.Minute) * 60)
                - DaggerfallUnity.WorldTime.DaggerfallDateTime.Second);
                DaggerfallUnity.WorldTime.DaggerfallDateTime.RaiseTime(raiseTime);
            }

            CloseWindow();
            GameManager.Instance.PlayerEntity.RaiseSkills();
            DaggerfallUI.Instance.FadeBehaviour.FadeHUDFromBlack();
        }

        /**
         * Returns true if it is possible that this location type has an active port.
         * Note that false doesn't mean that it cannot have a port, merely that if it has one, that port is not active.
         */ 
        bool MayHaveActivePort(DFRegion.LocationTypes locationType)
        {
            if (locationType == DFRegion.LocationTypes.ReligionTemple ||
                locationType == DFRegion.LocationTypes.Tavern ||
                locationType == DFRegion.LocationTypes.TownCity ||
                locationType == DFRegion.LocationTypes.TownHamlet ||
                locationType == DFRegion.LocationTypes.TownVillage ||
                locationType == DFRegion.LocationTypes.HomeWealthy)
                return true;

            return false;
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

        bool PlayerOwnsShip()
        {
            return DaggerfallWorkshop.Game.Banking.DaggerfallBankManager.OwnsShip;
        }


        #endregion

        #region Helper Methods

        //returns index to locationPixelColor array or -1 if invalid or filtered
        int GetPixelColorIndex(DFRegion.LocationTypes locationType)
        {
            int index = -1;
            switch (locationType)
            {
                case DFRegion.LocationTypes.DungeonLabyrinth:
                    index = 0;
                    break;
                case DFRegion.LocationTypes.DungeonKeep:
                    index = 1;
                    break;
                case DFRegion.LocationTypes.DungeonRuin:
                    index = 2;
                    break;
                case DFRegion.LocationTypes.Graveyard:
                    index = 3;
                    break;
                case DFRegion.LocationTypes.Coven:
                    index = 4;
                    break;
                case DFRegion.LocationTypes.HomeFarms:
                    index = 5;
                    break;
                case DFRegion.LocationTypes.HomeWealthy:
                    index = 6;
                    break;
                case DFRegion.LocationTypes.HomePoor:
                    index = 7;
                    break;
                case DFRegion.LocationTypes.HomeYourShips:
                    break;
                case DFRegion.LocationTypes.ReligionTemple:
                    index = 8;
                    break;
                case DFRegion.LocationTypes.ReligionCult:
                    index = 9;
                    break;
                case DFRegion.LocationTypes.Tavern:
                    index = 10;
                    break;
                case DFRegion.LocationTypes.TownCity:
                    index = 11;
                    break;
                case DFRegion.LocationTypes.TownHamlet:
                    index = 12;
                    break;
                case DFRegion.LocationTypes.TownVillage:
                    index = 13;
                    break;
                default:
                    break;
            }
            if (index < 0)
                return index;
            else if (index < 5 && filterDungeons)
                index = -1;
            else if (index > 4 && index < 8 && filterHomes)
                index = -1;
            else if (index > 7 && index < 10 && filterTemples)
                index = -1;
            else if (index > 9 && index < 14 && filterTowns)
                index = -1;
            return index;
        }

        // Handles events from Find Location pop-up.
        void HandleLocationFindEvent(DaggerfallInputMessageBox inputMessageBox, string locationName)
        {
            if (string.IsNullOrEmpty(locationName) || !FindLocation(locationName))
            {
                TextFile.Token[] textTokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(13);
                DaggerfallMessageBox messageBox = new DaggerfallMessageBox(uiManager, this);
                messageBox.SetTextTokens(textTokens);
                messageBox.ClickAnywhereToClose = true;
                uiManager.PushWindow(messageBox);
                return;
            }
            else //place flashing crosshair over location
            {
                locationSelected = true;
                findingLocation = true;
                StartIdentify();
            }
        }

        // Find location by name
        bool FindLocation(string name)
        {
            DFRegion.RegionMapTable locationInfo = new DFRegion.RegionMapTable();

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            string[] locations = currentDFRegion.MapNames.OrderBy(p => p).ToArray();
            name = name.ToLower();

            // bug-fix in find location functionality (see http://forums.dfworkshop.net/viewtopic.php?f=24&p=5362#p5362)          
            // first search for location with exact name as the search string
            for (int i = 0; i < locations.Count(); i++)
            {
                if (locations[i].ToLower() == name) // Valid location found with exact name
                {
                    if (!currentDFRegion.MapNameLookup.ContainsKey(locations[i]))
                    {
                        DaggerfallUnity.LogMessage("Error: location name key not found in Region MapNameLookup dictionary");
                        return false;
                    }
                    int index = currentDFRegion.MapNameLookup[locations[i]];
                    locationInfo = currentDFRegion.MapTable[index];
                    DFPosition pos = MapsFile.LongitudeLatitudeToMapPixel((int)locationInfo.Longitude, (int)locationInfo.Latitude);
                    if (DaggerfallUnity.ContentReader.HasLocation(pos.X, pos.Y, out locationSummary))
                    {
                        // only make location searchable if it is already discovered
                        if (!checkLocationDiscovered(locationSummary))
                            continue;

                        return true;
                    }
                }
            }

            // location with exact name was not found, search for substring containing the search string
            for (int i = 0; i < locations.Count(); i++)
            {
                if (locations[i].ToLower().Contains(name)) // Valid location found with substring
                {
                    if (!currentDFRegion.MapNameLookup.ContainsKey(locations[i]))
                    {
                        DaggerfallUnity.LogMessage("Error: location name key not found in Region MapNameLookup dictionary");
                        return false;
                    }
                    int index = currentDFRegion.MapNameLookup[locations[i]];
                    locationInfo = currentDFRegion.MapTable[index];
                    DFPosition pos = MapsFile.LongitudeLatitudeToMapPixel((int)locationInfo.Longitude, (int)locationInfo.Latitude);
                    if (DaggerfallUnity.ContentReader.HasLocation(pos.X, pos.Y, out locationSummary))
                    {
                        // only make location searchable if it is already discovered
                        if (!checkLocationDiscovered(locationSummary))
                            continue;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (locations[i][0] > name[0])
                    return false;
            }

            return false;
        }

        //creates a ListPickerWindow with a list of locations from current region
        //locations displayed will be filtered out depending on the dungeon / town / temple / home button settings
        private void ShowLocationPicker()
        {

            if (!RegionSelected || currentDFRegion.LocationCount < 1)
                return;

            DaggerfallListPickerWindow locationPicker = new DaggerfallListPickerWindow(uiManager, this);
            locationPicker.OnItemPicked += HandleLocationPickEvent;
            locationPicker.ListBox.MaxCharacters = 29;

            string[] locations = currentDFRegion.MapNames.OrderBy(p => p).ToArray();

            for (int i = 0; i < locations.Length; i++)
            {
                int index = currentDFRegion.MapNameLookup[locations[i]];
                if (GetPixelColorIndex(currentDFRegion.MapTable[index].LocationType) == -1)
                    continue;
                else
                    locationPicker.ListBox.AddItem(locations[i]);
            }

            uiManager.PushWindow(locationPicker);
        }

        public void HandleLocationPickEvent(int index, string locationName)
        {
            if (!RegionSelected || currentDFRegion.LocationCount < 1)
                return;

            CloseWindow();
            HandleLocationFindEvent(null, locationName);
        }

        // Gets current player position in map pixels
        DFPosition GetPlayerMapPosition()
        {
            DFPosition position = new DFPosition();
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;
            if (playerGPS)
                position = playerGPS.CurrentMapPixel;

            return position;
        }

        // Gets current player region or -1 if player not in any region (e.g. in ocean)
        int GetPlayerRegion()
        {
            DFPosition position = GetPlayerMapPosition();
            int region = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetPoliticIndex(position.X, position.Y) - 128;
            if (region < 0 || region >= DaggerfallUnity.Instance.ContentReader.MapFileReader.RegionCount)
                return -1;

            return region;
        }

        // Gets name of region
        string GetRegionName(int region)
        {
            return DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegionName(region);
        }

        // Gets maps for region
        string[] GetRegionMapNames(int region)
        {
            // Get map name array with special handling for multi-screen regions
            if (region == 0)
                return new string[] { "FMAPAI00.IMG", "FMAPBI00.IMG" };
            else if (region == 1)
                return new string[] { "FMAPAI01.IMG", "FMAPBI01.IMG", "FMAPCI01.IMG", "FMAPDI01.IMG" };
            else if (region == 16)
                return new string[] { "FMAPAI16.IMG", "FMAPBI16.IMG", "FMAPCI16.IMG", "FMAPDI16.IMG" };
            else
                return new string[] { string.Format("FMAP0I{0:00}.IMG", region) };
        }

        // Gets scale of region map
        float GetRegionMapScale(int region)
        {
            if (region == betonyIndex)//betony
                return 4f;
            else
                return 1;
        }

 

        #endregion


        #region Region Identification

        // Start region identification & location crosshair
        void StartIdentify()
        {
            var oldFindingLocation = findingLocation;
            if (identifying)//stop animation
                StopIdentify(false);
            identifying = true;
            identifyState = false;
            identifyChanges = 0;
            identifyLastChangeTime = 0;
            findingLocation = oldFindingLocation;
        }

        // Stop region identification & location crosshair
        void StopIdentify(bool createPopUp = true)
        {
            if (FindingLocation && createPopUp)
                CreateConfirmTravelWindow();

            identifying = false;
            identifyState = false;
            identifyChanges = 0;
            identifyLastChangeTime = 0;
            findingLocation = false;
        }

        // Animate region identification & location crosshair
        void AnimateIdentify()
        {
            if (!identifying)
            {
                return;
            }
            //redraw texture while animating
            draw = true;

            // Check if enough time has elapsed since last flash and toggle state
            bool lastIdentifyState = identifyState;
            float time = Time.realtimeSinceStartup;

            if (time > identifyLastChangeTime + identifyFlashInterval)
            {
                identifyState = !identifyState;
                identifyLastChangeTime = time;
            }

            // Turn off flash after specified number of on states
            if (!lastIdentifyState && identifyState)
            {
                if (++identifyChanges > identifyFlashCount)
                {
                    StopIdentify();
                }
            }
        }

        #endregion

        #region console_commands

        public static class TravelMapConsoleCommands
        {
            public static void RegisterCommands()
            {
                try
                {
                    ConsoleCommandsDatabase.RegisterCommand(RevealLocations.name, RevealLocations.description, RevealLocations.usage, RevealLocations.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(HideLocations.name, HideLocations.description, HideLocations.usage, HideLocations.Execute);
                    ConsoleCommandsDatabase.RegisterCommand(RevealLocation.name, RevealLocation.description, RevealLocation.usage, RevealLocation.Execute);
                }
                catch (System.Exception ex)
                {
                    DaggerfallUnity.LogMessage(ex.Message, true);
                }
            }

            private static class RevealLocations
            {
                public static readonly string name = "map_reveallocations";
                public static readonly string description = "Reveals undiscovered locations on travelmap (temporary)";
                public static readonly string usage = "map_reveallocations";


                public static string Execute(params string[] args)
                {
                    if (GameManager.Instance.IsPlayerInside)
                    {
                        return "this command only has an effect when outside";
                    }

                    revealUndiscoveredLocations = true;
                    return "undiscovered locations have been revealed (temporary) on the travelmap";
                }
            }

            private static class HideLocations
            {
                public static readonly string name = "map_hidelocations";
                public static readonly string description = "Hides undiscovered locations on travelmap";
                public static readonly string usage = "map_hidelocations";


                public static string Execute(params string[] args)
                {
                    if (GameManager.Instance.IsPlayerInside)
                    {
                        return "this command only has an effect when outside";
                    }

                    revealUndiscoveredLocations = false;
                    return "undiscovered locations have been hidden on the travelmap again";
                }

            }

            private static class RevealLocation
            {
                public static readonly string name = "map_reveallocation";
                public static readonly string error = "Failed to reveal location with given regionName and locatioName on travelmap";
                public static readonly string description = "Permanently reveals the location with [locationName] in region [regionName] on travelmap";
                public static readonly string usage = "map_reveallocation [regionName] [locationName] - inside the name strings use underscores instead of spaces, e.g Dragontail_Mountains";

                public static string Execute(params string[] args)
                {
                    if (args == null || args.Length < 2)
                    {
                        try
                        {
                            Wenzil.Console.Console.Log("please provide both a region name as well as a location name");
                            return HelpCommand.Execute(RevealLocation.name);
                        }
                        catch
                        {
                            return HelpCommand.Execute(RevealLocation.name);
                        }
                    }
                    else
                    {
                        string regionName = args[0];
                        string locationName = args[1];
                        regionName = regionName.Replace("_", " ");
                        locationName = locationName.Replace("_", " ");
                        try
                        {
                            GameManager.Instance.PlayerGPS.DiscoverLocation(regionName, locationName);
                            return String.Format("revealed location {0} : {1} on the travelmap", regionName, locationName);
                        }
                        catch (Exception ex)
                        {
                            return string.Format("Could not reveal location: {0}", ex.Message);
                        }
                    }
                }
            }
        }

        #endregion

    }
}
