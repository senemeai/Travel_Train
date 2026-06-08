  火车风景与 AI NPC 交互系统

   项目概述

一个基于 Unity 的 2D/2.5D 沉浸式体验项目。玩家身处一列行驶中的火车，可以：
- 在车厢内透过车窗观看  动态视差风景  （多层背景无缝滚动）
- 选择  下车  进入外部环境，近距离观赏风景
- 与车厢内的  AI NPC  进行自由对话，每位 NPC 均接入大语言模型 API，具备独立人格与形象
- 系统实时追踪玩家与 NPC 对话中的  情感变化  ，影响 NPC 回应风格与剧情走向

---

   当前已实现功能

| 模块 | 状态 | 说明 |
|---|---|---|
|   视差风景系统   | ✅ 已完成 | 多场景组无缝循环拼接，支持远景/中景/近景/天空/地面等任意层数，速度完全由 Inspector 控制 |
|   火车内角色移动   | ✅ 已完成 | 带边界约束的左右移动，自动翻转，动画支持 |
|   相机跟随   | ✅ 已完成 | 平滑跟随玩家，限制在火车框架内，不越界 |
|   场景切换（下车/上车）   | 🚧 待实现 | 火车内部 ↔ 外部环境的场景切换机制 |
|   AI NPC 对话   | 🚧 待实现 | 接入 LLM API，支持自由输入与多轮对话 |
|   情感追踪系统   | 🚧 待实现 | 对话情感分析、NPC 关系度、动态回应调整 |

---

   技术架构

-   引擎  ：Unity 2022.3 LTS+
-   渲染  ：2D Sprite + 视差滚动（Parallax Scrolling）
-   AI 层  ：LLM API 统一接入层（支持 OpenAI / Claude / 国产大模型等）
-   数据  ：NPC 配置 ScriptableObject、对话历史持久化、情感图谱
---

   核心系统详解

    1. 视差风景系统（SceneryManager）

  设计目标  ：火车窗外的风景必须永远不留白，且不同层速度不同，形成真实视差。

  关键特性  ：
-   LayerType 分层  ：`Background` / `Midground` / `Foreground` / `NearForeground` / `Sky` / `Ground`，层数任意扩展
-   组级循环  ：场景组按 `displayDuration` 时间顺序切换，最后一组结束后自动回到第一组
-   无缝拼接  ：旧场景完全滚出火车左边界后才退场，新场景从当前全局最右边缘接入
-   速度完全独立  ：每个 `SceneryLayer` 的 `scrollSpeed` 在 Inspector 中设置即生效，运行时严格按该值滚动
-   原始模板保护  ：运行开始时自动隐藏场景中的原始模板对象，所有显示均为克隆体

  Inspector 配置要点  ：
- `trainLeftBoundary` / `trainRightBoundary`：火车左右边界空物体
- `sceneryGroups`：场景组数组，每组包含若干 `SceneryLayer`
- `layerTransform`：必须带 `SpriteRenderer`，Pivot 建议为 Center
- `scrollSpeed`：值越大滚动越快（建议远景 0.5~2，近景 5~20）

---

    2. 玩家控制器（TrainPlayerController）

- 支持 `Horizontal` 轴输入（A/D 或左右方向键）
- 移动范围被限制在 `trainLeftBoundary` 与 `trainRightBoundary` 之间
- 自动翻转 `SpriteRenderer.flipX`
- 可选 `Animator` 驱动行走动画（Speed 参数）

---

    3. 相机系统（CameraFollow）

- 使用 `LateUpdate` 平滑跟随玩家
- 支持 `offset` 偏移量调整
- 若启用 `useBoundaries`，相机会被限制在火车框架内（基于 `Camera.orthographicSize   aspect` 计算半宽）

---

   未来扩展：AI NPC 与情感系统

    NPC 配置结构（建议）

