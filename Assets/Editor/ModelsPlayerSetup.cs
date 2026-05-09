#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pipeline: fix FBX skin import → Humanoid → build character prefab with hand sockets → swap Player.prefab child.
/// Unity 6 uses ModelImporterAnimationType.Human (not Humanoid).
/// </summary>
public static class ModelsPlayerSetup
{
    const string ModelPath = "Assets/Models/base.fbx";
    /// <summary>Blender Rigify 导出（Tools/BlenderRig/run_rig_keepdribbling.bat），有人形骨架时优先使用。</summary>
    const string RiggedModelPath = "Assets/Models/base_rigged.fbx";
    const string CharacterPrefabPath = "Assets/Models/PlayerCharacter_FromBase.prefab";
    const string PlayerPrefabPath = "Assets/Player.prefab";
    const string AnimatorControllerPath = "Assets/Animations/PlayerAnimController.controller";
    /// <summary>工程里已有的人形模型；骨架与 Mixamo 接近时可尝试「复制 Rig」。</summary>
    const string FallbackHumanoidSourcePath = "Assets/BasketBallActions/Models/Player_Base.fbx";

    const string CharacterLitMaterialPath = "Assets/Models/Materials/BaseCharacter_URP.mat";
    const string DiffuseTexturePath = "Assets/Models/texture_diffuse.png";
    const string NormalTexturePath = "Assets/Models/texture_normal.png";

    /// <summary>Batch: Unity.exe -batchmode -quit -executeMethod ModelsPlayerSetup.ApplyFromModelsFolder</summary>
    public static void ApplyFromModelsFolder()
    {
        ApplyInternal();
    }

    [MenuItem("Keep Dribbling/Replace Player With Models/base.fbx", false, 0)]
    [MenuItem("KeepDribbling/Replace Player With Models/base.fbx", false, 0)]
    static void ApplyFromModelsFolderMenu()
    {
        ApplyInternal();
    }

    [MenuItem("Keep Dribbling/Debug: Log base.fbx transform hierarchy", false, 100)]
    [MenuItem("KeepDribbling/Debug: Log base.fbx transform hierarchy", false, 100)]
    static void DebugLogBaseHierarchy()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (prefab == null)
        {
            Debug.LogError("Missing " + ModelPath);
            return;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            Debug.Log("--- base.fbx hierarchy (depth-first) ---");
            LogTransforms(go.transform, 0);

            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (smrs.Length > 0)
            {
                foreach (var smr in smrs)
                    Debug.Log($"SkinnedMeshRenderer on '{smr.gameObject.name}', bones={smr.bones?.Length ?? 0}");
            }
            else
                Debug.Log("未发现 SkinnedMeshRenderer（可能是静态 MeshFilter/MeshRenderer）。");

            int totalTransforms = go.GetComponentsInChildren<Transform>(true).Length;
            if (totalTransforms <= 3)
            {
                Debug.LogWarning(
                    "当前层级几乎没有骨骼节点（常见只有 base → model）。Humanoid 必须先有 **骨骼 Transform 层级**。\n" +
                    "请在 Blender：`选中 Armature + Mesh` → File → Export FBX，勾选 **骨架 Armature**、`Apply Transform`，必要时 `Bake Animation` 关掉若只做 T-Pose；导出后覆盖 Assets/Models/base.fbx，再执行 Replace。");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static void LogTransforms(Transform t, int depth)
    {
        Debug.Log(new string(' ', depth * 2) + t.name + "  (" + t.childCount + " children)");
        for (int i = 0; i < t.childCount; i++)
            LogTransforms(t.GetChild(i), depth + 1);
    }

    static void ApplyInternal()
    {
        bool hasRigged = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RiggedModelPath) != null;
        bool hasBase = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ModelPath) != null;
        bool hasFallback = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(FallbackHumanoidSourcePath) != null;
        if (!hasRigged && !hasBase && !hasFallback)
        {
            Debug.LogError($"ModelsPlayerSetup: 缺少 {RiggedModelPath} / {ModelPath} / {FallbackHumanoidSourcePath}。");
            return;
        }

        GameObject temp = null;
        string sourceUsed = null;

        // 只要工程里有 base_rigged.fbx，就只做「新绑骨模型」，失败直接中止，避免静默退回 Player_Base（看起来像「还是原来的人」）。
        if (hasRigged)
        {
            // Rigify 等与 Player_Base 骨架不一致：切勿 Copy Avatar，否则会写坏 base_rigged.fbx.meta。
            temp = TryBuildFromModel(RiggedModelPath, tryCopyAvatarFix: false);
            if (temp != null)
                sourceUsed = RiggedModelPath;
            else
            {
                Debug.LogError(
                    "ModelsPlayerSetup: base_rigged.fbx 无法生成可用角色（Humanoid Avatar 或左右手骨骼）。\n" +
                    "请选中 Assets/Models/base_rigged.fbx → Rig → Configure… 修正映射（勿使用 Copy From Other Avatar）。\n" +
                    "若暂时想用旧外观，可在 Project 中改名或移走 base_rigged.fbx 后再执行 Replace。");
                return;
            }
        }
        else if (hasBase)
        {
            temp = TryBuildFromModel(ModelPath, tryCopyAvatarFix: true);
            if (temp != null)
                sourceUsed = ModelPath;

            if (temp == null && hasFallback)
            {
                Debug.LogWarning(
                    "ModelsPlayerSetup: base.fbx 不可用，已改用 Player_Base.fbx。");
                temp = TryBuildFromModel(FallbackHumanoidSourcePath, tryCopyAvatarFix: false);
                if (temp != null)
                    sourceUsed = FallbackHumanoidSourcePath;
            }
        }
        else if (hasFallback)
        {
            temp = TryBuildFromModel(FallbackHumanoidSourcePath, tryCopyAvatarFix: false);
            if (temp != null)
                sourceUsed = FallbackHumanoidSourcePath;
        }

        if (temp == null)
        {
            Debug.LogError(
                "ModelsPlayerSetup: 无法生成 Humanoid 角色。请准备 base_rigged.fbx，或对 FBX Configure Hips。");
            return;
        }

        PrefabUtility.SaveAsPrefabAsset(temp, CharacterPrefabPath);
        UnityEngine.Object.DestroyImmediate(temp);

        GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
        if (characterPrefab == null)
        {
            Debug.LogError("ModelsPlayerSetup: failed to save character prefab.");
            return;
        }

        if (!ReplacePlayerChildPrefab(characterPrefab))
            Debug.LogError("ModelsPlayerSetup: failed to update Player.prefab.");

        AssetDatabase.SaveAssets();
        Debug.Log($"ModelsPlayerSetup: done. 使用模型: {sourceUsed} → {CharacterPrefabPath}");
    }

