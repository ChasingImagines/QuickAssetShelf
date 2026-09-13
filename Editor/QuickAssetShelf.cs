#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Rafa alınabilen asset türleri.</summary>
public enum ShelfAssetKind
{
    Prefab,
    ScriptableObject,
    Material,
}

/// <summary>
/// Quick Asset Shelf — Prefab, ScriptableObject ve Material'lar için kalıcı bir "hızlı erişim rafı".
/// Seçilen asset'ler otomatik kaydedilir, sabitlenebilir, aranabilir ve prefab'lar
/// Scene View'da "damga" olarak hızlıca yerleştirilebilir.
/// </summary>
[InitializeOnLoad]
public static class QuickAssetShelfService
{
    private const string PrefAutoRecord = "QuickAssetShelf_AutoRecord";
    private const string PrefRecents = "QuickAssetShelf_RecentGuids";
    private const string PrefPinned = "QuickAssetShelf_PinnedGuids";
    private const string PrefStamp = "QuickAssetShelf_StampGuid";
    private const string PrefIgnoredFolders = "QuickAssetShelf_IgnoredFolders";
    private const string PrefTrackPrefab = "QuickAssetShelf_TrackPrefab";
    private const string PrefTrackSo = "QuickAssetShelf_TrackSO";
    private const string PrefTrackMaterial = "QuickAssetShelf_TrackMaterial";
    private const string PrefHistoryLimit = "QuickAssetShelf_HistoryLimit";

    public const int DefaultHistoryLimit = 50;
    public const int MinHistoryLimit = 10;
    public const int MaxHistoryLimit = 200;

    public static readonly List<string> RecentGuids = new();
    public static readonly List<string> PinnedGuids = new();

    /// <summary>Bu klasörlerin (ve alt klasörlerinin) altındaki asset'ler rafa hiç alınmaz.</summary>
    public static readonly List<string> IgnoredFolders = new();

    private static GameObject _stampPrefab;

