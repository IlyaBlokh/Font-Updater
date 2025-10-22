using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Editor
{
  public class FontReplacer : EditorWindow
  {
    private readonly List<TextComponentInfo> _foundTexts = new();
    private TMP_FontAsset _newFont;
    private DefaultAsset _rootFolder;
    private Vector2 _scrollPosition;
    private string _rootFolderPath = "Assets";
    private bool _hasSearched;
    
    [System.Serializable]
    private class TextComponentInfo
    {
      public string prefabPath;
      public string hierarchyPath;
      public string currentFontName;
      public bool isSelected = true;
      
      public TextComponentInfo(string prefabPath, string hierarchyPath, TMP_FontAsset font)
      {
        this.prefabPath = prefabPath;
        this.hierarchyPath = hierarchyPath;
        currentFontName = font != null ? font.name : "None";
      }
    }
    
    [MenuItem("Tools/Font Replacer")]
    public static void ShowWindow()
    {
      GetWindow<FontReplacer>("Font Replacer");
    }

    private void OnGUI()
    {
      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
      
      GUILayout.Label("Replace Font in Prefabs", EditorStyles.boldLabel);
      GUILayout.Space(10);
      
      EditorGUILayout.LabelField("Select New Font Asset:", EditorStyles.label);
      _newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(_newFont, typeof(TMP_FontAsset), false);
      
      GUILayout.Space(10);
      
      EditorGUILayout.LabelField("Select Root Folder with Prefabs:", EditorStyles.label);
      DefaultAsset newFolder = (DefaultAsset)EditorGUILayout.ObjectField(_rootFolder, typeof(DefaultAsset), false);
      
      if (newFolder != _rootFolder)
      {
        _rootFolder = newFolder;
        if (_rootFolder != null)
        {
          _rootFolderPath = AssetDatabase.GetAssetPath(_rootFolder);
        }
      }
      
      if (_rootFolder != null)
      {
        EditorGUILayout.LabelField("Selected Path:", _rootFolderPath);
      }
      
      GUILayout.Space(20);
      
      GUI.enabled = _rootFolder != null;
      
      if (GUILayout.Button("Search Texts", GUILayout.Height(30)))
      {
        SearchTextsInPrefabs();
      }
      
      GUI.enabled = true;
      
      GUILayout.Space(20);
      
      if (_hasSearched)
      {
        if (_foundTexts.Count > 0)
        {
          EditorGUILayout.LabelField($"Found {_foundTexts.Count} TextMeshPro component(s):", EditorStyles.boldLabel);
          GUILayout.Space(10);
          
          EditorGUILayout.BeginHorizontal();
          if (GUILayout.Button("Select All"))
          {
            foreach (TextComponentInfo text in _foundTexts)
            {
              text.isSelected = true;
            }
          }
          if (GUILayout.Button("Deselect All"))
          {
            foreach (TextComponentInfo text in _foundTexts)
            {
              text.isSelected = false;
            }
          }
          EditorGUILayout.EndHorizontal();
          
          GUILayout.Space(10);
          
          EditorGUILayout.BeginVertical("box");
          
          foreach (TextComponentInfo textInfo in _foundTexts)
          {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            textInfo.isSelected = EditorGUILayout.Toggle(textInfo.isSelected, GUILayout.Width(20));
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Prefab: {textInfo.prefabPath}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Hierarchy: {textInfo.hierarchyPath}");
            EditorGUILayout.LabelField($"Current Font: {textInfo.currentFontName}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(5);
          }
          
          EditorGUILayout.EndVertical();
          
          GUILayout.Space(20);
          
          GUI.enabled = _newFont != null && _foundTexts.Exists(t => t.isSelected);
          
          int selectedCount = _foundTexts.FindAll(t => t.isSelected).Count;
          if (GUILayout.Button($"Replace Font ({selectedCount} selected)", GUILayout.Height(30))) 
            ReplaceFontsInSelectedTexts();
          
          GUI.enabled = true;
        }
        else
        {
          EditorGUILayout.HelpBox("No TextMeshPro components found in the selected folder.", MessageType.Info);
        }
      }
      
      EditorGUILayout.EndScrollView();
    }

    private void SearchTextsInPrefabs()
    {
      if (_rootFolder == null)
      {
        EditorUtility.DisplayDialog("Error", "Please select a root folder.", "OK");
        return;
      }

      _foundTexts.Clear();
      _hasSearched = false;

      string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { _rootFolderPath });
      
      if (prefabGuids.Length == 0)
      {
        EditorUtility.DisplayDialog("No Prefabs Found", "No prefabs found in the selected folder.", "OK");
        _hasSearched = true;
        return;
      }

      try
      {
        for (int i = 0; i < prefabGuids.Length; i++)
        {
          string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

          if (EditorUtility.DisplayCancelableProgressBar(
                "Searching Texts",
                $"Processing: {Path.GetFileName(prefabPath)} ({i + 1}/{prefabGuids.Length})",
                (float)i / prefabGuids.Length))
          {
            break;
          }

          SearchInPrefab(prefabPath);
        }
      }
      finally
      {
        EditorUtility.ClearProgressBar();
        _hasSearched = true;
      }

      Debug.Log($"Font Replacer: Found {_foundTexts.Count} TextMeshPro components in {prefabGuids.Length} prefab(s).");
    }

    private void SearchInPrefab(string prefabPath)
    {
      GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      
      if (prefabRoot == null)
        return;

      GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
      
      if (prefabInstance == null)
        return;

      try
      {
        TMP_Text[] tmpTexts = prefabInstance.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text textComponent in tmpTexts)
        {
          string hierarchyPath = GetHierarchyPath(textComponent.transform, prefabInstance.transform);
          var info = new TextComponentInfo(prefabPath, hierarchyPath, textComponent.font);
          _foundTexts.Add(info);
        }
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(prefabInstance);
      }
    }

    private string GetHierarchyPath(Transform transform, Transform root)
    {
      if (transform == root)
        return transform.name;

      string path = transform.name;
      Transform current = transform.parent;

      while (current != null && current != root)
      {
        path = current.name + "/" + path;
        current = current.parent;
      }

      if (current == root) 
        path = root.name + "/" + path;

      return path;
    }

    private void ReplaceFontsInSelectedTexts()
    {
      if (_newFont == null)
      {
        EditorUtility.DisplayDialog("Error", "Please select a font asset.", "OK");
        return;
      }

      List<TextComponentInfo> selectedTexts = _foundTexts.FindAll(t => t.isSelected);
      
      if (selectedTexts.Count == 0)
      {
        EditorUtility.DisplayDialog("Error", "No texts selected for replacement.", "OK");
        return;
      }

      Dictionary<string, List<TextComponentInfo>> textsByPrefab = new Dictionary<string, List<TextComponentInfo>>();
      
      foreach (TextComponentInfo textInfo in selectedTexts)
      {
        if (!textsByPrefab.ContainsKey(textInfo.prefabPath))
        {
          textsByPrefab[textInfo.prefabPath] = new List<TextComponentInfo>();
        }
        textsByPrefab[textInfo.prefabPath].Add(textInfo);
      }

      int totalReplacements = 0;
      int processedPrefabs = 0;
      List<string> modifiedPrefabs = new List<string>();

      try
      {
        AssetDatabase.StartAssetEditing();

        int current = 0;
        foreach (KeyValuePair<string, List<TextComponentInfo>> kvp in textsByPrefab)
        {
          string prefabPath = kvp.Key;
          List<TextComponentInfo> textsInPrefab = kvp.Value;

          if (EditorUtility.DisplayCancelableProgressBar(
                "Replacing Fonts",
                $"Processing: {Path.GetFileName(prefabPath)} ({current + 1}/{textsByPrefab.Count})",
                (float)current / textsByPrefab.Count))
          {
            break;
          }

          int replacements = ProcessPrefabForReplacement(prefabPath, textsInPrefab);
          if (replacements > 0)
          {
            totalReplacements += replacements;
            processedPrefabs++;
            modifiedPrefabs.Add(prefabPath);
          }
          
          current++;
        }
      }
      finally
      {
        AssetDatabase.StopAssetEditing();
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
      }

      string message = $"Font replacement complete!\n\n" +
                       $"Prefabs modified: {processedPrefabs}\n" +
                       $"Total TextMeshPro components updated: {totalReplacements}\n\n" +
                       $"New font: {_newFont.name}";
      
      EditorUtility.DisplayDialog("Success", message, "OK");
      
      Debug.Log($"Font Replacer: Modified {processedPrefabs} prefabs with {totalReplacements} font replacements.");
      foreach (string path in modifiedPrefabs) 
        Debug.Log($"  - {path}");
      
      SearchTextsInPrefabs();
    }

    private int ProcessPrefabForReplacement(string prefabPath, List<TextComponentInfo> textsToReplace)
    {
      GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      
      if (prefabRoot == null)
        return 0;

      GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
      
      if (prefabInstance == null)
        return 0;

      int replacementCount = 0;
      try
      {
        TMP_Text[] tmpTexts = prefabInstance.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text textComponent in tmpTexts)
        {
          string hierarchyPath = GetHierarchyPath(textComponent.transform, prefabInstance.transform);
          bool shouldReplace = textsToReplace.Exists(t => t.hierarchyPath == hierarchyPath);
          
          if (shouldReplace && textComponent.font != _newFont)
          {
            textComponent.font = _newFont;
            replacementCount++;
          }
        }

        if (replacementCount > 0) 
          PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
      }
      finally
      {
        PrefabUtility.UnloadPrefabContents(prefabInstance);
      }

      return replacementCount;
    }
  }
}