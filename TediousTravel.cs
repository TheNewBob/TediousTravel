using UnityEngine;
using System.Collections;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows; //required for pop-up window
using DaggerfallWorkshop.Game.Utility.ModSupport;   //required for modding features
using System;
using System.Reflection;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallConnect.Arena2;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;

namespace TediousTravel
{
    public class TediousTravel : MonoBehaviour
    {
        private TediousTravelMap travelMap = null;
        private TediousTravelControllMenu travelUi = null;
        private InputManager inputManager = null;
        private ContentReader.MapSummary destinationSummary;
        private PlayerAutoPilot playerAutopilot = null;
        private string destinationName = "";
        private PlayerCollision playerCollision = null;
        private AudioSource ridingAudioSource;

        private float baseFixedDeltaTime;

        public TediousTravelMap TravelMap { get { return travelMap; } }

        private void Start()
        {
            TediousData.Instance.LoadPortTowns();
            baseFixedDeltaTime = Time.fixedDeltaTime;
            DaggerfallUI.UIManager.OnWindowChange += travelMapInterceptor;
            travelMap = new TediousTravelMap(DaggerfallUI.UIManager, this);
            travelUi = new TediousTravelControllMenu(DaggerfallUI.UIManager, travelMap);
            travelUi.OnCancel += (sender) => {
                destinationName = "";
            };
            travelUi.OnClose += () => {
                InterruptFastTravel();
            };

            travelUi.OnTimeCompressionChanged += (newTimeCompression) => { SetTimeScale(newTimeCompression); };

            ridingAudioSource = GameManager.Instance.TransportManager.GetComponent<AudioSource>();
            Debug.Log("riding audio source: " + ridingAudioSource);

            // Clear destination for new or loaded games.
            SaveLoadManager.OnLoad += (saveData) => { destinationName = ""; };
            StartGameBehaviour.OnNewGame += () => { destinationName = ""; };
        }

        private void SetTimeScale(int timeScale)
        {
            // Must set fixed delta time to scale the fixed (physics) updates as well.
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = timeScale * baseFixedDeltaTime; // Default is 0.02 or 50/s
            //Debug.LogFormat("Set timescale= {0}, fixedDelta= {1}", timeScale, timeScale * baseFixedDeltaTime);
        }

        public void travelMapInterceptor(object sender, EventArgs e)
        {
            var manager = (UserInterfaceManager)sender;
            var window = manager.TopWindow;
            Debug.Log("top window: " + window);
            if (window != null && !travelMap.IsShowing && window.GetType() == typeof(DaggerfallTravelMapWindow))
            {

                DaggerfallTravelMapWindow originalTravelMap = window as DaggerfallTravelMapWindow;
                // check if the travel map was brought up to check for a destination for teleportation and let it proceed if yes.
                var isTeleportation = originalTravelMap.GetType().GetField("teleportationTravel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if ((bool)isTeleportation.GetValue(originalTravelMap)) return;

                // check if the travel map received a goto order from the journal link and replicate if yes
                var gotoLocation = (string)originalTravelMap.GetType().GetField("gotoLocation",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(originalTravelMap);
                if (gotoLocation != null)
                {
                    var gotoRegion = (int)originalTravelMap.GetType().GetField("gotoRegion",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(originalTravelMap);
                    travelMap.GotoLocation(gotoLocation, gotoRegion);
                }

                originalTravelMap.CloseWindow();

                // if a destination was picked, ask whether to resume or open map
                if (destinationName != "")
                {
                    DaggerfallMessageBox confirmTravelBox = new DaggerfallMessageBox(manager,
                        DaggerfallMessageBox.CommonMessageBoxButtons.YesNo,
                        "Resume travel to " + destinationName + "?",
                        manager.TopWindow);
                    confirmTravelBox.OnButtonClick += (_sender, button) =>
                    {
                        if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                        {
                            _sender.CloseWindow();
                            StartFastTravel(destinationSummary);
                        }
                        else
                        {
                            manager.PopWindow();
                            manager.PushWindow(travelMap);
                        }
                    };
                    confirmTravelBox.Show();
                }
                else
                {
                    manager.PushWindow(travelMap);
                }

            }
        }

        void Update()
        {
            if (playerAutopilot != null)
            {

                if (!travelUi.isShowing)
                {
                    DaggerfallUI.UIManager.PushWindow(travelUi);
                }

                playerAutopilot.Update();
                DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Update();

                if (GameManager.Instance.AreEnemiesNearby())
                {
                    travelUi.CloseWindow();
                    DaggerfallUI.MessageBox("Somebody is seeking to put a premature end to your journey...");
                    return;
                }
                    
            }
        }

        public void StartFastTravel(ContentReader.MapSummary destinationSummary)
        {
            DFLocation targetLocation;
            if (DaggerfallUnity.Instance.ContentReader.GetLocation(
                destinationSummary.RegionIndex, destinationSummary.MapIndex, out targetLocation))
            {
                destinationName = targetLocation.Name;
                travelUi.DestinationName = destinationName;
            }
            else throw new ArgumentException("TediousTravel destination not found!");

            playerAutopilot = new PlayerAutoPilot(destinationSummary);
            playerAutopilot.OnArrival += () =>
            {
                travelUi.CancelWindow();
                DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("You have arrived at your destination", 5f);
            };

            this.destinationSummary = destinationSummary;
            DisableAnnoyingSounds();
            SetTimeScale(travelUi.TimeCompressionSetting);
            Debug.Log("started tedious travel");
        }


        /// <summary>
        /// Stops fast travel, but leaves current destination active
        /// </summary>
        public void InterruptFastTravel()
        {
            SetTimeScale(1);
            playerAutopilot = null;
            GameManager.Instance.PlayerMouseLook.enableMouseLook = true;
            GameManager.Instance.PlayerMouseLook.lockCursor = true;
            GameManager.Instance.PlayerMouseLook.simpleCursorLock = false;
            EnableAnnoyingSounds();
        }

        /// <summary>
        /// Footsteps will drive you crazy at time acceleration.
        /// Horse hoofs are bearable but might be changed in the future not to be.
        /// And the neighing becomes just plain torture! ;)
        /// </summary>
        private void DisableAnnoyingSounds()
        {
            GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = false;
            GameManager.Instance.TransportManager.RidingVolumeScale = 0f;
        }

        private void EnableAnnoyingSounds()
        {
            GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = true;
            GameManager.Instance.TransportManager.RidingVolumeScale = 1f;
        }


        //this method will be called automatically by the modmanager after the main game scene is loaded.
        //The following requirements must be met to be invoked automatically by the ModManager during setup for this to happen:
        //1. Marked with the [Invoke] custom attribute
        //2. Be public & static class method
        //3. Take in an InitParams struct as the only parameter
        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {
            Debug.Log("main init");
            //just an example of how to add a mono-behavior to a scene.
            GameObject gObject = new GameObject("tedious");
            TediousTravel tediousTravel = gObject.AddComponent<TediousTravel>();
            
            //after finishing, set the mod's IsReady flag to true.
            ModManager.Instance.GetMod(initParams.ModTitle).IsReady = true;
        }

    }
}
