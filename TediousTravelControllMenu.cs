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

namespace TediousTravel
{
    public class TediousTravelControllMenu : DaggerfallPopupWindow
    {

        #region UI Rects

        Rect timeFasterRect = new Rect(4, 13, 48, 24);
        Rect timeLabelRect = new Rect(53, 13, 48, 24);
        Rect timeSlowerRect = new Rect(102, 13, 48, 24);
        Rect campButtonRect = new Rect(0, 50, 105, 41);
        Rect interruptButtonRect = new Rect(4, 10, 16, 8);
        Rect destinationLabelRect = new Rect(33, 26, 40, 10);

        #endregion

        #region UI Controls

        Button timeFasterButton;
        Button timeSlowerButton;
        Button campButton;
        Button interruptButton;

        Panel mainPanel = new Panel();
        TextLabel timeLabel = new TextLabel();
        TextLabel destinationLabel = new TextLabel();

        #endregion

        #region UI Textures

        Texture2D baseTexture;
//        Texture2D hoursPastTexture;
//        Texture2D hoursRemainingTexture;

        #endregion

        #region Fields

        const string baseTextureName = "REST00I0.IMG";              // Rest type
        const string hoursPastTextureName = "REST01I0.IMG";         // "Hours past"
        const string hoursRemainingTextureName = "REST02I0.IMG";    // "Hours remaining"


        PlayerEntity playerEntity;
        DaggerfallHUD hud;

        KeyCode toggleClosedBinding;
        TediousTravel controller;

        #endregion

        #region Enums

        #endregion

        #region Constructors

        public TediousTravelControllMenu(IUserInterfaceManager uiManager, TediousTravel controller)
            : base(uiManager)
        {
            this.controller = controller;
            this.PauseWhileOpen = false;
        }

        #endregion

        #region Setup Methods

        protected override void Setup()
        {
            
            // Load all the textures used by rest interface
            LoadTextures();

            // Hide world while resting
            //ParentPanel.BackgroundColor = Color.black;

            // Create interface panel

            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Top;
            mainPanel.BackgroundTexture = baseTexture;
            Rect mainPanelRect = new Rect(
                    new Vector2(0, 50),
                    new Vector2(ImageReader.GetImageData("REST00I0.IMG", 0, 0, false, false).width, ImageReader.GetImageData("REST00I0.IMG", 0, 0, false, false).height)
                );
            DaggerfallUI.AddPanel(mainPanelRect, mainPanel);

            //mainPanel.Position = new Vector2(0, 50);
            // mainPanel.Size = new Vector2(ImageReader.GetImageData("REST00I0.IMG", 0, 0, false, false).width, ImageReader.GetImageData("REST00I0.IMG", 0, 0, false, false).height);

            //            NativePanel.Components.Add(mainPanel);

            // Create buttons
            timeFasterButton = DaggerfallUI.AddButton(timeFasterRect, mainPanel);
            timeFasterButton.OnMouseClick += timeFaster_OnMouseClick;
            timeSlowerButton = DaggerfallUI.AddButton(timeSlowerRect, mainPanel);
            timeSlowerButton.OnMouseClick += timeSlower_OnMouseClick;
            campButton = DaggerfallUI.AddButton(campButtonRect, mainPanel);
            campButton.OnMouseClick += campButton_OnMouseClick;
            interruptButton = DaggerfallUI.AddButton(interruptButtonRect, mainPanel);
            interruptButton.OnMouseClick += campButton_OnMouseClick;

            //controller.StartFastTravel();

            //timeLabel = DaggerfallUI.AddTextLabel()

            // Store toggle closed binding for this window
//            toggleClosedBinding = InputManager.Instance.GetBinding(InputManager.Actions.Rest);
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

            // Draw vitals
            if (hud != null)
            {
                hud.HUDVitals.Draw();
            }
        }

        public override void OnPush()
        {
            base.OnPush();

            // Get references
            playerEntity = GameManager.Instance.PlayerEntity;
            hud = DaggerfallUI.Instance.DaggerfallHUD;
        }

        public override void OnPop()
        {
            base.OnPop();

//            Debug.Log(string.Format("Resting raised time by {0} hours total", totalHours));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually abort rest for enemy spawn.
        /// </summary>
  
        #endregion

        #region Private Methods

        void LoadTextures()
        {
            baseTexture = ImageReader.GetTexture(baseTextureName);
            //hoursPastTexture = ImageReader.GetTexture(hoursPastTextureName);
            //hoursRemainingTexture = ImageReader.GetTexture(hoursRemainingTextureName);
        }


        #endregion

        #region Event Handlers

        private void timeFaster_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void timeSlower_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void campButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
        }

        private void interruptButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PopToHUD();
        }

        private void RestFinishedPopup_OnClose()
        {
            DaggerfallUI.Instance.PopToHUD();
        }

        #endregion

    }
}