    /// <summary>Scene View'da yerleştirilecek aktif prefab (damga). GUID'i EditorPrefs'te saklanır.</summary>
    public static GameObject ActiveSpawnPrefab
    {
        get => _stampPrefab;
        set
        {
            _stampPrefab = value;
            string guid = value != null
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value))
                : string.Empty;
            EditorPrefs.SetString(PrefStamp, guid);
            SceneView.RepaintAll();
        }
    }

    public static bool AutoRecord
    {
        get => EditorPrefs.GetBool(PrefAutoRecord, true);
        set => EditorPrefs.SetBool(PrefAutoRecord, value);
    }

    // ---------- Tür takibi (tür bazlı yoksayma) ----------

    /// <summary>Prefab'lar rafa alınsın mı?</summary>
    public static bool TrackPrefabs
    {
        get => EditorPrefs.GetBool(PrefTrackPrefab, true);
        set => EditorPrefs.SetBool(PrefTrackPrefab, value);
    }

    /// <summary>ScriptableObject'ler rafa alınsın mı?</summary>
    public static bool TrackScriptableObjects
    {
        get => EditorPrefs.GetBool(PrefTrackSo, true);
        set => EditorPrefs.SetBool(PrefTrackSo, value);
    }

    /// <summary>Material'lar rafa alınsın mı?</summary>
    public static bool TrackMaterials
    {
        get => EditorPrefs.GetBool(PrefTrackMaterial, true);
        set => EditorPrefs.SetBool(PrefTrackMaterial, value);
    }

    /// <summary>Rafta tutulacak geçmiş kayıt sınırı.</summary>
    public static int HistoryLimit
    {
        get => Mathf.Clamp(EditorPrefs.GetInt(PrefHistoryLimit, DefaultHistoryLimit), MinHistoryLimit, MaxHistoryLimit);
        set => EditorPrefs.SetInt(PrefHistoryLimit, Mathf.Clamp(value, MinHistoryLimit, MaxHistoryLimit));
    }

    /// <summary>Verilen tür rafa kaydediliyor mu? (Kapalıysa tür yoksayılır.)</summary>
    public static bool IsKindTracked(ShelfAssetKind kind)
    {
        switch (kind)
        {
            case ShelfAssetKind.Prefab: return TrackPrefabs;
            case ShelfAssetKind.ScriptableObject: return TrackScriptableObjects;
            case ShelfAssetKind.Material: return TrackMaterials;
            default: return false;
        }
    }

    /// <summary>Tür takibini açar/kapatır ve açık pencereleri tazeler.</summary>
    public static void SetKindTracked(ShelfAssetKind kind, bool tracked)
    {
        switch (kind)
        {
            case ShelfAssetKind.Prefab: TrackPrefabs = tracked; break;
            case ShelfAssetKind.ScriptableObject: TrackScriptableObjects = tracked; break;
            case ShelfAssetKind.Material: TrackMaterials = tracked; break;
        }

        QuickAssetShelf.RepaintWindow();
        QuickAssetShelfSettings.RepaintWindow();
    }

    /// <summary>Tüm türler kapalı mı? (Rafın hiçbir şey kaydetmemesi durumu.)</summary>
    public static bool AllKindsDisabled => !TrackPrefabs && !TrackScriptableObjects && !TrackMaterials;

    static QuickAssetShelfService()
    {
        Load();

        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    // ---------- Kalıcılık ----------

    public static void Load()
    {
        RecentGuids.Clear();
        RecentGuids.AddRange(Split(EditorPrefs.GetString(PrefRecents, "")));

        PinnedGuids.Clear();
        PinnedGuids.AddRange(Split(EditorPrefs.GetString(PrefPinned, "")));

        IgnoredFolders.Clear();
        IgnoredFolders.AddRange(Split(EditorPrefs.GetString(PrefIgnoredFolders, "")));

        string stampGuid = EditorPrefs.GetString(PrefStamp, "");
        _stampPrefab = string.IsNullOrEmpty(stampGuid)
            ? null
            : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(stampGuid));
    }

    public static void SaveRecents() => EditorPrefs.SetString(PrefRecents, string.Join(";", RecentGuids));
    public static void SavePinned() => EditorPrefs.SetString(PrefPinned, string.Join(";", PinnedGuids));
    public static void SaveIgnoredFolders() => EditorPrefs.SetString(PrefIgnoredFolders, string.Join(";", IgnoredFolders));

    // ---------- Yoksayılan klasörler ----------

    /// <summary>Verilen asset yolu yoksayılan bir klasörün (veya alt klasörünün) içinde mi?</summary>
    public static bool IsIgnored(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return false;

        foreach (string folder in IgnoredFolders)
        {
            if (string.IsNullOrEmpty(folder)) continue;
            if (assetPath.Equals(folder, StringComparison.OrdinalIgnoreCase)) return true;
            if (assetPath.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>Yoksayılan klasör listesine klasör ekler. Değişiklik olduysa true döner.</summary>
    public static bool AddIgnoredFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return false;

        folderPath = folderPath.TrimEnd('/');
        if (IgnoredFolders.Any(f => string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase)))
            return false;

        IgnoredFolders.Add(folderPath);
        SaveIgnoredFolders();
        return true;
    }

    /// <summary>Yoksayılan klasörü listeden çıkarır. Değişiklik olduysa true döner.</summary>
    public static bool RemoveIgnoredFolder(string folderPath)
    {
        bool changed = IgnoredFolders.RemoveAll(f => string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase)) > 0;
        if (changed) SaveIgnoredFolders();
        return changed;
    }

    public static void ClearIgnoredFolders()
    {
        if (IgnoredFolders.Count == 0) return;
        IgnoredFolders.Clear();
        SaveIgnoredFolders();
    }

    /// <summary>Seçili asset'in bulunduğu klasörü yoksay listesine ekler.</summary>
    public static bool AddIgnoredFolderFromSelection()
    {
        var selected = Selection.activeObject;
        if (selected == null) return false;

        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path)) return false;

        // Klasör seçildiyse onu, dosya seçildiyse içindeki klasörü yoksay.
        string folder = AssetDatabase.IsValidFolder(path)
            ? path
            : (path.Contains('/') ? path.Substring(0, path.LastIndexOf('/')) : path);

        if (string.IsNullOrEmpty(folder)) return false;
        return AddIgnoredFolder(folder);
    }

    private static string[] Split(string raw)
        => string.IsNullOrEmpty(raw) ? Array.Empty<string>() : raw.Split(';', StringSplitOptions.RemoveEmptyEntries);

    // ---------- Sabitleme ----------

    public static bool IsPinned(string guid) => PinnedGuids.Contains(guid);

    public static void TogglePin(string guid)
    {
        if (!PinnedGuids.Remove(guid)) PinnedGuids.Insert(0, guid);
        SavePinned();
        QuickAssetShelf.RepaintWindow();
    }

    public static void Remove(string guid)
    {
        if (ActiveSpawnPrefab != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(ActiveSpawnPrefab)) == guid)
            ActiveSpawnPrefab = null;

        RecentGuids.Remove(guid);
        PinnedGuids.Remove(guid);
        SaveRecents();
        SavePinned();
    }

    public static void ClearRecents()
    {
        RecentGuids.Clear();
        SaveRecents();
        QuickAssetShelf.RepaintWindow();
    }

    /// <summary>Belirli bir türdeki tüm kayıtları (sabitlenenler dahil) raftan kaldırır. Kaldırılan sayıyı döner.</summary>
    public static int ClearKind(ShelfAssetKind kind)
    {
        int removed = RemoveByKind(RecentGuids, kind) + RemoveByKind(PinnedGuids, kind);

        // Damga yalnızca prefab olduğundan, prefab türü temizlenirse aktif damgayı da bırak.
        if (kind == ShelfAssetKind.Prefab && ActiveSpawnPrefab != null)
            ActiveSpawnPrefab = null;

        if (removed > 0)
        {
            SaveRecents();
            SavePinned();
            QuickAssetShelf.RepaintWindow();
        }

        return removed;
    }

    private static int RemoveByKind(List<string> guids, ShelfAssetKind kind)
        => guids.RemoveAll(g => GetKind(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g))) == kind);

    public static void ClearAll()
    {
        RecentGuids.Clear();
        PinnedGuids.Clear();
        IgnoredFolders.Clear();
        ActiveSpawnPrefab = null;

        EditorPrefs.DeleteKey(PrefTrackPrefab);
        EditorPrefs.DeleteKey(PrefTrackSo);
        EditorPrefs.DeleteKey(PrefTrackMaterial);
        EditorPrefs.DeleteKey(PrefHistoryLimit);

        SaveRecents();
        SavePinned();
        SaveIgnoredFolders();
        QuickAssetShelf.RepaintWindow();
        QuickAssetShelfSettings.RepaintWindow();
    }

    /// <summary>Silinmiş asset'lerin GUID'lerini iki listeden de temizler. Değişiklik olduysa true döner.</summary>
    public static bool PruneInvalid()
    {
        bool changed = PruneList(RecentGuids) | PruneList(PinnedGuids);
        if (changed)
        {
            SaveRecents();
            SavePinned();
        }
        return changed;
    }

    private static bool PruneList(List<string> guids)
    {
        int before = guids.Count;
        guids.RemoveAll(g => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g)));
        return guids.Count != before;
    }

    // ---------- Kayıt ----------

    private static void OnSelectionChanged()
    {
        if (!AutoRecord) return;

        var selected = Selection.activeObject;
        if (selected == null || !EditorUtility.IsPersistent(selected)) return;

        ShelfAssetKind? kind = GetKind(selected);
        if (kind == null || !IsKindTracked(kind.Value)) return;

        string assetPath = AssetDatabase.GetAssetPath(selected);
        if (IsIgnored(assetPath)) return;

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) return;

        RecentGuids.Remove(guid);
        RecentGuids.Insert(0, guid);

        int limit = HistoryLimit;
        while (RecentGuids.Count > limit)
            RecentGuids.RemoveAt(RecentGuids.Count - 1);

        SaveRecents();
        QuickAssetShelf.RepaintWindow();
    }

    /// <summary>Rafa alınabilir bir tür mü? (Prefab, ScriptableObject, Material)</summary>
    public static bool IsShelfAsset(UnityEngine.Object obj) => GetKind(obj) != null;

    /// <summary>Asset'in raf türünü döner; rafa uygun değilse null.</summary>
    public static ShelfAssetKind? GetKind(UnityEngine.Object obj)
    {
        switch (obj)
        {
            case null:
                return null;
            case GameObject go:
                return PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab
                    ? ShelfAssetKind.Prefab
                    : (ShelfAssetKind?)null;
            case ScriptableObject:
                return ShelfAssetKind.ScriptableObject;
            case Material:
                return ShelfAssetKind.Material;
            default:
                return null;
        }
    }

    /// <summary>Asset türünün Türkçe görünen adı.</summary>
    public static string KindLabel(ShelfAssetKind kind)
    {
        switch (kind)
        {
            case ShelfAssetKind.Prefab: return "Prefab";
            case ShelfAssetKind.ScriptableObject: return "SO";
            case ShelfAssetKind.Material: return "Materyal";
            default: return kind.ToString();
        }
    }

    // ---------- Damga (Scene View spawn) ----------

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (ActiveSpawnPrefab == null) return;

        Event currentEvent = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

        Vector3 targetPoint;
        Vector3 surfaceNormal = Vector3.up;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
            surfaceNormal = hit.normal;
        }
        else
        {
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            targetPoint = groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : ray.GetPoint(15f);
        }

        Handles.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        Handles.DrawWireDisc(targetPoint, surfaceNormal, 0.6f);
        Handles.DrawDottedLine(targetPoint, targetPoint + surfaceNormal * 0.8f, 2f);

        var textStyle = new GUIStyle
        {
            normal = { textColor = Color.cyan },
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        Handles.Label(targetPoint + surfaceNormal * 0.9f, $"[B / Shift+Tık]: {ActiveSpawnPrefab.name}", textStyle);

        bool isShiftClick = currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && currentEvent.shift;
        bool isBKeyPressed = currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.B;

        if (isShiftClick || isBKeyPressed)
        {
            SpawnPrefab(ActiveSpawnPrefab, targetPoint);
            currentEvent.Use();
        }

        if (currentEvent.type == EventType.MouseMove)
            sceneView.Repaint();
    }

    private static void SpawnPrefab(GameObject prefab, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;

        Undo.RegisterCreatedObjectUndo(instance, $"Spawn {prefab.name}");
        Selection.activeGameObject = instance;
    }
}

