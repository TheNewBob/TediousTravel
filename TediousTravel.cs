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

namespace TediousTravel
{ 
    public class TediousTravel : MonoBehaviour
    {
        private TediousTravelMap travelMap = null;
        private TestPopup travelUi = null;
        private InputManager inputManager = null;
        private ContentReader.MapSummary destinationSummary;
        private PlayerAutoPilot playerAutopilot = null;

        private void Start()
        {
            DaggerfallUI.UIManager.OnWindowChange += travelMapInterceptor;
            travelMap = new TediousTravelMap(DaggerfallUI.UIManager, this);
            travelMap.OnClose += () => {
                travelMap.IsShowing = false;
            };
            travelMap.OnCancel += (sender) => {
                travelMap.IsShowing = false;
            };
            travelUi = new TestPopup(DaggerfallUI.UIManager);
            travelUi.OnCancel += (sender) => {
                StopFastTravel();
                travelUi.isShowing = false;
                Debug.Log("travelUi closed");
            };
            travelUi.OnClose += () => {
                StopFastTravel();
                travelUi.isShowing = false;
                Debug.Log("travelUi canceled");
            };
        }

        public void travelMapInterceptor(object sender, EventArgs e)
        {
            var manager = (UserInterfaceManager)sender;
            var window = manager.TopWindow;
            if (!travelMap.IsShowing && window.GetType() == typeof(DaggerfallTravelMapWindow))
            {
                DaggerfallTravelMapWindow originalTravelMap = window as DaggerfallTravelMapWindow;
                originalTravelMap.CloseWindow();
                manager.PushWindow(travelMap);
                travelMap.IsShowing = true;
            }
        }

        void Update()
        {
            if (playerAutopilot != null)
            {

                if (!travelUi.isShowing)
                {
                    DaggerfallUI.UIManager.PushWindow(travelUi);
                    travelUi.isShowing = true;
                }

                playerAutopilot.Update();

                if (GameManager.Instance.AreEnemiesNearby())
                {
                    travelUi.CloseWindow();
                    Debug.Log("fast travel interrupted by enemies");
                    DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("There are enemies nearby");
                    return;
                }
                    
            }
        }

        public void StartFastTravel(ContentReader.MapSummary destinationSummary)
        {
            this.destinationSummary = destinationSummary;
            GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = false;
            playerAutopilot = new PlayerAutoPilot(destinationSummary);
            playerAutopilot.OnArrival += () =>
            {
                travelUi.CloseWindow();
                DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("You have arrived at your destination", 5f);
            };
            Time.timeScale = 20;
            Debug.Log("started tedious travel");
        }

        public void StopFastTravel()
        {
            Time.timeScale = 1;
            playerAutopilot = null;
            GameManager.Instance.PlayerMouseLook.enableMouseLook = true;
            GameManager.Instance.PlayerMouseLook.lockCursor = true;
            GameManager.Instance.PlayerMouseLook.simpleCursorLock = false;
            GameManager.Instance.PlayerActivate.GetComponentInParent<PlayerFootsteps>().enabled = true;
            Debug.Log("stopped tedious travel");
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
