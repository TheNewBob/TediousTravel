using UnityEngine;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game;
using System;

namespace TediousTravel
{
    public class TediousTravelControllMenu : DaggerfallPopupWindow
    {

        #region UI Rects
        Rect mainPanelRect = new Rect(0, 0, 215, 24);
        Rect destinationRect = new Rect(5, 2, 210, 10);
        Rect fasterButtonRect = new Rect(5, 12, 20, 10);
        Rect timeCompressionRect = new Rect(30, 12, 20, 10);
        Rect slowerButtonRect = new Rect(55, 12, 20, 10);
        Rect mapButtonRect = new Rect(80, 12, 40, 10);
        Rect interruptButtonRect = new Rect(125, 12, 40, 10);
        Rect cancelButtonRect = new Rect(170, 12, 40, 10);
        #endregion

        #region UI Controls

        Panel mainPanel = null;
        Button fasterButton;
        Button slowerButton;
        Button interruptButton;
        Button cancelButton;
        Button mapButton;
        TextBox destinationTextbox;
        TextBox timeCompressionTextbox;
        #endregion

        #region UI Textures

        Texture2D baseTexture;
        Texture2D disabledTexture;

        #endregion

        #region Fields

        public bool isShowing = false;

        KeyCode toggleClosedBinding;

        Color mainPanelBackgroundColor = new Color(0.0f, 0f, 0.0f, 1.0f);
        Color buttonBackgroundColor = new Color(0.0f, 0.5f, 0.0f, 0.4f);
        Color cancelButtonBackgroundColor = new Color(0.7f, 0.0f, 0.0f, 0.4f);
        string _destinationName = "";

        int timeCompressionSetting = 10;

        TediousTravelMap travelMap = null;

        public int TimeCompressionSetting { get { return timeCompressionSetting; } }
        public string DestinationName
        {
            set
            {
                _destinationName = "Travelling to " + value;
                if (destinationTextbox != null)
                    destinationTextbox.Text = _destinationName;
            }
        }

        #endregion

        #region Constructors

        public TediousTravelControllMenu(IUserInterfaceManager uiManager, TediousTravelMap mapWindow)
            : base(uiManager)
        {
            // Clear background
            ParentPanel.BackgroundColor = Color.clear;
            travelMap = mapWindow;
            this.pauseWhileOpened = false;
        }

        #endregion

        #region Setup Methods

        protected override void Setup()
        {

            // Create interface panel
            mainPanel = DaggerfallUI.AddPanel(mainPanelRect);
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Top;
            mainPanel.BackgroundColor = mainPanelBackgroundColor;

            // destination description
            destinationTextbox = DaggerfallUI.AddTextBox(destinationRect, _destinationName, mainPanel);
            destinationTextbox.ReadOnly = true;
            // increase time compression button
            fasterButton = DaggerfallUI.AddButton(fasterButtonRect, mainPanel);
            fasterButton.OnMouseClick += FasterButton_OnMouseClick;
            fasterButton.BackgroundColor = buttonBackgroundColor;
            fasterButton.Label.Text = "+";
            //display time compression
            timeCompressionTextbox = DaggerfallUI.AddTextBox(timeCompressionRect, TimeCompressionSetting + "x", mainPanel);
            timeCompressionTextbox.ReadOnly = true;

            // decrease time compression button
            slowerButton = DaggerfallUI.AddButton(slowerButtonRect, mainPanel);
            slowerButton.OnMouseClick += SlowerButton_OnMouseClick;
            slowerButton.BackgroundColor = buttonBackgroundColor;
            slowerButton.Label.Text = "-";

            // map button
            mapButton = DaggerfallUI.AddButton(mapButtonRect, mainPanel);
            mapButton.OnMouseClick += (_, __) => {
                uiManager.PushWindow(travelMap);
            };
            mapButton.BackgroundColor = buttonBackgroundColor;
            mapButton.Label.Text = "Map";

            // interrupt travel button
            interruptButton = DaggerfallUI.AddButton(interruptButtonRect, mainPanel);
            interruptButton.OnMouseClick += (_, __) => { CloseWindow(); };
            interruptButton.BackgroundColor = buttonBackgroundColor;
            interruptButton.Label.Text = "Interrupt";

            // cancel travel button
            cancelButton = DaggerfallUI.AddButton(cancelButtonRect, mainPanel);
            cancelButton.OnMouseClick += (_, __) => { CancelWindow(); };
            cancelButton.BackgroundColor = cancelButtonBackgroundColor;
            cancelButton.Label.Text = "Cancel";

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
//            DaggerfallUI.Instance.DaggerfallHUD.HUDCompass.Draw();
            DaggerfallUI.Instance.DaggerfallHUD.ShowMidScreenText = true;
        }

        public override void OnPush()
        {
            base.OnPush();
            isShowing = true;
        }

        public override void OnPop()
        {
            base.OnPop();
            isShowing = false;
        }

        #endregion

        #region Private Methods

        #endregion

        #region Event Handlers

        private void FasterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (timeCompressionSetting == 1)
                timeCompressionSetting = 5;
            else timeCompressionSetting += 5;
            timeCompressionTextbox.Text = timeCompressionSetting.ToString() + "x";
            RaiseOnTimeCompressionChangedEvent(timeCompressionSetting);
        }

        private void SlowerButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            timeCompressionSetting = Mathf.Max(1, timeCompressionSetting - 5);
            timeCompressionTextbox.Text = timeCompressionSetting.ToString() + "x";
            RaiseOnTimeCompressionChangedEvent(timeCompressionSetting);
        }

        private void CampButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void CancelButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        // events
        public delegate void OnTimeCOmpressionChangedHandler(int newTimeCompression);
        public event OnTimeCOmpressionChangedHandler OnTimeCompressionChanged;
        void RaiseOnTimeCompressionChangedEvent(int newTimeCompression)
        {
            if (OnTimeCompressionChanged != null)
                OnTimeCompressionChanged(newTimeCompression);
        }


        #endregion
    }
}
