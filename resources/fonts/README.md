# 打包简体中文界面字体

`RwtSansSC-Regular.otf` 是由官方 Noto Sans CJK SC Regular 派生的简中界面子集，改名为 **RWT Sans SC**。字体随项目分发，不依赖操作系统或CI runner安装中文字体。原始字体使用 SIL Open Font License 1.1，完整许可证见 `OFL.txt`；版权、固定来源提交和校验值见 `provenance.json`，字体内也保留原版权/许可证记录。

字形来源：完整 `ui/localization/UiText.cs`（包含静态文案、所有动态角色/脸色/嘴等级/表情/状态词）、所有ASCII数字/字母/标点、全角空格、常用箭头等。准确字符集合见 `ui_glyphs.txt`。不需要完整16MB字体，当前子集只服务本项目固定文案；新增文案必须重建并跑实际Font.HasChar验证，不能依赖系统fallback掩盖缺字。

重建：安装 `fonttools==4.61.1`，从 `provenance.json` 的固定官方源下载原始OTF，然后执行：

    python resources/fonts/build_subset.py /path/to/NotoSansCJKsc-Regular.otf

脚本校验原始SHA-256后生成字体、字形集合和provenance，不主动联网、不修改原始字体。修改字体采用新family/PostScript名称并保留原license。`project.godot` 的 `gui/theme/custom_font` 统一指定该打包字体，所有页面默认简体中文，无需新增语言切换系统。