/// <summary>
/// Quick Asset Shelf penceresi: arama, sabitlenenler + son kullanılanlar listesi, damga kontrolü.
/// Ayarlar ayrı bir pencerededir (<see cref="QuickAssetShelfSettings"/>).
/// </summary>
public class QuickAssetShelf : EditorWindow
{
    private const string PrefFilter = "QuickAssetShelf_Filter";
    private const string PrefSearch = "QuickAssetShelf_Search";

    private static readonly string[] FilterOptions = { "Tümü", "Prefab", "SO", "Materyal" };

    private Vector2 _scrollPos;
    private int _filterIndex;
    private string _search = "";

    [MenuItem("Tools/Quick Asset Shelf")]
    public static void OpenWindow()
    {
        var window = GetWindow<QuickAssetShelf>("Asset Shelf", typeof(SceneView));
        window.minSize = new Vector2(240, 200);
    }

    public static void RepaintWindow()
    {
        if (HasOpenInstances<QuickAssetShelf>())
            GetWindow<QuickAssetShelf>().Repaint();
    }

    private void OnEnable()
    {
        _filterIndex = Mathf.Clamp(EditorPrefs.GetInt(PrefFilter, 0), 0, FilterOptions.Length - 1);
        _search = EditorPrefs.GetString(PrefSearch, "");
    }

