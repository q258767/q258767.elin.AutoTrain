using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EvilMask.Elin.ModOptions;
using EvilMask.Elin.ModOptions.UI;

namespace AutoTrain
{
    [BepInDependency("evilmask.elinplugins.modoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("q258767.AutoTrain", "AutoTrain", "1.0.0")]
    public class AutoTrain : BaseUnityPlugin
    {
        public static AutoTrain Instance;
        public static ManualLogSource MyLog;

        public static ConfigEntry<bool> ConfigEnabled;
        public static ConfigEntry<bool> ConfigUsePlat;
        public static ConfigEntry<bool> ConfigLevelUpOnly;
        public static ConfigEntry<int> ConfigTrainAmount;

        public static Dictionary<int, ConfigEntry<bool>> SkillEnabled = new Dictionary<int, ConfigEntry<bool>>();
        public static Dictionary<int, ConfigEntry<int>> SkillThreshold = new Dictionary<int, ConfigEntry<int>>();

        public static Dictionary<string, List<int>> SkillCategories = new Dictionary<string, List<int>>();

        private void Awake()
        {
            Instance = this;
            MyLog = Logger;

            ConfigEnabled = Config.Bind("0. Global Settings", "ModEnabled", true, ModStrings.Get("Desc_ModEnabled"));
            ConfigUsePlat = Config.Bind("0. Global Settings", "ConsumePlatinum", true, ModStrings.Get("Desc_UsePlat"));
            ConfigLevelUpOnly = Config.Bind("0. Global Settings", "TrainOnLevelUpOnly", false, ModStrings.Get("Desc_LevelUpOnly"));
            ConfigTrainAmount = Config.Bind("0. Global Settings", "PotentialGainPerTrain", 10, ModStrings.Get("Desc_TrainAmount"));

            var harmony = new Harmony("q258767.AutoTrain");
            harmony.PatchAll();
        }

        private void Start()
        {
            InitSkillConfigs();
            StartCoroutine(RegisterOptions());
        }

        private IEnumerator RegisterOptions()
        {
            yield return null;
            ModOptionsBridge.Register(this);
        }

        private void InitSkillConfigs()
        {
            if (EClass.sources == null || EClass.sources.elements == null)
            {
                MyLog.LogWarning("EClass sources not ready yet.");
                return;
            }

            var skills = EClass.sources.elements.rows.Where(e => e.category == "skill" && !string.IsNullOrEmpty(e.encSlot)).ToList();

            SkillCategories.Clear();
            var orderedKeys = new string[] { "skillsCombat", "skillsWeapon", "skillsCraft", "skillsGeneral" };
            foreach (var key in orderedKeys) SkillCategories[key] = new List<int>();

            foreach (var skill in skills)
            {
                string sub = skill.categorySub;
                string targetKey = "skillsGeneral";

                if (sub == "combat") targetKey = "skillsCombat";
                else if (sub == "weapon") targetKey = "skillsWeapon";
                else if (sub == "production" || sub == "craft") targetKey = "skillsCraft";

                if (SkillCategories.ContainsKey(targetKey))
                {
                    SkillCategories[targetKey].Add(skill.id);
                }
                else
                {
                    SkillCategories["skillsGeneral"].Add(skill.id);
                }

                string configSectionName = targetKey.Replace("skills", "");
                string section = "Skill - " + configSectionName;

                string skillName = skill.alias;
                string descBase = skill.name + " (ID: " + skill.id + ") ";

                var entryEnabled = Config.Bind(section, skillName + "_Enabled", false, descBase + ModStrings.Get("Desc_SkillEnabled"));
                SkillEnabled[skill.id] = entryEnabled;

                var entryThreshold = Config.Bind(section, skillName + "_Threshold", 200, descBase + ModStrings.Get("Desc_SkillThreshold"));
                SkillThreshold[skill.id] = entryThreshold;
            }

            MyLog.LogInfo("Initialized configuration for " + SkillEnabled.Count + " skills.");
        }
    }

    // ========================================================================
    // UI Bridge
    // ========================================================================
    public static class ModOptionsBridge
    {
        public static void Register(AutoTrain plugin)
        {
            string guid = "q258767.AutoTrain";
            var controller = ModOptionController.Register(guid, "ModTitle", new object[0]);

            // ID, EN, JP, CN
            controller.SetTranslation(guid, "AutoTrain", "自動訓練", "自动训练");
            controller.SetTranslation("ModTitle", "AutoTrain", "自動訓練", "自动训练");

            controller.OnBuildUI += delegate (OptionUIBuilder builder)
            {
                var root = builder.Root;

                if (root.Base != null)
                {
                    root.Base.spacing = 4;
                }

                // --- 1. 全局设置 ---
                var groupGlobal = root.AddVLayoutWithBorder(ModStrings.Get("Cfg_Global"));

                var tEnabled = groupGlobal.AddToggle(ModStrings.Get("Cfg_ModEnabled"), AutoTrain.ConfigEnabled.Value);
                tEnabled.OnValueChanged += delegate (bool v) { AutoTrain.ConfigEnabled.Value = v; };
                UIHelper.SetTooltip(tEnabled, ModStrings.Get("Tip_ModEnabled"));

                var tUsePlat = groupGlobal.AddToggle(ModStrings.Get("Cfg_UsePlat"), AutoTrain.ConfigUsePlat.Value);
                tUsePlat.OnValueChanged += delegate (bool v) { AutoTrain.ConfigUsePlat.Value = v; };
                UIHelper.SetTooltip(tUsePlat, ModStrings.Get("Tip_UsePlat"));

                var tLevelUp = groupGlobal.AddToggle(ModStrings.Get("Cfg_LevelUpOnly"), AutoTrain.ConfigLevelUpOnly.Value);
                tLevelUp.OnValueChanged += delegate (bool v) { AutoTrain.ConfigLevelUpOnly.Value = v; };
                UIHelper.SetTooltip(tLevelUp, ModStrings.Get("Tip_LevelUpOnly"));

                var lineAmount = groupGlobal.AddHLayout();
                if (lineAmount.Base != null)
                {
                    lineAmount.Base.childAlignment = TextAnchor.MiddleLeft;
                    lineAmount.Base.childForceExpandHeight = false;
                }

                var lblAmount = lineAmount.AddText(ModStrings.Get("Cfg_TrainAmount"), TextAnchor.MiddleLeft);
                UIHelper.SetTooltip(lblAmount, ModStrings.Get("Tip_TrainAmount"));

                var layoutLblAmount = lblAmount.Base.GetComponent<LayoutElement>() ?? lblAmount.Base.gameObject.AddComponent<LayoutElement>();
                layoutLblAmount.minWidth = 200;
                layoutLblAmount.flexibleWidth = 0;

                var inputAmount = lineAmount.AddInput(
                    value: AutoTrain.ConfigTrainAmount.Value.ToString(),
                    validation: InputField.CharacterValidation.Integer
                );
                UIHelper.FixInput(inputAmount, 60f);
                UIHelper.SetTooltip(inputAmount, ModStrings.Get("Tip_TrainAmount"));

                inputAmount.OnValueChanged += delegate (string v) {
                    if (int.TryParse(v, out int val)) AutoTrain.ConfigTrainAmount.Value = val;
                };

                // --- 2. 技能设置 ---
                foreach (var category in AutoTrain.SkillCategories)
                {
                    string catKey = category.Key;
                    List<int> skillIds = category.Value;
                    if (skillIds.Count == 0) continue;

                    var groupCat = root.AddVLayoutWithBorder(ModStrings.Get(catKey));

                    foreach (int skillId in skillIds)
                    {
                        if (!AutoTrain.SkillEnabled.TryGetValue(skillId, out var cfgEnabled) ||
                            !AutoTrain.SkillThreshold.TryGetValue(skillId, out var cfgThreshold))
                        {
                            continue;
                        }

                        var source = EClass.sources.elements.map[skillId];
                        string skillLabel = source.GetName();

                        var lineSkill = groupCat.AddHLayout();
                        if (lineSkill.Base != null)
                        {
                            lineSkill.Base.childAlignment = TextAnchor.MiddleLeft;
                            lineSkill.Base.childForceExpandHeight = false;
                            lineSkill.Base.padding = new RectOffset(0, 10, 0, 0);
                        }

                        var tSkill = lineSkill.AddToggle(skillLabel, cfgEnabled.Value);
                        tSkill.OnValueChanged += delegate (bool v) { cfgEnabled.Value = v; };

                        try
                        {
                            var layout = tSkill.Base.GetComponent<LayoutElement>() ?? tSkill.Base.gameObject.AddComponent<LayoutElement>();
                            layout.flexibleWidth = 1f;
                            layout.minWidth = -1;
                        }
                        catch { }

                        UIHelper.ShiftToggleVisuals(tSkill, 0f, -2f);

                        var lblTh = lineSkill.AddText(ModStrings.Get("UI_Threshold") + ":", TextAnchor.MiddleRight, 12, Color.gray);
                        var layoutLbl = lblTh.Base.GetComponent<LayoutElement>() ?? lblTh.Base.gameObject.AddComponent<LayoutElement>();
                        layoutLbl.minWidth = 60;
                        UIHelper.SetTooltip(lblTh, ModStrings.Get("Tip_Threshold"));

                        var inputTh = lineSkill.AddInput(
                            value: cfgThreshold.Value.ToString(),
                            validation: InputField.CharacterValidation.Integer
                        );
                        UIHelper.FixInput(inputTh, 50f);
                        UIHelper.SetTooltip(inputTh, ModStrings.Get("Tip_Threshold"));

                        inputTh.OnValueChanged += delegate (string v) {
                            if (int.TryParse(v, out int val))
                            {
                                if (val > 1000)
                                {
                                    val = 1000;
                                    if (inputTh.Base != null) inputTh.Base.text = "1000";
                                }
                                cfgThreshold.Value = val;
                            }
                        };
                    }
                }

                var spacer = root.AddText("");
                var leSpacer = spacer.Base.gameObject.AddComponent<LayoutElement>();
                leSpacer.minHeight = 150;
            };
        }
    }

    // ========================================================================
    // UI Helper
    // ========================================================================
    public static class UIHelper
    {
        public static void FixInput(OptInput input, float width)
        {
            if (input == null || input.Base == null) return;
            try
            {
                var layout = input.Base.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.flexibleWidth = 0;
                    layout.minWidth = width;
                    layout.preferredWidth = width;
                }
                input.PrefferedWidth = (int)width;
            }
            catch { }
        }

        public static void ShiftToggleVisuals(OptToggle toggle, float x, float y)
        {
            if (toggle == null || toggle.Base == null) return;
            try
            {
                var t = toggle.Base.GetComponent<UnityEngine.UI.Toggle>();
                if (t != null)
                {
                    if (t.targetGraphic != null) t.targetGraphic.rectTransform.anchoredPosition += new Vector2(x, y);
                    if (t.graphic != null) t.graphic.rectTransform.anchoredPosition += new Vector2(x, y);
                }
            }
            catch { }
        }

        public static void SetTooltip(object uiElement, string text)
        {
            if (uiElement == null) return;
            GameObject go = null;
            if (uiElement is OptToggle t) go = t.Base?.gameObject;
            else if (uiElement is OptInput i) go = i.Base?.gameObject;
            else if (uiElement is OptLabel l) go = l.Base?.gameObject;
            else if (uiElement is GameObject g) go = g;

            if (go != null)
            {
                var old = go.GetComponent<SimpleTooltipTrigger>();
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
                var trigger = go.AddComponent<SimpleTooltipTrigger>();
                trigger.text = text;
            }
        }
    }

    public class SimpleTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string text;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!string.IsNullOrEmpty(text))
            {
                TooltipData data = new TooltipData();
                data.text = text;
                TooltipManager.Instance.ShowTooltip(data, this.transform);
            }
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipManager.Instance.HideTooltips();
        }
        private void OnDisable()
        {
            if (TooltipManager.Instance != null)
                TooltipManager.Instance.HideTooltips();
        }
    }

    // ========================================================================
    // Harmony Patch
    // ========================================================================
    [HarmonyPatch(typeof(ElementContainer), "ModExp")]
    public class Patch_ElementContainer_ModExp
    {
        static void Prefix(ElementContainer __instance, int ele, out int __state)
        {
            __state = -1;
            if (!AutoTrain.ConfigEnabled.Value) return;
            if (__instance.Card == null || !__instance.Card.IsPC) return;

            Element element = __instance.GetElement(ele);
            if (element != null)
            {
                __state = element.Value;
            }
        }

        static void Postfix(ElementContainer __instance, int ele, int __state)
        {
            if (!AutoTrain.ConfigEnabled.Value) return;
            if (__instance.Card == null || !__instance.Card.IsPC) return;

            Element element = __instance.GetElement(ele);
            if (element == null) return;

            if (!AutoTrain.SkillEnabled.TryGetValue(ele, out var configEnabled) || !configEnabled.Value)
            {
                return;
            }

            if (AutoTrain.ConfigLevelUpOnly.Value)
            {
                if (__state == -1 || element.Value <= __state)
                {
                    return;
                }
            }

            if (!AutoTrain.SkillThreshold.TryGetValue(ele, out var configThreshold))
            {
                return;
            }
            int threshold = configThreshold.Value;

            if (element.vTempPotential >= threshold)
            {
                return;
            }

            TrainSkill(__instance.Card.Chara, element, threshold);
        }

        private static void TrainSkill(Chara chara, Element element, int threshold)
        {
            int trainCount = 0;
            int totalCost = 0;
            int gainPerTrain = AutoTrain.ConfigTrainAmount.Value;
            bool usePlat = AutoTrain.ConfigUsePlat.Value;

            Thing platThing = chara.things.Find("plat");
            int currentPlat = platThing != null ? platThing.Num : 0;

            int safetyBreak = 100;

            while (element.vTempPotential < threshold && safetyBreak > 0)
            {
                if (element.vTempPotential + gainPerTrain > 1000)
                {
                    break;
                }

                int cost = CalculateCost(chara, element);

                if (usePlat)
                {
                    if (currentPlat < cost)
                    {
                        EClass.pc.Say(ModStrings.Format("Log_NoPlat", element.source.GetName()));
                        break;
                    }

                    chara.things.AddCurrency(chara, "plat", -cost);

                    currentPlat -= cost;
                    totalCost += cost;
                }

                element.vTempPotential += gainPerTrain;
                if (element.vTempPotential > 1000) element.vTempPotential = 1000;

                trainCount++;
                safetyBreak--;
            }

            if (trainCount > 0)
            {
                EClass.pc.Say(ModStrings.Format("Log_Train", element.source.GetName(), trainCount, totalCost, element.vTempPotential));
            }
        }

        private static int CalculateCost(Chara c, Element e)
        {
            int cost = Mathf.Max(1, e.CostTrain * (c.HasElement(1202) ? 80 : 100) / 100);

            string[] tags = e.source.tag;

            Faction branch = null;
            if (tags.Contains("fighter")) branch = EClass.game.factions.Fighter;
            else if (tags.Contains("mage")) branch = EClass.game.factions.Mage;
            else if (tags.Contains("thief")) branch = EClass.game.factions.Thief;
            else if (tags.Contains("merchant")) branch = EClass.game.factions.Merchant;

            if (branch != null)
            {
                if (branch.relation.rank < 2)
                {
                    cost *= 2;
                }
            }

            return cost;
        }
    }
}