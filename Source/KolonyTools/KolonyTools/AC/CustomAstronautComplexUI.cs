using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

//Borrowed from The Read Panda's excellent mod, TRP-Hire, with permission.

namespace KolonyTools.AC
{
    class CustomAstronautComplexUI : MonoBehaviour
    {
        private Rect _areaRect = new Rect(-500f, -500f, 200f, 200f);
        private Vector2 _guiScalar = Vector2.one;
        private Vector2 _guiPivot = Vector2.zero;
        private float KBulk = 1;
        private int KBulki = 1;
        private int crewWeCanHire = 10;
        private static float KStupidity = 50;
        private static float KCourage = 50;
        private static bool KFearless = false;
        private static int KCareer = 0;
        private List<Kolonist> _kolonists; private static int KLevel = 0;
        private float Krep = Reputation.CurrentRep;
        private string[] KLevelStringsZero = new string[1] { "Level 0" };
        private string[] KLevelStringsOne = new string[2] { "Level 0", "Level 1" };
        private string[] KLevelStringsTwo = new string[3] { "Level 0", "Level 1", "Level 2" };
        private string[] KLevelStringsAll = new string[6] { "Level 0", "Level 1", "Level 2", "Level 3", "Level 4", "Level 5" };
        private static int KGender = 0;
        private GUIContent KMale = new GUIContent("Male", AssetBase.GetTexture("kerbalicon_recruit"));
        private GUIContent KFemale = new GUIContent("Female", AssetBase.GetTexture("kerbalicon_recruit_female"));
        private GUIContent KGRandom = new GUIContent("Random", "When this option is selected the kerbal might be male or female");
        Color basecolor = GUI.color;
        private float ACLevel = 0;
        private double KDead;
        private double DCost = 1;
        KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
        private bool hTest = true;
        private bool hasKredits = true;
        private bool kerExp = HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>().KerbalExperienceEnabled(HighLogic.CurrentGame.Mode);
        private static string RecruitLevel = "RecruitementLevel";

        [KSPAddon(KSPAddon.Startup.Instantly, true)]
        public class StaticLoader : MonoBehaviour
        {
            public StaticLoader()
            {
                Debug.Log("InitStaticData");
                for (int level = 1; level <= 5; level++)
                {
                    var expValue = GetExperienceNeededFor(level);
                    KerbalRoster.AddExperienceType(RecruitLevel + level, "Recruited at level " + level + " on", 0.0f, expValue);
                }
            }
        }

        private void Awake()
        {
            _kolonists = new List<Kolonist>();
            _kolonists.Add(new Kolonist { Name = "Pilot", isBase = true, Effects = "Autopilot, VesselControl, RepBoost, Logistics, Explorer" });
            _kolonists.Add(new Kolonist { Name = "Scientist", isBase = true, Effects = "Science, Experiment, Botany, Agronomy, Medical, ScienceBoost" });
            _kolonists.Add(new Kolonist { Name = "Engineer", isBase = true, Effects = "Repair, Converter, Drill, Geology, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Kolonist", isBase = true, Effects = "RepBoost, FundsBoost, ScienceBoost" });
            _kolonists.Add(new Kolonist { Name = "Miner", isBase = true, Effects = "Drill, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Technician", isBase = true, Effects = "Converter, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Mechanic", isBase = true, Effects = "Repair, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Biologist", isBase = true, Effects = "Biology, ScienceBoost" });
            _kolonists.Add(new Kolonist { Name = "Geologist", isBase = true, Effects = "Geology, FundsBoost" });
            _kolonists.Add(new Kolonist { Name = "Farmer", isBase = true, Effects = "Agronomy, ScienceBoost, RepBoost" });
            _kolonists.Add(new Kolonist { Name = "Medic", isBase = true, Effects = "Medical, ScienceBoost, RepBoost" });
            _kolonists.Add(new Kolonist { Name = "Quartermaster", isBase = true, Effects = "Logistics, RepBoost" });
            _kolonists.Add(new Kolonist { Name = "Scout", isBase = true, Effects = "Explorer" });
        }