    private void OnDisable()
    {
        EditorPrefs.SetInt(PrefFilter, _filterIndex);
        EditorPrefs.SetString(PrefSearch, _search);
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawActiveStampBanner();
        DrawItemList();
    }

    /// <summary>Aktif filtrenin karşılık geldiği tür; "Tümü" seçiliyse null.</summary>
    private ShelfAssetKind? SelectedFilterKind()
    {
        switch (_filterIndex)
        {
            case 1: return ShelfAssetKind.Prefab;
            case 2: return ShelfAssetKind.ScriptableObject;
            case 3: return ShelfAssetKind.Material;
            default: return null;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        bool listening = QuickAssetShelfService.AutoRecord;
        GUI.color = listening ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button(listening ? "● Dinliyor" : "○ Duraklatıldı", EditorStyles.toolbarButton, GUILayout.Width(80)))
            QuickAssetShelfService.AutoRecord = !listening;
        GUI.color = Color.white;

        _filterIndex = EditorGUILayout.Popup(_filterIndex, FilterOptions, EditorStyles.toolbarPopup, GUILayout.Width(66));

        EditorGUI.BeginChangeCheck();
        _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField);
        if (EditorGUI.EndChangeCheck()) Repaint();

        if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.toolbarButton)
            && !string.IsNullOrEmpty(_search))
        {
            _search = "";
            GUIUtility.keyboardControl = 0;
            Repaint();
        }

        // Seçili türdeki tüm kayıtları temizle
        ShelfAssetKind? filterKind = SelectedFilterKind();
        using (new EditorGUI.DisabledScope(filterKind == null))
        {
            var clearContent = new GUIContent(
                "🗑",
                filterKind == null
                    ? "Temizlemek için önce bir tür seçin (Prefab / SO / Materyal)."
                    : $"Raftaki tüm {QuickAssetShelfService.KindLabel(filterKind.Value)} kayıtlarını temizle.");
            if (GUILayout.Button(clearContent, EditorStyles.toolbarButton, GUILayout.Width(28)))
                ClearSelectedKind(filterKind.Value);
        }

        if (GUILayout.Button(new GUIContent("⚙", "Ayarlar penceresini aç"), EditorStyles.toolbarButton, GUILayout.Width(24)))
            QuickAssetShelfSettings.OpenWindow();

        EditorGUILayout.EndHorizontal();
    }

    private void ClearSelectedKind(ShelfAssetKind kind)
    {
        string label = QuickAssetShelfService.KindLabel(kind);
        bool confirm = EditorUtility.DisplayDialog(
            "Quick Asset Shelf",
            $"Raftaki tüm {label} kayıtları (sabitlenenler dahil) kaldırılacak.\n\nProject asset'leri silinmez, yalnızca raf temizlenir. Emin misiniz?",
            "Temizle",
            "Vazgeç");

        if (!confirm) return;

        int removed = QuickAssetShelfService.ClearKind(kind);
        Debug.Log($"[QuickAssetShelf] {removed} adet {label} kaydı raftan temizlendi.");
        Repaint();
    }

    private void DrawActiveStampBanner()
    {
        if (QuickAssetShelfService.ActiveSpawnPrefab == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        GUI.color = new Color(0.4f, 0.9f, 1f);
        EditorGUILayout.LabelField($"🎯 Aktif Damga: <b>{QuickAssetShelfService.ActiveSpawnPrefab.name}</b>", titleStyle);
        GUI.color = Color.white;

        EditorGUILayout.LabelField("Scene View'da <b>Shift + Sol Tık</b> veya <b>'B'</b> tuşu ile yerleştir.",
            new GUIStyle(EditorStyles.miniLabel) { richText = true });

        if (GUILayout.Button("Damga Modunu Kapat", EditorStyles.miniButton))
            QuickAssetShelfService.ActiveSpawnPrefab = null;

        EditorGUILayout.EndVertical();
    }

    private void DrawItemList()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        QuickAssetShelfService.PruneInvalid();

        var pinned = Query(QuickAssetShelfService.PinnedGuids);
        var recents = Query(QuickAssetShelfService.RecentGuids.Where(g => !QuickAssetShelfService.IsPinned(g)));

        if (pinned.Count == 0 && recents.Count == 0)
        {
            EditorGUILayout.Space(15);
            string message;
            if (QuickAssetShelfService.AllKindsDisabled)
                message = "Tüm türler ayarlardan kapatılmış. Hiçbir asset rafa kaydedilmiyor.";
            else if (!string.IsNullOrEmpty(_search))
                message = "Aramayla eşleşen asset yok.";
            else
                message = "Arka plan dinlemede. Project panelinden Prefab, ScriptableObject veya Material seçtiğinizde buraya kaydedilir.";

            EditorGUILayout.HelpBox(message, MessageType.Info);
            if (QuickAssetShelfService.AllKindsDisabled &&
                GUILayout.Button("Ayarları Aç", EditorStyles.miniButton))
                QuickAssetShelfSettings.OpenWindow();
        }
        else
        {
            DrawSection("📌 Sabitlenenler", pinned, true);
            DrawSection("🕘 Son Kullanılanlar", recents, false);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSection(string title, List<(string guid, UnityEngine.Object item)> items, bool pinnedSection)
    {
        if (items.Count == 0) return;

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField($"{title} ({items.Count})", EditorStyles.miniBoldLabel);
        foreach (var (guid, item) in items)
            DrawItemRow(item, guid, pinnedSection);
    }

    private void DrawItemRow(UnityEngine.Object item, string guid, bool pinned)
    {
        ShelfAssetKind? kind = QuickAssetShelfService.GetKind(item);
        if (kind == null) return;

        bool isStamp = QuickAssetShelfService.ActiveSpawnPrefab == item;

        if (isStamp) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(26));
        GUI.backgroundColor = Color.white;

        // Çift tıklama bilgisini satır çizilmeden önce yakala (butonlar event'i tüketebilir)
        Event evt = Event.current;
        bool doubleClick = evt != null && evt.type == EventType.MouseDown && evt.clickCount == 2;
        Vector2 mousePosition = evt != null ? evt.mousePosition : Vector2.zero;

        GUILayout.Label(new GUIContent(AssetPreview.GetMiniThumbnail(item)), GUILayout.Width(20), GUILayout.Height(20));

        string badge;
        switch (kind.Value)
        {
            case ShelfAssetKind.Prefab: badge = "<color=#88CCFF>[Prefab]</color>"; break;
            case ShelfAssetKind.ScriptableObject: badge = "<color=#FFD700>[SO]</color>"; break;
            default: badge = "<color=#C39BFF>[Materyal]</color>"; break;
        }

        var labelStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true, alignment = TextAnchor.MiddleLeft };

        if (GUILayout.Button($"<b>{item.name}</b> {badge}", labelStyle, GUILayout.Height(20)))
        {
            EditorGUIUtility.PingObject(item);
            Selection.activeObject = item;
        }

        if (kind.Value == ShelfAssetKind.Prefab)
        {
            GUI.color = isStamp ? Color.cyan : Color.white;
            if (GUILayout.Button(isStamp ? "🎯 Hazır" : "Damgala", EditorStyles.miniButton, GUILayout.Width(58), GUILayout.Height(18)))
                QuickAssetShelfService.ActiveSpawnPrefab = isStamp ? null : (GameObject)item;
            GUI.color = Color.white;
        }

        GUI.color = pinned ? new Color(1f, 0.85f, 0.3f) : Color.white;
        if (GUILayout.Button(pinned ? "📌" : "📍", EditorStyles.miniButton, GUILayout.Width(24), GUILayout.Height(18)))
            QuickAssetShelfService.TogglePin(guid);
        GUI.color = Color.white;

        if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(18)))
        {
            QuickAssetShelfService.Remove(guid);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();

        if (doubleClick && rowRect.Contains(mousePosition))
        {
            AssetDatabase.OpenAsset(item);
            evt.Use();
        }

        HandleDragDrop(rowRect, item);
    }

    private void HandleDragDrop(Rect rect, UnityEngine.Object target)
    {
        Event evt = Event.current;
        if (evt.type == EventType.MouseDrag && rect.Contains(evt.mousePosition))
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { target };
            DragAndDrop.StartDrag(target.name);
            evt.Use();
        }
    }

    /// <summary>GUID listesini yüklenebilir asset'lere çevirir; tür takibi, filtre ve aramayı uygular.</summary>
    private List<(string guid, UnityEngine.Object item)> Query(IEnumerable<string> guids)
    {
        var result = new List<(string, UnityEngine.Object)>();
        ShelfAssetKind? selected = SelectedFilterKind();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (QuickAssetShelfService.IsIgnored(path)) continue;

            var item = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (item == null) continue;

            ShelfAssetKind? kind = QuickAssetShelfService.GetKind(item);
            if (kind == null || !QuickAssetShelfService.IsKindTracked(kind.Value)) continue;
            if (selected != null && kind.Value != selected.Value) continue;

            if (!string.IsNullOrEmpty(_search) &&
                item.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            result.Add((guid, item));
        }

        return result;
    }
}

