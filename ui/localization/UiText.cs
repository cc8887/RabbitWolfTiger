using System;
using System.Collections.Generic;
using System.Linq;
using RabbitWolfTiger.Entities.Diet;
using RabbitWolfTiger.Entities.Portrait;

namespace RabbitWolfTiger.Ui.Localization;

/// <summary>Default Simplified Chinese presentation only. Never use translated values as rule or node identifiers.</summary>
public static class UiText
{
    public const string Locale = "zh_CN";
    public const string FontPath = "res://resources/fonts/RwtSansSC-Regular.otf";
    public static readonly IReadOnlyDictionary<string, string> Strings = new Dictionary<string, string>
    {
        ["GameTitle"] = "兔狼虎",
        ["GalleryTitle"] = "旅客头像与表情",
        ["GallerySubtitle"] = "原图分层素材＋新增矢量组件 · 八位旅客 · 独立视线与眨眼",
        ["RulesNav"] = "食性规则 / F2",
        ["LegacyNav"] = "旧版移动测试 / F4",
        ["ReferenceNav"] = "原图对照 / F5",
        ["Blink"] = "眨眼", ["Pause"] = "暂停 / 继续", ["Gaze"] = "视线：跟随 / 回正",
        ["Select"] = "选择", ["SwapFaces"] = "换脸", ["SwapMouths"] = "换嘴", ["SwapHeadwear"] = "换头饰",
        ["All"] = "全体", ["Paused"] = "已暂停", ["Live"] = "动画播放中",
        ["GalleryFooter"] = "表情只改变外观，不改变食性；每卡显示真实嘴等级与可食颜色。人类食谱暂定，身份能力尚未实现。",
        ["ReferenceTitle"] = "原图对照 · 中性表情 / 正视 / 暂停",
        ["ReferenceSubtitle"] = "统一可见脸宽为96像素。左侧为原图示例切片，右侧为实时分层头像。",
        ["GalleryBack"] = "旅客首页 / F5", ["Reference"] = "原图示例", ["Modular"] = "分层头像",
        ["RefugeeReference"] = "普通人基底参考；头巾为身份变体",
        ["ReferenceCardNote"] = "原图示例与独立组件对照",
        ["ReferenceFooter"] = "新增组件：黑瞳、白口鼻垫、中性嘴与虎颊纹。人类食谱仍为暂定。",
        ["DietTitle"] = "脸色与嘴等级 · 食性组合测试",
        ["DietLegend"] = "小嘴＝1种颜色　中嘴＝2种颜色　大嘴＝3种颜色　不捕食＝0种",
        ["DietInstructions"] = "使用原图组件；眼白与瞳孔独立。点击换嘴、换脸，检查组合后的食性。",
        ["ResetParts"] = "重置组件", ["DietBack"] = "返回旅客首页 / F2",
        ["DietFooter"] = "难民、刺客、国王、护卫均使用肤色；当前暂定不捕食，尚未实现身份能力。",
        ["LegacyTitle"] = "旧版移动测试场景",
        ["LegacyHint"] = "仅用于旧版移动测试　｜　F2：食性规则　｜　F3：返回旅客首页",
        ["LegacyRules"] = "食性规则 / F2", ["LegacyHome"] = "旅客首页 / F3",
        ["None"] = "无", ["NonPredatory"] = "无（不捕食）"
    };
    public static string T(string key) => Strings[key];
    public static string Character(string id) => id switch
    {
        "Human" => "人类", "Rabbit" => "兔子", "Wolf" => "狼", "Tiger" => "老虎", "Carrot" => "胡萝卜",
        "Refugee" => "难民", "Assassin" => "刺客", "King" => "国王", "Guard" => "护卫",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown character display key.")
    };
    public static string Expression(FaceExpression value) => value switch
    {
        FaceExpression.Neutral => "中性", FaceExpression.Happy => "开心", FaceExpression.Angry => "生气",
        FaceExpression.Scared => "害怕", FaceExpression.Sad => "难过", _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
    public static string Face(FaceColor value) => value switch
    {
        FaceColor.Cream => "奶白", FaceColor.Gray => "灰色", FaceColor.Orange => "橙色",
        FaceColor.GoldenOrange => "橙黄", FaceColor.Skin => "肤色", _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
    public static string Mouth(MouthSize value) => value switch
    {
        MouthSize.None => "不捕食", MouthSize.Small => "小嘴", MouthSize.Medium => "中嘴", MouthSize.Large => "大嘴",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
    public static string Recipe(IEnumerable<FaceColor> colors)
    {
        string[] names = colors.Select(Face).ToArray();
        return names.Length == 0 ? T("NonPredatory") : string.Join("、", names);
    }
    public static string GalleryDetails(DietBody body) => $"脸色：{Face(body.Face.Color)}　嘴：{Mouth(body.Mouth.Size)}\n可食：{Recipe(body.Mouth.EdibleColors)}";
    public static string GalleryState(string? selectedId, bool paused) => $"当前对象：{(selectedId == null ? T("All") : Character(selectedId))} · {T(paused ? "Paused" : "Live")} · 可切换鼠标视线";
    public static string DietDetails(DietBody body, IEnumerable<string> targetIds)
    {
        string[] targets = targetIds.Select(Character).ToArray();
        return $"脸色：{Face(body.Face.Color)}\n嘴等级：{Mouth(body.Mouth.Size)}\n可食颜色：\n{Recipe(body.Mouth.EdibleColors)}\n可捕食：{(targets.Length == 0 ? T("None") : string.Join("、", targets))}";
    }
    public static string Health(int current, int maximum) => $"生命值 {current} / {maximum}";
}
