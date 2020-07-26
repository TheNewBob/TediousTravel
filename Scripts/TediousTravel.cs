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
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

namespace TediousTravel
{
    public class TediousTravel : MonoBehaviour
    {
        private TediousTravelMap travelMap = null;
        private TediousTravelControllMenu travelUi = null;
        private InputManager inputManager = null;
        private ContentReader.MapSummary destinationSummary;
        private PlayerAutoPilot playerAutopilot = null;
        private string destinationName = null;
        private PlayerCollision playerCollision = null;
        private AudioSource ridingAudioSource;

        private float baseFixedDeltaTime;

        public TediousTravelMap TravelMap { get { return travelMap; } }

        private PlayerEntity playerEntity;
        private HUDVitals hudVitals;

        private int maxSuccessChance;

        private bool delayCombat;
        private uint delayCombatTime;
        private GameObject rain;
        private GameObject snow;
        private int diseaseCount = 0;

		public static Mod mod;

        // Native Daggerfall ship travel calculation settings
        public bool UseNativeDfShipCalculations { get; private set; }
        public bool UseInnsWhenTravelingByShip { get; private set; }

        #region Settings

        private enum AvoidEncounterChoice
        {
            NO = 0, ASK = 1, ALWAYS = 2
        };

        // These need to match the settings file section names. See modsettings.json
        private const string SHIP_TRAVEL_OPTIONS_SETTINGS_SECTION = "ShipTravelOptions";
        private const string RANDOM_ENCOUNTERS_SETTINGS_SECTION = "RandomEncounters";

        private AvoidEncounterChoice avoidEncounters;

        #endregion

        private void Start()
        {
            ModSettings settings = mod.GetSettings();

            UseNativeDfShipCalculations = settings.GetValue<bool>(SHIP_TRAVEL_OPTIONS_SETTINGS_SECTION, "EnableDaggerfallNativeShipTravel");
            UseInnsWhenTravelingByShip = settings.GetValue<bool>(SHIP_TRAVEL_OPTIONS_SETTINGS_SECTION, "UseInnsWhenTravelingByShip");

            int avoidEncounterInt = settings.GetValue<int>(RANDOM_ENCOUNTERS_SETTINGS_SECTION, "AvoidRandomEncounters");
            avoidEncounters = (AvoidEncounterChoice)Enum.ToObject(typeof(AvoidEncounterChoice), avoidEncounterInt);
            maxSuccessChance = settings.GetValue<int>(RANDOM_ENCOUNTERS_SETTINGS_SECTION, "MaxChanceToAvoidEncounter");

            hudVitals = DaggerfallUI.Instance.DaggerfallHUD.HUDVitals;
            playerEntity = GameManager.Instance.PlayerEntity;

            TediousData.Instance.LoadPortTowns(mod.PersistentDataDirectory);
            baseFixedDeltaTime = Time.fixedDeltaTime;
            DaggerfallUI.UIManager.OnWindowChange += travelMapInterceptor;
            travelMap = new TediousTravelMap(DaggerfallUI.UIManager, this);
            travelUi = new TediousTravelControllMenu(DaggerfallUI.UIManager, travelMap);
            travelUi.OnCancel += (sender) =>
            {
                destinationName = null;
            };
            travelUi.OnClose += () =>
            {
                InterruptFastTravel();
            };

            travelUi.OnTimeCompressionChanged += (newTimeCompression) => { SetTimeScale(newTimeCompression); };

            ridingAudioSource = GameManager.Instance.TransportManager.GetComponent<AudioSource>();
            Debug.Log("riding audio source: " + ridingAudioSource);

            // Clear destination for new or loaded games.
            SaveLoadManager.OnLoad += (saveData) => { destinationName = null; };
            StartGameBehaviour.OnNewGame += () => { destinationName = null; };
            GameManager.OnEncounter += GameManager_OnEncounter;

            rain = GameManager.Instance.WeatherManager.PlayerWeather.RainParticles;
            snow = GameManager.Instance.WeatherManager.PlayerWeather.SnowParticles;
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
                if (destinationName != null)
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
                if (destinationName == null)
                {
                    InterruptFastTravel();
                    return;
                }

                if (!travelUi.isShowing)
                {
                    DaggerfallUI.UIManager.PushWindow(travelUi);
                }

                playerAutopilot.Update();

                hudVitals.Update();

                // This code only comes into play if enemies are nearby without an encounter event having been fired *first*.
                // This is the case when the core spawns enemies nearby. When quests trigger encounters on the other hand, the OnEncounter event fires first and this code is never reached.
                if (GameManager.Instance.AreEnemiesNearby() && !delayCombat)
                {
                    Debug.Log("enemies nearby while fast travelling");

                    if (avoidEncounters != AvoidEncounterChoice.NO)
                    {
                        travelUi.CloseWindow();
                        AttemptAvoid();
                    }
                    else
                    {
                        travelUi.CloseWindow();
                        DaggerfallUI.MessageBox("An enemy is seeking to bring a premature end to your journey...");
                        return;
                    }
                }
                else if (delayCombat)
                {
                    if (DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime() >= delayCombatTime)
                        delayCombat = false;
                }
                var currentDiseaseCount = GameManager.Instance.PlayerEffectManager.DiseaseCount;

                // check for diseases.
                if (currentDiseaseCount != diseaseCount)
                {
                    if (currentDiseaseCount > diseaseCount)
                    {
                        Debug.Log("detected new disease, interrupting fast travel!");
                        InterruptFastTravel();
                        DaggerfallUI.Instance.CreateHealthStatusBox(DaggerfallUI.Instance.UserInterfaceManager.TopWindow).Show();
                    }
                    diseaseCount = currentDiseaseCount;
                }
            }
        }