/// <summary>
/// Quick Asset Shelf ayarları: dinleme, izlenen türler, geçmiş sınırı, yoksayılan klasörler ve veri temizleme.
/// </summary>
public class QuickAssetShelfSettings : EditorWindow
{
    private const string PrefShowIgnored = "QuickAssetShelf_SettingsShowIgnored";

    private Vector2 _scrollPos;
    private bool _showIgnored = true;

    [MenuItem("Tools/Quick Asset Shelf/Ayarlar")]
    public static void OpenWindow()
    {
        var window = GetWindow<QuickAssetShelfSettings>("Shelf Ayarları");
        window.minSize = new Vector2(320, 320);
    }

    public static void RepaintWindow()
    {
        if (HasOpenInstances<QuickAssetShelfSettings>())
            GetWindow<QuickAssetShelfSettings>().Repaint();
    }

    private void OnEnable()
    {
        _showIgnored = EditorPrefs.GetBool(PrefShowIgnored, true);
    }

    private void OnDisable()
    {
        EditorPrefs.SetBool(PrefShowIgnored, _showIgnored);
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        DrawRecording();
        DrawTrackedTypes();
        DrawHistoryLimit();
        DrawIgnoredFolders();
        DrawDangerZone();

        EditorGUILayout.EndScrollView();
    }