        public void Initialize(Rect guiRect)
        {
            var uiScaleMultiplier = GameSettings.UI_SCALE;

            // the supplied rect will have the UI scalar already factored in
            //
            // to respect the player's UI scale wishes, work out what the unscaled rect
            // would be. Then we'll apply the scale again in OnGUI so all of our GUILayout elements
            // will respect the multiplier
            var correctedRect = new Rect(guiRect.x, guiRect.y, guiRect.width / uiScaleMultiplier,
                guiRect.height / uiScaleMultiplier);

            _areaRect = correctedRect;

            _guiPivot = new Vector2(_areaRect.x, _areaRect.y);
            _guiScalar = new Vector2(GameSettings.UI_SCALE, GameSettings.UI_SCALE);

            enabled = true;
        }

        private void kHire()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                double myFunds = Funding.Instance.Funds;
                Funding.Instance.AddFunds(-costMath(), TransactionReasons.CrewRecruited);
                Debug.Log("KSI :: Total Funds removed " + costMath());
            }

            for (int i = 0; i < KBulki; i++)
            {
                ProtoCrewMember newKerb = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Crew);

                switch (KGender) // Sets gender
                {
                    case 0: newKerb.gender = ProtoCrewMember.Gender.Male; break;
                    case 1: newKerb.gender = ProtoCrewMember.Gender.Female; break;
                    case 2: break;
                    default: break;
                }
                string career = "";
                switch (KCareer) // Sets career
                {
                    case 0: career = "Pilot"; break;
                    case 1: career = "Scientist"; break;
                    case 2: career = "Engineer"; break;
                    case 3: career = "Kolonist"; break;
                    case 4: career = "Scout"; break;
                    case 5: career = "Kolonist"; break;
                    case 6: career = "Miner"; break;
                    case 7: career = "Technician"; break;
                    case 8: career = "Mechanic"; break;
                    case 9: career = "Biologist"; break;
                    case 10: career = "Geologist"; break;
                    case 11: career = "Farmer"; break;
                    case 12: career = "Medic"; break;
                    case 13: career = "Quartermaster"; break;
                }
                // Sets the kerbal's career based on the KCareer switch.
                KerbalRoster.SetExperienceTrait(newKerb, career);

                // Debug.Log("KSI :: KIA MIA Stat is: " + KDead);
                // Debug.Log("KSI :: " + newKerb.experienceTrait.TypeName + " " + newKerb.name + " has been created in: " + loopcount.ToString() + " loops.");
                newKerb.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                newKerb.experience = 0;


                if (KolonyACOptions.CustomKerbonautsEnabled)
                {
                    newKerb.experienceLevel = 0;
                    newKerb.courage = KCourage/100;
                    newKerb.stupidity = KStupidity/100;
                    if (KFearless)
                    {
                        newKerb.isBadass = true;
                    }
                }

