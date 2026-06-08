using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public enum LayerType
{
    Background,
    Midground,
    Foreground,
    NearForeground,
    Sky,
    Ground
}

[System.Serializable]
public class SceneryLayer
{
    public LayerType layerType;
    public Transform layerTransform;
    public float scrollSpeed = 1f;
    [HideInInspector] public float layerWidth;
}

[System.Serializable]
public class SceneryGroup
{
    public string groupName;
    public float displayDuration = 60f;
    public List<SceneryLayer> layers = new List<SceneryLayer>();
}

public class SceneryBlock
{
    public Transform transform;
    public int groupIndex;
    public float width;
    public float scrollSpeed;
    public float leftEdge => transform.position.x - width * 0.5f;
    public float rightEdge => transform.position.x + width * 0.5f;
}

public class SceneryManager : MonoBehaviour
{
    [Header("场景组配置")]
    public List<SceneryGroup> sceneryGroups;

    [Header("火车边界")]
    public Transform trainLeftBoundary;
    public Transform trainRightBoundary;

    [Header("起始X偏移")]
    public float startXOffset = 0f;

    private class LayerState
    {
        public LayerType layerType;
        public List<SceneryBlock> blocks = new List<SceneryBlock>();
        public Transform[] groupPrefabs;
        public float[] groupWidths;
        public float maxWidth;
    }

    private Dictionary<LayerType, LayerState> layerStates = new Dictionary<LayerType, LayerState>();
    private int[] groupBlockCounts;

    private int currentGroupIndex = 0;
    private int nextGroupIndex = 0;
    private float timer = 0f;
    private bool initialized = false;

    void Start() => Initialize();

    void Update()
    {
        if (!initialized) return;
        if (trainLeftBoundary == null || trainRightBoundary == null)
        {
            Debug.LogError("火车边界未设置！");
            return;
        }

        float trainLeft = trainLeftBoundary.position.x;
        float trainRight = trainRightBoundary.position.x;

        // 1. 滚动：每个块用自己的 scrollSpeed
        foreach (var state in layerStates.Values)
        {
            foreach (var block in state.blocks)
            {
                float move = block.scrollSpeed * Time.deltaTime;
                block.transform.position += Vector3.left * move;
            }
        }

        // 2. 清理已离开左边界的块
        foreach (var state in layerStates.Values)
        {
            while (state.blocks.Count > 0)
            {
                var first = state.blocks[0];
                if (first.rightEdge < trainLeft)
                {
                    Destroy(first.transform.gameObject);
                    groupBlockCounts[first.groupIndex]--;
                    state.blocks.RemoveAt(0);
                }
                else break;
            }
        }

        // 3. 检查旧场景是否完全退场，正式切换
        if (currentGroupIndex != nextGroupIndex && !IsGroupOnScreen(currentGroupIndex))
        {
            Debug.Log($"[SceneryManager] {sceneryGroups[currentGroupIndex].groupName} 完全退场，正式切换为 {sceneryGroups[nextGroupIndex].groupName}");
            currentGroupIndex = nextGroupIndex;
            timer = 0f;
        }

        // 4. 补充新块：严格只使用 nextGroupIndex
        foreach (var state in layerStates.Values)
        {
            float layerRightEdge = GetLayerRightEdge(state);
            float safetyThreshold = trainRight + state.maxWidth * 2f;

            // ========== 核心修复：新层首次出现，强制从全局最右边缘接第一块 ==========
            if (state.blocks.Count == 0)
            {
                float globalRight = GetGlobalRightEdge();
                int groupIndex = GetValidGroupIndex(state, nextGroupIndex);
                if (groupIndex >= 0)
                {
                    float w = state.groupWidths[groupIndex];
                    // 强制在全局最右边缘生成第一块，不受 safetyThreshold 限制
                    // 确保新层不会出现在火车左边，而是紧跟现有内容
                    SpawnBlock(state, groupIndex, globalRight + w * 0.5f);
                    layerRightEdge = GetLayerRightEdge(state);
                }
            }

            while (layerRightEdge < safetyThreshold)
            {
                int groupIndex = GetValidGroupIndex(state, nextGroupIndex);
                if (groupIndex < 0) break;

                float w = state.groupWidths[groupIndex];
                float spawnX = layerRightEdge + w * 0.5f;
                SpawnBlock(state, groupIndex, spawnX);
                layerRightEdge = GetLayerRightEdge(state);
            }
        }

        // 5. 计时器：控制何时预告下一个场景
        if (currentGroupIndex == nextGroupIndex)
        {
            timer += Time.deltaTime;
            if (timer >= sceneryGroups[currentGroupIndex].displayDuration)
            {
                // 自动循环：最后一个场景的下一个就是第一个场景
                nextGroupIndex = (nextGroupIndex + 1) % sceneryGroups.Count;
                Debug.Log($"[SceneryManager] {sceneryGroups[currentGroupIndex].groupName} 时间到，开始接入 {sceneryGroups[nextGroupIndex].groupName}");
            }
        }
    }

