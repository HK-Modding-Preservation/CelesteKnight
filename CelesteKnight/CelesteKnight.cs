global using GlobalEnums;
global using HKMirror.Reflection;
global using HutongGames.PlayMaker;
global using HutongGames.PlayMaker.Actions;
global using Modding;
global using Satchel;
global using System.Reflection;
global using UnityEngine;
namespace CelesteKnight
{
    public class Settings
    {
        public bool on = true;
        public bool doubleJump = true;
        public bool shadeCloak = true;
    }
    public class CelesteKnight : Mod, IMenuMod, IGlobalSettings<Settings>
    {
        public static CelesteKnight instance;
        public bool ToggleButtonInsideMenu => true;
        public Settings settings_ = new Settings();
        private List<Module> modules = new List<Module>();
        private GameObject beam;
        private GameObject radiance;
        private bool lookingUpPreviously = false;
        public CelesteKnight() : base("CelesteKnight")
        {
            instance = this;
            modules.Add(new Input());
            modules.Add(new Dash());
            modules.Add(new Afterimage());
            modules.Add(new Momentum());
            modules.Add(new Update());
            modules.Add(new Room());
        }
        public override string GetVersion() => "0.2.0.0";
        public override List<(string, string)> GetPreloadNames()
        {
            List<(string, string)> p = new List<(string, string)>();
            foreach (var module in modules)
            {
                foreach (var name in module.GetPreloadNames())
                {
                    p.Add(name);
                }
            }
            p.Add(("GG_Radiance", "Boss Control"));
            return p;
        }
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Unity Version: " + Application.unityVersion);
            foreach (var module in modules)
            {
                module.Initialize(preloadedObjects);
            }
            SetActive(settings_.on);
            ModHooks.HeroUpdateHook += HeroUpdateHook;
            radiance = preloadedObjects["GG_Radiance"]["Boss Control"].transform.Find("Absolute Radiance").gameObject;
            var burst = radiance.transform.Find("Eye Beam Glow").gameObject.transform.Find("Burst 1").gameObject;
            beam = burst.transform.Find("Radiant Beam").gameObject;
        }
        private float TransformX(float x)
        {
            x /= 40;
            x += 60;
            return x;
        }
        private float TransformY(float y)
        {
            y = 2 * 412 - y;
            y /= 40;
            y += 37.5f;
            return y;
        }
        private void HeroUpdateHook()
        {
            if (HeroController.instance.cState.lookingUp && !lookingUpPreviously && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GG_Atrium")
            {
                var p = HeroController.instance.transform.position;
                if (p.x >= 73.4 && p.x <= 76.4 && p.y >= 40.25 && p.y <= 40.75)
                {
                    Log("IMBD!");
                    List<(string, Vector2, Vector2)> lasers = new List<(string, Vector2, Vector2)>
                {
                    ("a1", new Vector2(73, 316), new Vector2(76,517)),
                    ("b1", new Vector2(324, 323), new Vector2(316, 528)),
                    ("c1", new Vector2(579, 332), new Vector2(561, 510)),
                    ("d1", new Vector2(850, 323), new Vector2(966, 304)),
                };
                    var delay = 0.5f;
                    foreach (var l in lasers)
                    {
                        if (GameObject.Find("IMBD_" + l.Item1) == null)
                        {
                            var b = UnityEngine.Object.Instantiate(beam);
                            b.name = "IMBD_" + l.Item1;
                            Log("Created " + b.name);
                            b.SetActive(true);
                            b.SetActiveChildren(true);
                            b.transform.position = new Vector3(TransformX(l.Item2.x), TransformY(l.Item2.y), 0);
                            var dx = TransformX(l.Item3.x) - TransformX(l.Item2.x);
                            var dy = TransformY(l.Item3.y) - TransformY(l.Item2.y);
                            var angle = Mathf.Atan2(dy, dx) / Mathf.PI * 180;
                            b.transform.rotation = Quaternion.Euler(0, 0, angle);
                            var s = b.transform.localScale;
                            b.transform.localScale = new Vector3((new Vector2(dx, dy)).magnitude * 0.4f * Mathf.Sqrt(2), s.y, s.z);
                            var fsm = b.LocateMyFSM("Control");
                            fsm.AddTransition("Antic", "FINISHED", "Fire");
                            fsm.GetAction<Wait>("Antic", 2).time = delay;
                            delay += 0.5f;
                            fsm.SetState("Antic");
                            var action = radiance.LocateMyFSM("Attack Commands").GetAction<AudioPlayerOneShotSingle>("Aim", 3);
                            action.spawnPoint = b;
                            action.delay = 0;
                            fsm.AddAction("Fire", action);
                        }
                        else
                        {
                            var b = GameObject.Find("IMBD_" + l.Item1);
                            Log("Reused " + b.name);
                            b.LocateMyFSM("Control").SetState("Antic");
                        }
                    }
                }
            }
            lookingUpPreviously = HeroController.instance.cState.lookingUp;
        }
        private void SetActive(bool active)
        {
            foreach (var module in modules)
            {
                module.SetActive(active);
            }
        }
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
        {
            List<IMenuMod.MenuEntry> menus = new();
            menus.Add(
                new()
                {
                    Name = "Enabled",
                    Values = new string[]
                    {
                        Language.Language.Get("MOH_ON", "MainMenu"),
                        Language.Language.Get("MOH_OFF", "MainMenu")
                    },
                    Saver = i =>
                    {
                        settings_.on = i == 0;
                        SetActive(settings_.on);
                    },
                    Loader = () => settings_.on ? 0 : 1
                }
            );
            menus.Add(
                new()
                {
                    Name = "Double Jump",
                    Values = new string[]
                    {
                        Language.Language.Get("MOH_ON", "MainMenu"),
                        Language.Language.Get("MOH_OFF", "MainMenu")
                    },
                    Saver = i => settings_.doubleJump = i == 0,
                    Loader = () => settings_.doubleJump ? 0 : 1
                }
            );
            menus.Add(
                new()
                {
                    Name = "Shade Cloak",
                    Values = new string[]
                    {
                        Language.Language.Get("MOH_ON", "MainMenu"),
                        Language.Language.Get("MOH_OFF", "MainMenu")
                    },
                    Saver = i => settings_.shadeCloak = i == 0,
                    Loader = () => settings_.shadeCloak ? 0 : 1
                }
            );
            return menus;
        }
        public void OnLoadGlobal(Settings settings) => settings_ = settings;
        public Settings OnSaveGlobal() => settings_;
    }
}