        private void MakeAvoidAttempt(int successChance)
        {
            if (Dice100.SuccessRoll(successChance))
            {
                delayCombat = true;
                delayCombatTime = DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime() + 10;
                StartFastTravel(destinationSummary);
            }
            else
            {
                if (avoidEncounters == AvoidEncounterChoice.ALWAYS)
                    DaggerfallUI.MessageBox("You failed to avoid an encounter!");
                else
                    DaggerfallUI.MessageBox("You failed to avoid the encounter!");
            }
        }

        private void AttemptAvoid()
        {
            int playerSkillRunning = playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Running);
            int playerSkillStealth = playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Stealth);

            int successChance = Mathf.Max(playerSkillRunning, playerSkillStealth);

            successChance = successChance * maxSuccessChance / 100;

            if (avoidEncounters == AvoidEncounterChoice.ALWAYS)
                MakeAvoidAttempt(successChance);
            else
            {
                DaggerfallMessageBox mb = new DaggerfallMessageBox(DaggerfallUI.Instance.UserInterfaceManager);
                mb.SetText("You approach a hostile encounter. Attempt to avoid it?");
                mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes, true);
                mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                mb.ParentPanel.BackgroundColor = Color.clear;

                mb.OnButtonClick += (_sender, button) =>
                {
                    _sender.CloseWindow();
                    if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
                        MakeAvoidAttempt(successChance);
                };
                mb.Show();
            }
        }


        private void GameManager_OnEncounter()
        {
            if (travelUi.isShowing)
            {
                SetTimeScale(1); // Essentially redundant, but still helpful, since the close window event takes longer to trigger the time downscale.
                travelUi.CloseWindow();
                DaggerfallUI.MessageBox("You interrupt your journey.");
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
            DisableWeather();
            SetTimeScale(travelUi.TimeCompressionSetting);
            diseaseCount = GameManager.Instance.PlayerEffectManager.DiseaseCount;

            Debug.Log("started tedious travel");
        }


        /// <summary>
        /// Stops fast travel, but leaves current destination active
        /// </summary>
        public void InterruptFastTravel()
        {
            Debug.Log("fast travel interrupted");
            SetTimeScale(1);
            GameManager.Instance.PlayerMouseLook.enableMouseLook = true;
            GameManager.Instance.PlayerMouseLook.lockCursor = true;
            GameManager.Instance.PlayerMouseLook.simpleCursorLock = false;
            playerAutopilot.MouseLookAtDestination();
            playerAutopilot = null;
            EnableAnnoyingSounds();
            EnableWeather();
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

        private void DisableWeather()
        {
            var weather = GameManager.Instance.WeatherManager.PlayerWeather;
            rain.SetActive(false);
            snow.SetActive(false);
            weather.RainParticles = null;
            weather.SnowParticles = null;
        }

        private void EnableWeather()
        {
            var weather = GameManager.Instance.WeatherManager.PlayerWeather;
            weather.RainParticles = rain;
            weather.SnowParticles = snow;
            switch (weather.WeatherType)
            {
                case WeatherType.Rain:
                case WeatherType.Thunder:
                    rain.SetActive(true);
                    snow.SetActive(false);
                    break;
                case WeatherType.Snow:
                    rain.SetActive(false);
                    snow.SetActive(true);
                    break;
                default:
                    rain.SetActive(false);
                    snow.SetActive(false);
                    break;
            }
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

            mod = initParams.Mod;

            //just an example of how to add a mono-behavior to a scene.
            GameObject gObject = new GameObject("tedious");
            TediousTravel tediousTravel = gObject.AddComponent<TediousTravel>();

            //after finishing, set the mod's IsReady flag to true.
            ModManager.Instance.GetMod(initParams.ModTitle).IsReady = true;
        }

    }
}