    void Initialize()
    {
        if (sceneryGroups.Count == 0) return;
        if (trainLeftBoundary == null || trainRightBoundary == null)
        {
            Debug.LogError("请在 Inspector 中设置 trainLeftBoundary 和 trainRightBoundary！");
            return;
        }

        groupBlockCounts = new int[sceneryGroups.Count];

        // 收集所有类型
        HashSet < LayerType > allTypes = new HashSet< LayerType > ();
        foreach (var group in sceneryGroups)
            foreach (var layer in group.layers)
                allTypes.Add(layer.layerType);

        // 初始化各层状态
        foreach (var type in allTypes)
        {
            LayerState state = new LayerState();
            state.layerType = type;
            state.groupPrefabs = new Transform[sceneryGroups.Count];
            state.groupWidths = new float[sceneryGroups.Count];
            float maxW = 0f;

            for (int g = 0; g < sceneryGroups.Count; g++)
            {
                var group = sceneryGroups[g];
                SceneryLayer match = null;
                for (int l = 0; l < group.layers.Count; l++)
                {
                    if (group.layers[l].layerType == type)
                    {
                        match = group.layers[l];
                        break;
                    }
                }

                if (match != null)
                {
                    state.groupPrefabs[g] = match.layerTransform;

                    if (match.layerTransform != null)
                    {
                        SpriteRenderer sr = match.layerTransform.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            float w = sr.bounds.size.x;
                            state.groupWidths[g] = w;
                            match.layerWidth = w;
                            if (w > maxW) maxW = w;
                        }
                        else
                        {
                            Debug.LogError($"场景组 {group.groupName} 的 {type} 层没有 SpriteRenderer！");
                        }
                    }
                }
            }

            state.maxWidth = maxW;
            layerStates[type] = state;
        }

        // 初始铺设：严格只使用第 0 组
        float trainLeft = trainLeftBoundary.position.x;
        float trainRight = trainRightBoundary.position.x;

        foreach (var state in layerStates.Values)
        {
            int groupIndex = GetValidGroupIndex(state, 0);
            if (groupIndex < 0) continue;

            float w = state.groupWidths[groupIndex];
            if (w <= 0) continue;

            float targetRight = trainRight + state.maxWidth * 2f;
            float currentX = trainLeft + startXOffset;

            while (currentX < targetRight)
            {
                SpawnBlock(state, groupIndex, currentX + w * 0.5f);
                currentX += w;
            }
        }

        currentGroupIndex = 0;
        nextGroupIndex = 0;
        timer = 0f;
        initialized = true;

        // 隐藏原始模板
        foreach (var group in sceneryGroups)
        {
            foreach (var layer in group.layers)
            {
                if (layer.layerTransform != null && layer.layerTransform.gameObject.activeSelf)
                    layer.layerTransform.gameObject.SetActive(false);
            }
        }

        Debug.Log($"[SceneryManager] 初始化完成：{layerStates.Count} 个层类型，{sceneryGroups.Count} 个场景组。");
    }

    int GetValidGroupIndex(LayerState state, int preferredIndex)
    {
        if (preferredIndex >= 0 && preferredIndex < state.groupPrefabs.Length && state.groupPrefabs[preferredIndex] != null)
            return preferredIndex;
        return -1;
    }

    float GetLayerScrollSpeed(int groupIndex, LayerType type)
    {
        if (groupIndex < 0 || groupIndex >= sceneryGroups.Count) return 1f;
        var group = sceneryGroups[groupIndex];
        foreach (var layer in group.layers)
        {
            if (layer.layerType == type)
                return layer.scrollSpeed;
        }
        return 1f;
    }

    void SpawnBlock(LayerState state, int groupIndex, float posX)
    {
        Transform prefab = state.groupPrefabs[groupIndex];
        if (prefab == null) return;

        Transform instance = Instantiate(prefab, this.transform);
        instance.position = new Vector3(posX, prefab.position.y, prefab.position.z);
        instance.gameObject.SetActive(true);

        float speed = GetLayerScrollSpeed(groupIndex, state.layerType);

        SceneryBlock block = new SceneryBlock
        {
            transform = instance,
            groupIndex = groupIndex,
            width = state.groupWidths[groupIndex],
            scrollSpeed = speed
        };

        state.blocks.Add(block);
        groupBlockCounts[groupIndex]++;
    }

    // ========== 新增：获取所有层中最右的边缘 ==========
    float GetGlobalRightEdge()
    {
        float max = trainRightBoundary.position.x;
        foreach (var s in layerStates.Values)
        {
            if (s.blocks.Count > 0)
            {
                float r = s.blocks[s.blocks.Count - 1].rightEdge;
                if (r > max) max = r;
            }
        }
        return max;
    }

    float GetLayerRightEdge(LayerState state)
    {
        if (state.blocks.Count == 0)
            return trainRightBoundary.position.x;
        return state.blocks[state.blocks.Count - 1].rightEdge;
    }

    public int GetGroupBlockCount(int groupIndex)
    {
        if (groupBlockCounts == null || groupIndex < 0 || groupIndex >= groupBlockCounts.Length) return 0;
        return groupBlockCounts[groupIndex];
    }

    public bool IsGroupOnScreen(int groupIndex) => GetGroupBlockCount(groupIndex) > 0;
    public int CurrentGroupIndex => currentGroupIndex;
    public int NextGroupIndex => nextGroupIndex;
    public string CurrentGroupName => sceneryGroups.Count > 0 ? sceneryGroups[currentGroupIndex].groupName : "None";
    public string NextGroupName => sceneryGroups.Count > 0 ? sceneryGroups[nextGroupIndex].groupName : "None";
}