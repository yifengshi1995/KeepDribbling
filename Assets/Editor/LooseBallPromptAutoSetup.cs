#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click setup for LooseBallPromptUI + optional CameraShakeJuice on follow cameras.
/// Menu: <b>Keep Dribbling</b> (top bar).
/// </summary>
public static class LooseBallPromptAutoSetup
{
    const string MenuSetupAll = "Keep Dribbling/Setup Loose Ball Prompt + Camera Shake";
    const string MenuPromptOnly = "Keep Dribbling/Setup Loose Ball Prompt UI Only";

    [MenuItem(MenuSetupAll, priority = 1)]
    static void SetupAll()
    {
        SetupPromptInternal(rewireCameraShake: true);
    }

    [MenuItem(MenuPromptOnly, priority = 2)]
    static void SetupPromptOnly()
    {
        SetupPromptInternal(rewireCameraShake: false);
    }

    static void SetupPromptInternal(bool rewireCameraShake)
    {
        Canvas canvas = FindPreferredCanvas();
        if (canvas == null)
        {
            canvas = CreateCanvasRoot();
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }

        const string rootName = "LooseBallPrompt";
        Transform canvasTf = canvas.transform;
        Transform existing = canvasTf.Find(rootName);
        GameObject root;
        if (existing != null)
        {
            root = existing.gameObject;
            Undo.RecordObject(root, "Rewire Loose Ball Prompt");
        }
        else
        {
            root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Loose Ball Prompt");
            Undo.SetTransformParent(root.transform, canvasTf, "Parent Loose Ball Prompt");
        }

        RectTransform rootRt = root.GetComponent<RectTransform>();
        if (rootRt == null)
            rootRt = Undo.AddComponent<RectTransform>(root);
        StretchFullScreen(rootRt);

        CanvasGroup cg = root.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = Undo.AddComponent<CanvasGroup>(root);
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        LooseBallPromptUI promptUI = root.GetComponent<LooseBallPromptUI>();
        if (promptUI == null)
            promptUI = Undo.AddComponent<LooseBallPromptUI>(root);

        GameObject textGo;
        Transform textChild = root.transform.Find("PromptText");
        if (textChild != null)
            textGo = textChild.gameObject;
        else
        {
            textGo = new GameObject("PromptText");
            Undo.RegisterCreatedObjectUndo(textGo, "Create Prompt Text");
            Undo.SetTransformParent(textGo.transform, root.transform, "Parent Prompt Text");
        }

        RectTransform textRt = textGo.GetComponent<RectTransform>();
        if (textRt == null)
            textRt = Undo.AddComponent<RectTransform>(textGo);
        textRt.anchorMin = textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(1400f, 240f);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = Undo.AddComponent<TextMeshProUGUI>(textGo);
        tmp.text = "GO GET THE BALL!";
        tmp.fontSize = 84f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.3f, 0.05f, 1f);
        tmp.raycastTarget = false;

        TMP_FontAsset fontsFolderLast = LoadLastTmpFontAssetInFolder("Assets/Fonts");
        if (fontsFolderLast != null)
            tmp.font = fontsFolderLast;

        SerializedObject so = new SerializedObject(promptUI);
        so.FindProperty("promptText").objectReferenceValue = tmp;
        so.FindProperty("rootTransform").objectReferenceValue = rootRt;
        SerializedProperty fontProp = so.FindProperty("promptFont");
        if (fontProp != null && fontsFolderLast != null)
            fontProp.objectReferenceValue = fontsFolderLast;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(textGo);
        EditorUtility.SetDirty(promptUI);

        int shakeAdded = 0;
        if (rewireCameraShake)
            shakeAdded = EnsureCameraShakeJuice();

        EditorSceneManager.MarkSceneDirty(root.scene);

        string msg = "Loose Ball Prompt is under Canvas: \"" + canvas.gameObject.name + "\".\n" +
                     "TMP + references are wired.\n";
        if (rewireCameraShake)
            msg += $"CameraShakeJuice added (or already present) on {shakeAdded} camera rig(s) with CameraFollow / FinalCameraFollow.\n";
        msg += "Pure Cinemachine scenes: add Impulse Source on CameraShakeJuice yourself.";
        EditorUtility.DisplayDialog("Keep Dribbling", msg, "OK");

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    static int EnsureCameraShakeJuice()
    {
        int count = 0;
        foreach (var cf in Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cf != null && cf.GetComponent<CameraShakeJuice>() == null)
            {
                Undo.AddComponent<CameraShakeJuice>(cf.gameObject);
                EditorUtility.SetDirty(cf.gameObject);
                count++;
            }
        }

        foreach (var f in Object.FindObjectsByType<FinalCameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (f != null && f.GetComponent<CameraShakeJuice>() == null)
            {
                Undo.AddComponent<CameraShakeJuice>(f.gameObject);
                EditorUtility.SetDirty(f.gameObject);
                count++;
            }
        }

        return count;
    }

    static Canvas FindPreferredCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas bestOverlay = null;
        foreach (Canvas c in canvases)
        {
            if (c == null) continue;
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy)
                bestOverlay = c;
        }
        if (bestOverlay != null)
            return bestOverlay;

        foreach (Canvas c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy)
                return c;
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    static Canvas CreateCanvasRoot()
    {
        GameObject go = new GameObject("Canvas");
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void StretchFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>Lexicographically last TMP Font Asset path under folder (e.g. Assets/Fonts → Schoolbell-Regular SDF).</summary>
    static TMP_FontAsset LoadLastTmpFontAssetInFolder(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { folder });
        if (guids == null || guids.Length == 0)
            return null;

        var paths = new List<string>(guids.Length);
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (!string.IsNullOrEmpty(p))
                paths.Add(p);
        }

        if (paths.Count == 0)
            return null;

        paths.Sort(System.StringComparer.Ordinal);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(paths[paths.Count - 1]);
    }
}
#endif
