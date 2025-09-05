using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TrackedImageSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ImagePrefabPair
    {
        public string imageName;   // 必须与 Reference Image Library 中的 Name 完全一致
        public GameObject prefab;  // 要生成的内容
        public bool matchImageSize = true; // 是否把内容缩放到图片物理尺寸
    }

    public ARTrackedImageManager trackedImageManager;
    public List<ImagePrefabPair> imagePrefabs = new List<ImagePrefabPair>();

    // 运行时保存：TrackableId -> 实例
    private readonly Dictionary<TrackableId, GameObject> spawnedById = new();
    // 名字索引，便于查找
    private readonly Dictionary<string, ImagePrefabPair> mapByName = new();

    void Awake()
    {
        foreach (var pair in imagePrefabs)
        {
            if (pair != null && !string.IsNullOrEmpty(pair.imageName) && pair.prefab != null)
                mapByName[pair.imageName] = pair;
        }
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        // 新增
        foreach (var img in args.added)
            CreateOrUpdate(img);

        // 更新（姿态/跟踪状态变化）
        foreach (var img in args.updated)
            CreateOrUpdate(img);

        // 移除
        foreach (var img in args.removed)
        {
            if (spawnedById.TryGetValue(img.trackableId, out var go) && go)
                Destroy(go);
            spawnedById.Remove(img.trackableId);
        }
    }

    private void CreateOrUpdate(ARTrackedImage trackedImage)
    {
        // 跟踪状态可用时才显示
        bool visible = trackedImage.trackingState == TrackingState.Tracking;

        // 找该图像名对应的 Prefab
        string name = trackedImage.referenceImage.name;
        if (!mapByName.TryGetValue(name, out var pair) || pair.prefab == null)
        {
            SetActiveById(trackedImage.trackableId, false);
            return;
        }

        // 没生成过就实例化
        if (!spawnedById.TryGetValue(trackedImage.trackableId, out var instance) || instance == null)
        {
            instance = Instantiate(pair.prefab, trackedImage.transform);
            instance.name = $"Spawned_{name}";
            spawnedById[trackedImage.trackableId] = instance;
        }

        // 位置/旋转直接跟随 trackedImage（其原点在图像平面中心，法线朝上）
        instance.transform.SetPositionAndRotation(trackedImage.transform.position, trackedImage.transform.rotation);

        // 可选：匹配图像物理尺寸（以宽度匹配，保持等比）
        if (pair.matchImageSize)
        {
            var size = trackedImage.size; // 单位：米（来自 ReferenceImage 的物理尺寸）
            // 约定 Prefab 在 1x1 尺寸下刚好占满单位方形，此处按宽度缩放
            float targetWidth = size.x;
            // 计算当前实例在本地的“内容宽度”，如果无法测得，就直接按宽度=1 的约定来
            float currentWidth = 1f;
            var renderer = instance.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                currentWidth = renderer.bounds.size.x / instance.transform.lossyScale.x;
                if (currentWidth <= 0.0001f) currentWidth = 1f;
            }
            float scale = targetWidth / currentWidth;
            instance.transform.localScale = Vector3.one * scale;
        }

        // 显示/隐藏（丢失跟踪就先隐藏）
        instance.SetActive(visible);
    }

    private void SetActiveById(TrackableId id, bool active)
    {
        if (spawnedById.TryGetValue(id, out var go) && go)
            go.SetActive(active);
    }
}