                // Debug.Log("PSH :: Status set to Available, courage and stupidity set, fearless trait set.");
                if (KLevel > 0)
                {
                    var logName = RecruitLevel + KLevel;
                    var homeworldName = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).FirstOrDefault().name;
                    newKerb.flightLog.AddEntry(logName, homeworldName);
                    newKerb.ArchiveFlightLog();
                    newKerb.experience = GetExperienceNeededFor(KLevel);
                    newKerb.experienceLevel = KLevel;
                }
                if (ACLevel == 5 || kerExp == false)
                {
                    newKerb.experience = 9999;
                    newKerb.experienceLevel = 5;
                    Debug.Log("KSI :: Level set to 5 - Non-Career Mode default.");
                }
            }

            // Refreshes the AC so that new kerbal shows on the available roster.
            Debug.Log("PSH :: Hiring Function Completed.");
            GameEvents.onGUIAstronautComplexDespawn.Fire();
            GameEvents.onGUIAstronautComplexSpawn.Fire();

        }


        private int costMath()
        {
            int active = HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount();
            var defaultCost = (int)GameVariables.Instance.GetRecruitHireCost(active);

            if (KolonyACOptions.CostCapEnabled)
                defaultCost = Math.Min(defaultCost, KolonyACOptions.GetMaxCost);

            if (_kolonists[KCareer].isBase && !KolonyACOptions.AlternateCoreCostEnabled)
                return defaultCost;
            if (!_kolonists[KCareer].isBase && !KolonyACOptions.AlternateKolonistCostEnabled)
                return defaultCost;

            dCheck();
            float fearlessCost = 0;
            float basecost = KolonyACOptions.GetKolonistCost;
            if (_kolonists[KCareer].isBase)
                basecost = KolonyACOptions.GetCoreCost;

            float couragecost = KCourage * 150;
            float stupidRebate = KStupidity * 150;
            float diffcost = HighLogic.CurrentGame.Parameters.Career.FundsLossMultiplier;
            if (KFearless == true)
            {
                fearlessCost += 10000;
            }
            DCost = 1 + (KDead * 0.1f);

            double minCost = 0.1d * basecost;
            double attribsCost = Math.Max(minCost, basecost + couragecost - stupidRebate);

            double currentcost = (attribsCost + fearlessCost) * (KLevel + 1) * DCost * diffcost * KBulki;
            int finalcost = Convert.ToInt32(currentcost); 

            if (KolonyACOptions.CostCapEnabled)
                finalcost = Math.Min(finalcost, KolonyACOptions.GetMaxCost);

            return finalcost;
        }

        // these slightly reduce garbage created by avoiding array allocations which is one reason OnGUI
        // is so terrible
        private static readonly GUILayoutOption[] DefaultLayoutOptions = new GUILayoutOption[0];
        private static readonly GUILayoutOption[] PortraitOptions = { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false) };
        private static readonly GUILayoutOption[] FixedWidth = { GUILayout.Width(220f) };
        private static readonly GUILayoutOption[] HireButtonOptions = { GUILayout.ExpandHeight(true), GUILayout.MaxHeight(40f), GUILayout.MinWidth(40f) };
        private static readonly GUILayoutOption[] StatOptions = { GUILayout.MaxWidth(100f) };
        private static readonly GUILayoutOption[] FlavorTextOptions = { GUILayout.MaxWidth(200f) };

        private string hireStatus()
        {

            string bText = "Hire Applicant";
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                double kredits = Funding.Instance.Funds;
                if (costMath() > kredits)
                {
                    bText = "Not Enough Funds!";
                    hTest = false;
                }
                if (HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount() >= GameVariables.Instance.GetActiveCrewLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)))
                {
                    bText = "Roster is Full!";
                    hTest = false;
                }
                else
                {
                    hTest = true;
                }
            }
            return bText;
        }

        private int cbulktest()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                crewWeCanHire = Mathf.Clamp(GameVariables.Instance.GetActiveCrewLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)) - HighLogic.CurrentGame.CrewRoster.GetActiveCrewCount(), 0, 10);
            }
            return crewWeCanHire;
        }

        private void dCheck()
        {
            KDead = 0;
            // 10 percent for dead and 5 percent for missing, note can only have dead in some career modes.
            foreach (ProtoCrewMember kerbal in roster.Crew)
            {
                if (kerbal.rosterStatus.ToString() == "Dead")
                {
                    if (kerbal.experienceTrait.Title == _kolonists[KCareer].Name)
                    {
                        KDead += 1;
                    }
                }
                if (kerbal.rosterStatus.ToString() == "Missing")
                {
                    if (kerbal.experienceTrait.Title == _kolonists[KCareer].Name)
                    {
                        KDead += 0.5;
                    }
                }
            }
        }

        private void OnGUI()
        {

            GUI.skin = HighLogic.Skin;
            var roster = HighLogic.CurrentGame.CrewRoster;
            GUIContent[] KGendArray = new GUIContent[3] { KGRandom, KMale, KFemale };
            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
            {
                hasKredits = false;
                ACLevel = 5;
            }
            if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                hasKredits = false;
                ACLevel = 5;
            }
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                hasKredits = true;
                ACLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex);
            }

            GUILayout.BeginArea(_areaRect);
            {
                GUILayout.Label("Recruit new Kerbalnaut"); // Testing Renaming Label Works

                // Gender selection 
                GUILayout.BeginHorizontal("box");
                KGender = GUILayout.Toolbar(KGender, KGendArray);
                GUILayout.EndHorizontal();

                // Career selection
                GUILayout.BeginVertical("box");
                var kArray = _kolonists.Select(k => k.Name).Take(3).ToArray();
                if (KolonyACOptions.KolonistHiringEnabled)
                {
                    kArray = _kolonists.Select(k => k.Name).ToArray();
                }
                KCareer = GUILayout.SelectionGrid(KCareer,kArray, 4);

                // Adding a section for 'number/bulk hire' here using the int array kBulk 
                if (cbulktest() < 1)
                {
                    GUILayout.Label("Bulk hire Option: You can not hire any more kerbals at this time!");
                }
                else
                {
                    GUILayout.Label("Bulk hire Selector: " + KBulki);
                    KBulk = GUILayout.HorizontalSlider(KBulk, 1, cbulktest());
                    KBulki = Convert.ToInt32(KBulk);

                }

                GUI.contentColor = basecolor;
                GUILayout.EndVertical();

                if (KolonyACOptions.CustomKerbonautsEnabled)
                {
                    // Courage Brains and BadS flag selections
                    GUILayout.BeginVertical("box");
                    GUILayout.Label("Courage:  " + Math.Truncate(KCourage));
                    KCourage = GUILayout.HorizontalSlider(KCourage, 0, 100);
                    GUILayout.Label("Stupidity:  " + Math.Truncate(KStupidity));
                    KStupidity = GUILayout.HorizontalSlider(KStupidity, 0, 100);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Is this Kerbal Fearless?");
                    KFearless = GUILayout.Toggle(KFearless, "Fearless");
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

                    // Level selection
                    GUILayout.BeginVertical("box");
                    GUILayout.Label("Select Your Level:");

                    // If statements for level options
                    if (kerExp == false)
                    {
                        GUILayout.Label("Level 5 - Mandatory for Career with no EXP enabled.");
                    }
                    else
                    {
                        if (ACLevel == 0)
                        {
                            KLevel = GUILayout.Toolbar(KLevel, KLevelStringsZero);
                        }
                        if (ACLevel == 0.5)
                        {
                            KLevel = GUILayout.Toolbar(KLevel, KLevelStringsOne);
                        }
                        if (ACLevel == 1)
                        {
                            KLevel = GUILayout.Toolbar(KLevel, KLevelStringsTwo);
                        }
                        if (ACLevel == 5)
                        {
                            GUILayout.Label("Level 5 - Mandatory for Sandbox or Science Mode.");
                        }
                    }
                    GUILayout.EndVertical();
                }

                if (hasKredits == true)
                {
                    GUILayout.BeginHorizontal("window");
                    GUILayout.BeginVertical();
                    //GUILayout.FlexibleSpace();
                    if (costMath() <= Funding.Instance.Funds)
                    {
                        GUILayout.Label("Cost: " + costMath(), HighLogic.Skin.textField);
                    }
                    else
                    {
                        GUI.color = Color.red;
                        GUILayout.Label("Insufficient Funds - Cost: " + costMath(), HighLogic.Skin.textField);
                        GUI.color = basecolor;
                    }
                    // GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (hTest)
                {
                    if (GUILayout.Button(hireStatus(), GUILayout.Width(200f)))
                        kHire();
                }
                if (!hTest)
                {
                    GUILayout.Button(hireStatus(), GUILayout.Width(200f));
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndArea();
            }
        }
        void Update()
        {
            AstronautComplex ac = GameObject.FindObjectOfType<AstronautComplex>();
            if (ac != null)
            {
                if (ac.ScrollListApplicants.Count > 0)
                {
                    Debug.Log("TRP: Clearing Applicant List");
                    ac.ScrollListApplicants.Clear(true);
                }
            }
        }

        public static float GetExperienceNeededFor(int level)
        {
            switch (level)
            {
                case 0:
                    return 0;
                case 1:
                    return 2;
                case 2:
                    return 8;
                case 3:
                    return 16;
                case 4:
                    return 32;
                case 5:
                    return 64;
                default:
                    return 0;
            }
        }

    }

}