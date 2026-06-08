using UnityEngine;
using System.Collections.Generic;

public class ParallaxController : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layerTransform;  // 图层Transform
        public float parallaxFactor;      // 视差因子 (0-1之间, 0=完全静止, 1=跟玩家同步移动)
        [HideInInspector] public float layerWidth;  // 图层宽度
        [HideInInspector] public List<Transform> clones = new List<Transform>(); // 克隆体列表
    }

    [Header("视差图层设置")]
    public List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();

    [Header("玩家设置")]
    public Transform player;

    [Header("视差强度")]
    [Range(0, 1)]
    public float globalParallaxMultiplier = 1f;

    [Header("图层复用数量")]
    public int cloneCount = 2; // 每个图层的克隆数量（左右各一个通常就够用）

    private Vector3 lastPlayerPosition;
    private Dictionary<Transform, Vector3> layerStartPositions = new Dictionary<Transform, Vector3>();

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("请设置玩家Transform!");
            return;
        }

        lastPlayerPosition = player.position;
        InitializeLayers();
    }

    void InitializeLayers()
    {
        foreach (var layer in parallaxLayers)
        {
            if (layer.layerTransform == null) continue;

            // 获取图层宽度（假设使用SpriteRenderer）
            SpriteRenderer spriteRenderer = layer.layerTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                layer.layerWidth = spriteRenderer.bounds.size.x;
            }
            else
            {
                // 如果没有SpriteRenderer，尝试从子对象获取
                SpriteRenderer[] childRenderers = layer.layerTransform.GetComponentsInChildren<SpriteRenderer>();
                if (childRenderers.Length > 0)
                {
                    Bounds combinedBounds = childRenderers[0].bounds;
                    for (int i = 1; i < childRenderers.Length; i++)
                    {
                        combinedBounds.Encapsulate(childRenderers[i].bounds);
                    }
                    layer.layerWidth = combinedBounds.size.x;
                }
                else
                {
                    Debug.LogWarning($"图层 {layer.layerTransform.name} 没有找到SpriteRenderer，使用默认宽度10");
                    layer.layerWidth = 10f;
                }
            }

            // 保存初始位置
            layerStartPositions[layer.layerTransform] = layer.layerTransform.position;

            // 创建克隆体实现无限滚动
            CreateClones(layer);
        }
    }

    void CreateClones(ParallaxLayer layer)
    {
        // 清除旧的克隆体
        foreach (var clone in layer.clones)
        {
            if (clone != null)
                Destroy(clone.gameObject);
        }
        layer.clones.Clear();

        // 创建左右克隆体
        for (int i = 1; i <= cloneCount; i++)
        {
            // 右侧克隆
            Transform rightClone = Instantiate(layer.layerTransform, layer.layerTransform.parent);
            rightClone.position = layer.layerTransform.position + Vector3.right * layer.layerWidth * i;
            rightClone.name = $"{layer.layerTransform.name}_Clone_R{i}";
            layer.clones.Add(rightClone);

            // 左侧克隆
            Transform leftClone = Instantiate(layer.layerTransform, layer.layerTransform.parent);
            leftClone.position = layer.layerTransform.position - Vector3.right * layer.layerWidth * i;
            leftClone.name = $"{layer.layerTransform.name}_Clone_L{i}";
            layer.clones.Add(leftClone);
        }
    }

    void Update()
    {
        Vector3 playerMovement = player.position - lastPlayerPosition;

        foreach (var layer in parallaxLayers)
        {
            if (layer.layerTransform == null) continue;

            // 计算视差移动量
            float parallaxMovement = playerMovement.x * layer.parallaxFactor * globalParallaxMultiplier;

            // 移动主图层
            Vector3 newPos = layer.layerTransform.position;
            newPos.x += parallaxMovement;
            layer.layerTransform.position = newPos;

            // 同步移动克隆体
            foreach (var clone in layer.clones)
            {
                if (clone != null)
                {
                    Vector3 clonePos = clone.position;
                    clonePos.x += parallaxMovement;
                    clone.position = clonePos;
                }
            }

            // 检查并重新定位图层（无限循环逻辑）
            RepositionLayers(layer);
        }

        lastPlayerPosition = player.position;
    }

    void RepositionLayers(ParallaxLayer layer)
    {
        float layerWidth = layer.layerWidth;

        // 检查主图层
        RepositionSingleLayer(layer.layerTransform, layer);

        // 检查所有克隆体
        foreach (var clone in layer.clones)
        {
            if (clone != null)
                RepositionSingleLayer(clone, layer);
        }
    }

    void RepositionSingleLayer(Transform layerTransform, ParallaxLayer layer)
    {
        float layerWidth = layer.layerWidth;
        Vector3 playerPos = player.position;

        // 计算图层相对于玩家的位置
        float relativeX = layerTransform.position.x - playerPos.x;

        // 如果图层太靠左，移到右边
        if (relativeX < -layerWidth * 1.5f)
        {
            Vector3 newPos = layerTransform.position;
            newPos.x += layerWidth * 3f; // 移到右边（因为有克隆体，所以移动3个宽度）
            layerTransform.position = newPos;
        }
        // 如果图层太靠右，移到左边
        else if (relativeX > layerWidth * 1.5f)
        {
            Vector3 newPos = layerTransform.position;
            newPos.x -= layerWidth * 3f; // 移到左边
            layerTransform.position = newPos;
        }
    }

    // 在编辑器中更新克隆体
    void OnValidate()
    {
        if (Application.isPlaying && parallaxLayers != null)
        {
            foreach (var layer in parallaxLayers)
            {
                if (layer.layerTransform != null)
                {
                    CreateClones(layer);
                }
            }
        }
    }
}