    private void DrawRecording()
    {
        EditorGUILayout.LabelField("Dinleme", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        bool auto = QuickAssetShelfService.AutoRecord;
        bool newAuto = EditorGUILayout.ToggleLeft("Otomatik kaydet (arka planda dinle)", auto);

        if (newAuto != auto)
        {
            QuickAssetShelfService.AutoRecord = newAuto;
            QuickAssetShelf.RepaintWindow();
        }

        EditorGUILayout.LabelField(
            "Açıkken Project panelinden seçilen uygun türdeki asset'ler rafa kaydedilir.",
            new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
    }

    private void DrawTrackedTypes()
    {
        EditorGUILayout.LabelField("İzlenen Türler", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Kapatılan türler rafa hiç kaydedilmez ve mevcut kayıtları listede görünmez.",
            new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
        EditorGUILayout.Space(2);

        DrawKindToggle("Prefab", ShelfAssetKind.Prefab);
        DrawKindToggle("ScriptableObject", ShelfAssetKind.ScriptableObject);
        DrawKindToggle("Material", ShelfAssetKind.Material);

        if (QuickAssetShelfService.AllKindsDisabled)
            EditorGUILayout.HelpBox("Tüm türler kapalı: raf hiçbir şey kaydetmez.", MessageType.Warning);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
    }

    private void DrawKindToggle(string label, ShelfAssetKind kind)
    {
        bool tracked = QuickAssetShelfService.IsKindTracked(kind);
        bool newValue = EditorGUILayout.ToggleLeft(label, tracked);
        if (newValue != tracked)
            QuickAssetShelfService.SetKindTracked(kind, newValue);
    }

    private void DrawHistoryLimit()
    {
        EditorGUILayout.LabelField("Geçmiş Sınırı", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        int limit = QuickAssetShelfService.HistoryLimit;
        int newLimit = EditorGUILayout.IntSlider(limit, QuickAssetShelfService.MinHistoryLimit, QuickAssetShelfService.MaxHistoryLimit);
        if (newLimit != limit)
            QuickAssetShelfService.HistoryLimit = newLimit;

        EditorGUILayout.LabelField($"Son kullanılanlar en fazla {newLimit} kayıt tutar.",
            new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
    }

    private void DrawIgnoredFolders()
    {
        int count = QuickAssetShelfService.IgnoredFolders.Count;
        _showIgnored = EditorGUILayout.Foldout(_showIgnored, $"🚫 Yoksayılan Klasörler ({count})", true);
        if (!_showIgnored) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "Bu klasörlerin altındaki asset'ler rafa kaydedilmez ve listede görünmez.",
            new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Seçili klasörü ekle", EditorStyles.miniButton))
            {
                if (QuickAssetShelfService.AddIgnoredFolderFromSelection())
                    Repaint();
                else
                    Debug.LogWarning("[QuickAssetShelf] Klasör eklenemedi. Project panelinden bir klasör seçin.");
            }

            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button("Tümünü temizle", EditorStyles.miniButton, GUILayout.Width(110)))
                {
                    QuickAssetShelfService.ClearIgnoredFolders();
                    Repaint();
                }
            }
        }

        for (int i = QuickAssetShelfService.IgnoredFolders.Count - 1; i >= 0; i--)
        {
            string folder = QuickAssetShelfService.IgnoredFolders[i];

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button(folder, EditorStyles.miniLabel))
                {
                    var folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
                    if (folderObj != null)
                    {
                        EditorGUIUtility.PingObject(folderObj);
                        Selection.activeObject = folderObj;
                    }
                }

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    QuickAssetShelfService.RemoveIgnoredFolder(folder);
                    GUIUtility.ExitGUI();
                }
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawDangerZone()
    {
        EditorGUILayout.LabelField("Veri", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(QuickAssetShelfService.RecentGuids.Count == 0))
            {
                if (GUILayout.Button("Son Kullanılanları Temizle", EditorStyles.miniButton))
                {
                    QuickAssetShelfService.ClearRecents();
                    Repaint();
                }
            }

            if (GUILayout.Button("Tüm Verileri Temizle", EditorStyles.miniButton))
            {
                if (EditorUtility.DisplayDialog(
                        "Quick Asset Shelf",
                        "Raf kayıtları, sabitler, yoksayılan klasörler ve tüm ayarlar sıfırlanacak. Project asset'leri silinmez.\n\nDevam edilsin mi?",
                        "Temizle",
                        "Vazgeç"))
                {
                    QuickAssetShelfService.ClearAll();
                    Repaint();
                }
            }
        }

        EditorGUILayout.LabelField("Tüm veriler EditorPrefs altında 'QuickAssetShelf_*' anahtarlarında saklanır.",
            new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
        EditorGUILayout.EndVertical();
    }
}

/// <summary>Project paneli sağ tık menüsü: seçili klasörü rafın yoksay listesine ekler/çıkarır.</summary>
internal static class QuickAssetShelfContextMenu
{
    private const string MenuPath = "Assets/Quick Asset Shelf/";
    private const string AddIgnore = MenuPath + "Yoksayılan Klasörlere Ekle";
    private const string RemoveIgnore = MenuPath + "Yoksayılan Klasörlerden Çıkar";

    [MenuItem(AddIgnore, true)]
    private static bool ValidateAddIgnore()
        => GetSelectedFolder() != null;

    [MenuItem(AddIgnore)]
    private static void AddIgnoreToSelection()
    {
        string folder = GetSelectedFolder();
        if (folder == null) return;

        if (QuickAssetShelfService.AddIgnoredFolder(folder))
        {
            Debug.Log($"[QuickAssetShelf] Yoksayılan klasörlere eklendi: {folder}");
            QuickAssetShelf.RepaintWindow();
            QuickAssetShelfSettings.RepaintWindow();
        }
    }

    [MenuItem(RemoveIgnore, true)]
    private static bool ValidateRemoveIgnore()
    {
        string folder = GetSelectedFolder();
        return folder != null && QuickAssetShelfService.IgnoredFolders.Any(
            f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
    }

    [MenuItem(RemoveIgnore)]
    private static void RemoveIgnoreFromSelection()
    {
        string folder = GetSelectedFolder();
        if (folder == null) return;

        if (QuickAssetShelfService.RemoveIgnoredFolder(folder))
        {
            Debug.Log($"[QuickAssetShelf] Yoksayılan klasörlerden çıkarıldı: {folder}");
            QuickAssetShelf.RepaintWindow();
            QuickAssetShelfSettings.RepaintWindow();
        }
    }

    private static string GetSelectedFolder()
    {
        var selected = Selection.activeObject;
        if (selected == null) return null;

        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path)) return null;

        if (AssetDatabase.IsValidFolder(path)) return path;

        string parent = path.Contains('/') ? path.Substring(0, path.LastIndexOf('/')) : null;
        return !string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent) ? parent : null;
    }
}
#endif
