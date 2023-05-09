using IO = System.IO;
using Collections = System.Collections.Generic;
using PMActions = HutongGames.PlayMaker.Actions;
using UE = UnityEngine;
using MAPI = Modding;
using IC = ItemChanger;
using ICTags = ItemChanger.Tags;
using ICSpecLoc = ItemChanger.Locations.SpecialLocations;
using RC = RandomizerCore;
using RCLogic = RandomizerCore.Logic;
using RCLogicItems = RandomizerCore.LogicItems;
using RandoRC = RandomizerMod.RC;
using RandoSettings = RandomizerMod.Settings;
using RandoLogging = RandomizerMod.Logging;
using RandoMenu = RandomizerMod.Menu;
using RandoData = RandomizerMod.RandomizerData;
using MC = MenuChanger;
using MCMenuElements = MenuChanger.MenuElements;

namespace TheGloryOfBeingAFoolRandomizer
{
    public class TheGloryOfBeingAFoolRandomizer : MAPI.Mod, MAPI.IGlobalSettings<ModSettings>
    {
        public override string GetVersion() => "1.0";

        private const string Colosseum3LocationName = "Glory_of_Being_a_Fool-Colosseum";

        public override Collections.List<(string, string)> GetPreloadNames() => new()
        {
            (IC.SceneNames.Room_Colosseum_Spectate, "Crowd Audio")
        };

        public override void Initialize(Collections.Dictionary<string, Collections.Dictionary<string, UE.GameObject>> preloads)
        {
            TheGloryOfBeingAFool.Cheer = (UE.AudioClip)preloads[IC.SceneNames.Room_Colosseum_Spectate]["Crowd Audio"].LocateMyFSM("Control")
                .FsmStates.FirstOrDefault(s => s.Name == "Cheer")
                .Actions.OfType<PMActions.AudioPlayerOneShotSingle>().FirstOrDefault()
                .audioClip.Value;
            IC.Finder.DefineCustomItem(new TheGloryOfBeingAFool());
            IC.Finder.DefineCustomLocation(new ICSpecLoc.ColosseumLocation()
            {
                name = Colosseum3LocationName,
                sceneName = IC.SceneNames.Room_Colosseum_Gold,
                objectName = "Shiny Item",
                fsmParent = "Colosseum Manager",
                fsmName = "Geo Pool",
                fsmVariable = "Shiny Obj",
                flingType = IC.FlingType.Everywhere
            });

            RandoRC.RequestBuilder.OnUpdate.Subscribe(30, ApplyPreviewSetting);
            RandoRC.RequestBuilder.OnUpdate.Subscribe(50, AddGloryToPool);            
            RandoRC.RCData.RuntimeLogicOverride.Subscribe(50, HookLogic);

            RandoMenu.RandomizerMenuAPI.AddMenuPage(_ => {}, BuildConnectionMenuButton);
            // missing: RSM integration
            RandoLogging.SettingsLog.AfterLogSettings += LogRandoSettings;
        }

        private ModSettings settings = new();

        public void OnLoadGlobal(ModSettings s)
        {
            settings = s;
        }

        public ModSettings OnSaveGlobal() => settings;

        private void AddGloryToPool(RandoRC.RequestBuilder rb)
        {
            if (settings.Enabled)
            {
                rb.AddItemByName(TheGloryOfBeingAFool.Name);
                rb.AddLocationByName(Colosseum3LocationName);
            }
        }

        private void ApplyPreviewSetting(RandoRC.RequestBuilder rb)
        {
            if (settings.Enabled && !rb.gs.LongLocationSettings.ColosseumPreview)
            {
                rb.EditLocationRequest(Colosseum3LocationName, info =>
                {
                    info.onPlacementFetch += (_, _, placement) =>
                        placement.GetOrAddTag<ICTags.DisableItemPreviewTag>();
                });
            }
        }

        private void HookLogic(RandoSettings.GenerationSettings gs, RCLogic.LogicManagerBuilder lmb)
        {
            if (settings.Enabled)
            {
                var term = lmb.GetOrAddTerm(TheGloryOfBeingAFool.Name);
                lmb.AddItem(
                    new RCLogicItems.SingleItem(TheGloryOfBeingAFool.Name,
                    new(term, 1)));
                lmb.LogicLookup[Colosseum3LocationName] = lmb.LogicLookup[IC.LocationNames.Pale_Ore_Colosseum];
            }
        }

        private bool BuildConnectionMenuButton(MC.MenuPage landingPage, out MCMenuElements.SmallButton settingsButton)
        {
            var button = new MCMenuElements.SmallButton(landingPage, "T.G.O.B.A.F. Rando");

            void UpdateButtonColor()
            {
                button.Text.color = settings.Enabled ? MC.Colors.TRUE_COLOR : MC.Colors.DEFAULT_COLOR;
            }

            UpdateButtonColor();
            button.OnClick += () =>
            {
                settings.Enabled = !settings.Enabled;
                UpdateButtonColor();
            };
            settingsButton = button;
            return true;
        }

        private void LogRandoSettings(RandoLogging.LogArguments args, IO.TextWriter w)
        {
            w.WriteLine("Logging TheGloryOfBeingAFoolRandomizer settings:");
            w.WriteLine(RandoData.JsonUtil.Serialize(settings));
        }
    }
}
