namespace AutoTrain
{
    public static class ModStrings
    {
        public static string Get(string key)
        {
            // Lang 是 Elin 全局类，不需要引用 UnityEngine
            string lang = Lang.langCode?.ToLower() ?? "en";

            switch (key)
            {
                // [Mod 标题]
                case "ModTitle": return lang == "cn" ? "自动训练" : (lang == "jp" ? "自動訓練" : "AutoTrain");

                // --- 全局设置 ---
                case "Cfg_Global": return lang == "cn" ? "全局设置" : (lang == "jp" ? "全体設定" : "Global Settings");
                case "Cfg_ModEnabled": return lang == "cn" ? "启用 Mod" : (lang == "jp" ? "Mod有効化" : "Enable Mod");
                case "Cfg_UsePlat": return lang == "cn" ? "消耗白金币" : (lang == "jp" ? "プラチナ硬貨消費" : "Use Platinum");
                case "Cfg_TrainAmount": return lang == "cn" ? "单次训练潜力" : (lang == "jp" ? "訓練潜在能力" : "Potential Gain");
                case "Cfg_LevelUpOnly": return lang == "cn" ? "仅升级时训练" : (lang == "jp" ? "レベルアップ時のみ" : "Level Up Only");

                case "UI_Threshold": return lang == "cn" ? "阈值" : (lang == "jp" ? "閾値" : "Limit");

                // --- 官方技能分类 ---
                case "skillsCombat": return lang == "cn" ? "战斗技能" : (lang == "jp" ? "戦闘技能" : "Combat Skills");
                case "skillsCraft": return lang == "cn" ? "制造技能" : (lang == "jp" ? "製作技能" : "Craft Skills");
                case "skillsGeneral": return lang == "cn" ? "常规技能" : (lang == "jp" ? "一般技能" : "General Skills");
                case "skillsWeapon": return lang == "cn" ? "武器专精" : (lang == "jp" ? "武器の専門" : "Weapon Masteries");

                // --- Tooltip ---
                case "Tip_ModEnabled":
                    return lang == "cn" ? "功能总开关。" :
                           lang == "jp" ? "Modの機能を完全に有効/無効にします。" :
                           "Toggle the entire mod on or off.";

                case "Tip_UsePlat":
                    return lang == "cn" ? "开启后，自动训练将消耗背包中的白金币。\n如果白金币不足，将停止训练。" :
                           lang == "jp" ? "有効にすると、自動訓練でプラチナ硬貨を消費します。\n不足している場合、訓練は停止します。" :
                           "If enabled, auto-training consumes platinum coins.\nStops if you run out of coins.";

                case "Tip_TrainAmount":
                    return lang == "cn" ? "每次触发训练时，技能潜力增加的百分比数值。\n默认为 10 (即 +10%)。" :
                           lang == "jp" ? "一度の訓練で上昇する潜在能力のパーセンテージ。\nデフォルトは 10 (+10%) です。" :
                           "The percentage of potential gained per single training action.\nDefault is 10 (+10%).";

                case "Tip_SkillSwitch":
                    return lang == "cn" ? "开启或关闭此特定技能的自动训练。" :
                           lang == "jp" ? "このスキルの自動訓練を有効/無効にします。" :
                           "Enable/Disable auto-train for this specific skill.";

                case "Tip_Threshold":
                    return lang == "cn" ? "当技能潜力低于此数值时，自动触发训练。\n直到潜力恢复到此数值为止。" :
                           lang == "jp" ? "潜在能力がこの値を下回ると自動訓練を開始します。\nこの値に戻るまで訓練を続けます。" :
                           "Auto-train triggers when potential drops below this value.\nTrains until this value is reached.";

                case "Tip_LevelUpOnly":
                    return lang == "cn" ? "开启后，只有在技能等级提升时才会检查并训练潜力。\n关闭后，每次获得经验时都会检查。" :
                           lang == "jp" ? "有効にすると、スキルレベルが上昇した時のみ潜在能力をチェック・訓練します。\n無効にすると、経験値を得るたびにチェックします。" :
                           "If enabled, auto-train checks only trigger when skill level increases.";

                // --- 日志 ---
                case "Log_Train":
                    return lang == "cn" ? "AutoTrain: {0} 训练{1}次，消耗白金币{2}，潜力->{3}%。" :
                           lang == "jp" ? "AutoTrain: {0}を{1}回訓練(プラチナ{2}枚)、潜在{3}%。" :
                           "AutoTrain: Trained {0} x{1} (cost {2}), pot->{3}%.";

                case "Log_NoPlat":
                    return lang == "cn" ? "AutoTrain: 白金币不足，无法训练 {0}。" :
                           lang == "jp" ? "AutoTrain: お金が足りない ({0})。" :
                           "AutoTrain: No plat for {0}.";

                // --- Config 描述 ---
                case "Desc_ModEnabled": return "Enable mod.";
                case "Desc_UsePlat": return "Consume platinum?";
                case "Desc_TrainAmount": return "Potential per train.";
                case "Desc_SkillEnabled": return "Enable auto-train.";
                case "Desc_SkillThreshold": return "Target potential.";
                case "Desc_LevelUpOnly": return "Train only on level up.";

                default: return key;
            }
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);
    }
}