    /// <returns>临时实例（尚未写成 Prefab），失败返回 null。</returns>
    static GameObject TryBuildFromModel(string modelAssetPath, bool tryCopyAvatarFix)
    {
        ConfigureHumanoidCreateFromModel(modelAssetPath);
        AssetDatabase.Refresh();

        GameObject temp = BuildCharacterHierarchyFor(modelAssetPath);
        if (temp != null)
            return temp;

        if (tryCopyAvatarFix
            && modelAssetPath == ModelPath
            && ConfigureHumanoidCopyFromOther(modelAssetPath, FallbackHumanoidSourcePath))
        {
            AssetDatabase.Refresh();
            temp = BuildCharacterHierarchyFor(modelAssetPath);
        }

        return temp;
    }

    static void ApplySkinImportHints(ModelImporter mi)
    {
        if (mi == null)
            return;
        // 保留层级、勿合并骨骼，便于识别 Hips / 手脚（与 base.fbx.meta 一致）
        mi.preserveHierarchy = true;
        mi.optimizeBones = false;
    }

    static void ConfigureHumanoidCreateFromModel(string path)
    {
        var mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi == null)
            return;

        ApplySkinImportHints(mi);
        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.sourceAvatar = null;
        mi.SaveAndReimport();

        // Rigify FBX：自动映射常把 LeftHand/RightHand 绑到 thumb.01.*，与拇指 Humanoid 槽重复 → Avatar 无效。
        mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi != null && TryFixRigifyThumbMisassignedAsHands(mi))
            mi.SaveAndReimport();
    }

    /// <summary>
    /// Unity Humanoid 自动映射有时把手腕绑到拇指第一节；拇指链仍需映射到 Left/Right Thumb *，手腕应绑 hand.L / hand.R。
    /// </summary>
    static bool TryFixRigifyThumbMisassignedAsHands(ModelImporter mi)
    {
        HumanDescription hd = mi.humanDescription;
        if (hd.human == null || hd.human.Length == 0)
            return false;

        var bones = new List<HumanBone>(hd.human);
        bool changed = false;
        for (int i = 0; i < bones.Count; i++)
        {
            HumanBone b = bones[i];
            if (b.humanName == "LeftHand" && b.boneName == "thumb.01.L")
            {
                b.boneName = "hand.L";
                bones[i] = b;
                changed = true;
            }
            else if (b.humanName == "RightHand" && b.boneName == "thumb.01.R")
            {
                b.boneName = "hand.R";
                bones[i] = b;
                changed = true;
            }
        }

        if (!changed)
            return false;

        hd.human = bones.ToArray();
        mi.humanDescription = hd;
        return true;
    }

    static bool ConfigureHumanoidCopyFromOther(string targetPath, string sourceModelPath)
    {
        var mi = AssetImporter.GetAtPath(targetPath) as ModelImporter;
        Avatar src = LoadAvatarSubAsset(sourceModelPath);
        if (mi == null || src == null)
            return false;

        ApplySkinImportHints(mi);
        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        mi.sourceAvatar = src;
        mi.SaveAndReimport();
        return true;
    }

    static GameObject BuildCharacterHierarchyFor(string modelAssetPath)
    {
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
        if (src == null)
            return null;

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(src);
        root.name = "PlayerCharacter_Runtime";

        Avatar avatar = LoadAvatarSubAsset(modelAssetPath);
        var animator = root.GetComponent<Animator>();
        if (animator == null)
            animator = root.AddComponent<Animator>();

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false;

        if (avatar == null || !avatar.isHuman)
        {
            UnityEngine.Object.DestroyImmediate(root);
            return null;
        }

        animator.Rebind();
        animator.Update(0f);

        if (!TryResolveHandTransforms(animator, root.transform, out Transform lh, out Transform rh))
        {
            Debug.LogError("ModelsPlayerSetup: 找不到左右手骨骼（Humanoid 映射与层级命名都不匹配）。");
            UnityEngine.Object.DestroyImmediate(root);
            return null;
        }

        CreateSocket(lh, "LeftHandSocket");
        CreateSocket(rh, "RightHandSocket");

        if (root.GetComponent<AnimationEventProxy>() == null)
            root.AddComponent<AnimationEventProxy>();

        ApplyCharacterLitMaterials(root.transform);

        return root;
    }

    static void EnsureFolderExists(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
            return;

        string parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
        string leaf = Path.GetFileName(assetFolderPath);
        if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
            EnsureFolderExists(parent);

        AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, leaf);
    }

    static Material EnsureCharacterLitMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(CharacterLitMaterialPath);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        EnsureFolderExists(Path.GetDirectoryName(CharacterLitMaterialPath)?.Replace('\\', '/') ?? "Assets/Models/Materials");

        var mat = new Material(shader) { name = "BaseCharacter_URP" };

        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(DiffuseTexturePath);
        if (diffuse != null)
        {
            mat.SetTexture("_BaseMap", diffuse);
            mat.SetTexture("_MainTex", diffuse);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
        }

        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
        if (normal != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 1f);
        }

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.05f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.4f);

        AssetDatabase.CreateAsset(mat, CharacterLitMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void ApplyCharacterLitMaterials(Transform root)
    {
        Material mat = EnsureCharacterLitMaterial();
        if (mat == null)
            return;

        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            int n = smr.sharedMaterials.Length;
            if (n <= 0)
                continue;
            var mats = new Material[n];
            for (int i = 0; i < n; i++)
                mats[i] = mat;
            smr.sharedMaterials = mats;
        }

        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            int n = mr.sharedMaterials.Length;
            if (n <= 0)
                continue;
            var mats = new Material[n];
            for (int i = 0; i < n; i++)
                mats[i] = mat;
            mr.sharedMaterials = mats;
        }
    }

    /// <summary>
    /// 垂直高度由运行时 PlayerMovement 射线对齐球场；此处勿写负 local Y（会导致脚永远在 pivot 下方陷地）。
    /// </summary>
    static void LiftCharacterFeetToGroundPlane(Transform characterRoot)
    {
        characterRoot.localPosition = Vector3.zero;
        characterRoot.localRotation = Quaternion.identity;
    }

    static bool TryResolveHandTransforms(Animator animator, Transform root, out Transform lh, out Transform rh)
    {
        lh = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rh = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (lh != null && rh != null)
            return true;

        lh ??= FindHandByNaming(root, true);
        rh ??= FindHandByNaming(root, false);
        return lh != null && rh != null;
    }

    static Transform FindHandByNaming(Transform root, bool left)
    {
        string[] keys = left
            ? new[] { "lefthand", "hand_l", "hand.l", "l_hand", "mixamorig:lefthand", "left_hand" }
            : new[] { "righthand", "hand_r", "hand.r", "r_hand", "mixamorig:righthand", "right_hand" };

        Transform best = null;
        int bestRank = -1;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.Replace(" ", "").Replace("_", "").Replace(".", "").ToLowerInvariant();
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i].Replace("_", "").Replace(".", "").Replace(":", "");
                if (n.Contains(k))
                {
                    if (i > bestRank)
                    {
                        bestRank = i;
                        best = t;
                    }
                }
            }
        }

        return best;
    }

    static void CreateSocket(Transform parent, string socketName)
    {
        var existing = parent.Find(socketName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject(socketName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
    }

    static Avatar LoadAvatarSubAsset(string modelPath)
    {
        foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            if (o is Avatar a)
                return a;
        return null;
    }

    static bool ReplacePlayerChildPrefab(GameObject characterPrefab)
    {
        GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            var pm = playerRoot.GetComponent<PlayerMovement>();
            if (pm == null)
            {
                Debug.LogError("ModelsPlayerSetup: PlayerMovement missing on Player.prefab root.");
                return false;
            }

            for (int i = playerRoot.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(playerRoot.transform.GetChild(i).gameObject);

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab, playerRoot.transform);

            LiftCharacterFeetToGroundPlane(inst.transform);

            pm.rightHandSocket = FindChildRecursive(inst.transform, "RightHandSocket");
            pm.leftHandSocket = FindChildRecursive(inst.transform, "LeftHandSocket");

            if (pm.rightHandSocket == null || pm.leftHandSocket == null)
                Debug.LogWarning("ModelsPlayerSetup: Hand sockets not found under new character — assign manually on PlayerMovement.");

            PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerRoot);
        }
    }

    static Transform FindChildRecursive(Transform t, string name)
    {
        foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
            if (c.name == name)
                return c;
        return null;
    }
}
#endif
