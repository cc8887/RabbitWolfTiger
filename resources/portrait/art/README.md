# 原图素材接入（初版）

这里的79张PNG完整复制自用户提供的原图确定性裁切包，未重绘、未补画、未清理alpha。`source_manifest.json`保留原图SHA-256、裁切坐标、分组、pivot与复合眼状态；原图1536×1024，SHA-256 `30a6c627cae3ffaa7c9c4ba19fc4905bd923384a95115399745aa0ff040840a7`。

## 选型与边界

采用Godot Sprite2D分层换件 + 单一FaceAnimator；不采用骨骼蒙皮/Spine/逐表情整头像，避免为静态组件臆造可变形拓扑或重复绘图。八个角色共用同一场景，身体/装备/后帽/耳/脸/纹/发/前帽/双眼/鼻/表情嘴为明确稳定槽。

所有开放眼都使用真实 `sclera_blank` 与独立 `pupil_brown`，每眼眼白和虹膜各有独立ShaderMaterial，源纹理共用缓存。其余11个 `eye_baked_*`/`eye_heart_baked`/`eye_shape_*` 文件仅是可选整眼姿态，本实现没有将其冒充可分层瞳孔。素材没有独立眉毛/眼皮，因此shader只裁去已有像素形成眼睑开合、左右镜像倾斜，不补画新的素材。没有把整块V形眉纹拆成两条眉。

## 动画通道

FaceAnimator是唯一眼部写入者。五种表情（neutral/happy/angry/scared/sad）设置目标开度/倾角并切换MouthPose；gaze经圆形长度限制和指数平滑独立推进；blink是0.20秒开度乘子，表情事件取消旧眨眼并抑制0.35秒，避免争抢属性。本版暂未实现头部/耳朵轻微姿态运动，表情由眼部开度/倾角、嘴姿态组合呈现。八角色使用确定性不同眨眼相位与间隔，避免同步机械眨眼。每帧delta上限0.05秒，暂停冻结计时和视线；无Timer、Tween、每帧节点/纹理分配、遗留回调。退出树清理眨眼状态，重新加入树沿用当前表情/视线继续。

瞳孔最大移动3.1头像单位，眼白缩放0.38、虹膜0.24。两层在同一眼白UV坐标做开合裁剪；瞳孔额外用眼白alpha蒙版和UV范围约束，防止极端视线/眨眼漏出。原图低alpha噪点仍保留，只对运行时眼白裁剪蒙版阈值处理，未改PNG。headscarf/helmet为自带填充的原切片；hood放脸后，脸覆盖其烘焙肤色区域。

## 玩法分离

ModularPortrait只接受DietBody的展示快照，不持有/改写DietBody。FaceExpression和MouthPose没有写入FaceColor、MouthComponent、EdibleColors、CanEat的入口。表情嘴按当前玩法嘴尺寸校准宽度；Neutral有能力时仍显示对应small/medium/large原嘴。None表示不捕食，不等于生理无嘴：使用flat/smile/frown装饰嘴，食谱保持空。画廊每张卡始终显示真正Face/Mouth/Eats，防止微笑、牙齿等视觉姿态被误读为新食性。人类四身份初始均Skin +暂定None，无身份特殊能力。

F3进入八角色展示，初始不同表情；Select指定一人，All恢复全员表情控制。换脸/换玩法嘴/换头饰保留当前表情和视线状态；F2旧规则示例保留原界面/断言契约。

## 可复用接口

`ModularPortrait.Configure(species, role, body)` 装配，`ProjectGameplay(body)`投影变化，`SetHeadPart(name)`只换头饰。`Animator.SetExpression`、`SetGaze`、`Blink`、`FollowPointer`、`AutoBlink`、`AnimationPaused`控制动画；`Advance(delta)`支持确定性验证。场景根可自由平移缩放。

自动验收位于 `tests/portrait/`；实际GPU视觉与shader渲染仍需真实非headless运行确认，headless逻辑验收不能替代截图或GPU验收。