```csharp
[CreateAssetMenu(fileName = "NPCData", menuName = "Train/NPC Data")]
public class NPCDataSO : ScriptableObject
{
    public string npcId;                    // 唯一标识
    public string npcName;                // 显示名称
    public Sprite portrait;               // 对话头像
    public Sprite fullBodySprite;         // 场景内形象
    public string characterPrompt;        // 系统提示词（角色设定）
    public string modelEndpoint;          // 指定模型端点（可选）
    public float openness;                // 开放度：影响情感变化敏感度
    public List<string> topicsOfInterest; // 感兴趣话题标签
}
```

    对话系统流程

1. 玩家靠近 NPC，触发交互提示
2. 按交互键打开对话框（UI 预制体）
3. 玩家自由输入文本 → 经 `DialogueManager` 发送至 `LLMConnector`
4. `LLMConnector` 拼接 `characterPrompt + 对话历史 + 用户输入`，请求 API
5. 返回 NPC 回复，显示在对话框中，并记录到对话历史

    情感追踪系统（EmotionTracker）

  短期情感（单轮对话）  ：
- 分析玩家输入的情感倾向（正面 / 负面 / 中性 / 愤怒 / 悲伤 / 喜悦）
- 动态调整 NPC 本轮回应的语气（安慰、兴奋、严肃、调侃等）

  长期情感（关系度）  ：
- 维护 `RelationshipScore`（-100 ~ +100）
- 连续友好对话提升好感，冲突/冒犯降低好感
- 好感度影响 NPC 主动行为（赠送物品、透露秘密、改变立绘表情等）

  情感接口预留  ：
```csharp
public interface IEmotionAnalyzer
{
    EmotionResult Analyze(string userInput);
    void UpdateRelationship(string npcId, EmotionResult emotion);
    float GetRelationshipScore(string npcId);
}
```

---

   快速开始

1.   场景搭建  
   - 创建空物体作为 `SceneryManager`，挂载脚本
   - 在火车左右各放一个空物体作为 `trainLeftBoundary` / `trainRightBoundary`
   - 创建场景组（如：森林、城市、沙漠），每组配置各层 SpriteRenderer
   - 设置 `LayerType` 与 `scrollSpeed`

2.   玩家设置  
   - 创建玩家角色，挂载 `TrainPlayerController`，拖入边界引用
   - 添加 `SpriteRenderer` 与 `Animator`（可选）

3.   相机设置  
   - 主相机挂载 `CameraFollow`，拖入玩家 Transform
   - 如需边界限制，勾选 `useBoundaries` 并拖入边界

4.   运行测试  
   - 点击 Play，观察风景是否正常滚动、无留白、无缝循环
   - 检查各层速度是否符合 Inspector 设置

---

   注意事项

-   同 LayerType 速度建议一致  ：虽然代码允许不同组同类型速度不同，但接缝处会因速度差产生错位，建议统一规划
-   SceneryManager 的 Scale  ：确保 `SceneryManager` 所在物体的 `Scale` 为 `(1, 1, 1)`，否则克隆体缩放会异常
-   原始模板隐藏  ：运行后场景中的原始模板会被自动隐藏，所有可见内容均为运行时克隆，请勿依赖原始物体做逻辑
-   火车边界与相机边界  ：`TrainPlayerController` 的边界限制玩家移动，`CameraFollow` 的边界限制相机视野，两者可共用同一组边界空物体

---

   开发路线图

- [x] 视差风景系统（多组、多层、无缝、循环）
- [x] 火车内玩家移动与相机
- [ ] 火车车门交互与场景切换（下车/上车）
- [ ] NPC 实体与交互触发
- [ ] 对话框 UI 与输入系统
- [ ] LLM API 接入与多轮对话
- [ ] 情感分析模块（接入或本地轻量模型）
- [ ] 情感关系度与 NPC 动态行为
- [ ] 存档系统（对话历史、情感分数持久化）

---

 本项目旨在创造一个"行驶中的情感空间"——火车不仅是交通工具，更是玩家与 AI 角色建立关系的流动舞台。 
