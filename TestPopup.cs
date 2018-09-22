// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Hazelnut

using UnityEngine;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Banking;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public class TestPopup : DaggerfallPopupWindow
    {
        #region UI Rects

        Rect fasterButtonRect = new Rect(5, 5, 20, 10);
        Rect tempRect = new Rect(30, 5, 60, 10);
        Rect slowerButtonRect = new Rect(95, 5, 20, 10);
        Rect campButtonRect = new Rect(120, 5, 40, 10);
        Rect interruptButtonRect = new Rect(165, 5, 40, 10);

        #endregion

        #region UI Controls

        Panel mainPanel = null;
        Button fasterButton;
        Button slowerButton;
        Button campButton;
        Button interruptButton;

        #endregion

        #region UI Textures

        Texture2D baseTexture;
        Texture2D disabledTexture;

        #endregion

        #region Fields

        const string baseTextureName = "MOVE00I0.IMG";
        const string disabledTextureName = "MOVE01I0.IMG";

        Vector2 baseSize;
        public bool isShowing = false;

        KeyCode toggleClosedBinding;

        #endregion

        #region Constructors

        public TestPopup(IUserInterfaceManager uiManager)
            : base(uiManager)
        {
            // Clear background
            ParentPanel.BackgroundColor = Color.clear;
            this.pauseWhileOpened = false;
        }

        #endregion

        #region Setup Methods

        protected override void Setup()
        {
            // Load all textures
            LoadTextures();
            
            // Create interface panel
            Rect mainPanelRect = new Rect(0, 0, 210, 20);
            mainPanel = DaggerfallUI.AddPanel(mainPanelRect);
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Top;
            mainPanel.BackgroundTexture = baseTexture;
            
            //mainPanel.Position = new Vector2(0, 50);
            //mainPanel.Size = baseSize;
            DFSize disabledTextureSize = new DFSize(122, 36);

            // Foot button
            fasterButton = DaggerfallUI.AddButton(fasterButtonRect, mainPanel);
            fasterButton.OnMouseClick += FasterButton_OnMouseClick;

            // Horse button
            slowerButton = DaggerfallUI.AddButton(slowerButtonRect, mainPanel);
            slowerButton.OnMouseClick += SlowerButton_OnMouseClick;

            // Cart button
            campButton = DaggerfallUI.AddButton(campButtonRect, mainPanel);
            campButton.OnMouseClick += CampButton_OnMouseClick;

            // Ship button
            interruptButton = DaggerfallUI.AddButton(interruptButtonRect, mainPanel);
            interruptButton.OnMouseClick += InterruptButton_OnMouseClick;

            NativePanel.Components.Add(mainPanel);

        }

        #endregion

        #region Overrides

        public override void Update()
        {
            base.Update();
        }

        public override void Draw()
        {
            base.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.ShowMidScreenText = true;
        }

        #endregion

        #region Private Methods

        void LoadTextures()
        {
            ImageData baseData = ImageReader.GetImageData(baseTextureName);
            baseTexture = baseData.texture;
            baseSize = new Vector2(baseData.width, baseData.height);
            disabledTexture = ImageReader.GetTexture(disabledTextureName);
        }

        #endregion

        #region Event Handlers

        private void FasterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void SlowerButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void CampButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void InterruptButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        #endregion
    }
}
