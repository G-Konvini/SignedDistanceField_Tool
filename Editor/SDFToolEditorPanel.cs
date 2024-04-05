// Copyright (c) 2024.4 G-Konvini. All rights reserved
// Author: Takeshi

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using G_Konvini.SDFTools.Editor.CombineSDF;
using G_Konvini.SDFTools.Editor.GPUSaito1994;
using G_Konvini.SDFTools.Editor.PreviewShader;
using G_Konvini.SDFTools.Editor.ImageIO;

namespace G_Konvini.SDFTools.Editor
{
    internal class SDFToolEditorPanel : EditorWindow
    {
        [MenuItem("Tools/Signed Distance Field Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<SDFToolEditorPanel>();
            window.titleContent = new GUIContent("Signed Distance Field Tool");
            window.minSize = new Vector2(800, 590);
            window.Show();
        }
        
        private EditorCacheData _data;
        private PreviewDriver _previewDriver = new PreviewDriver();
        private CombineSDFDriver _combineSDFDriver = new CombineSDFDriver();
        private float _sdfScale;
        public float SDFScale { get => _sdfScale; set => _sdfScale = Mathf.Clamp(value, 1, 10); }
        
        private Vector2 _rawClipsScroll;
        private int _singleSdfPreviewIdx;
        
        enum GenerateMode
        {
            CombinedSDF,
            PerSingleSDF
        }
        
        private GenerateMode _mode;
        private string[] _modeTiles = { "Combined SDF", "Per Single SDF" };
        
        
        private GUIContent _saveButtonContent;
        private GUIContent _saveAllButtonContent;
        private GUIContent _importGUIContent;
        private GUIContent _discardContent;
        private GUIContent _stepViewContent;
        private GUIContent _sectionSettingsContent;
        private GUIContent _sdfSettingsContent;
        private GUIContent _resetContent;

        private void OnEnable()
        {
            Init();
            _saveButtonContent = new GUIContent("Export", EditorGUIUtility.IconContent("SaveActive").image);
            _saveAllButtonContent = new GUIContent("Export All", EditorGUIUtility.IconContent("SaveActive").image);
            _importGUIContent = new GUIContent(" Import Selected Sections ", EditorGUIUtility.IconContent("SceneLoadIn").image);
            _discardContent = new GUIContent("Discard",EditorGUIUtility.IconContent("d_TreeEditor.Trash").image);
            _stepViewContent = new GUIContent(EditorGUIUtility.IconContent("d_scenevis_visible_hover").image);
            _sectionSettingsContent = new GUIContent("Section Settings", EditorGUIUtility.IconContent("_Popup").image);
            _sdfSettingsContent = new GUIContent("SDF Settings", EditorGUIUtility.IconContent("_Popup").image);
            _resetContent = new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Refresh").image);

            EditorApplication.update += RenderPreview;
        }

        private void OnDisable()
        {
            Clear();
            EditorApplication.update -= RenderPreview;
        }

        void Init()
        {
            _data = ScriptableObject.CreateInstance<EditorCacheData>();
        }

        void Clear()
        {
            _data.ClearAll();
        }
        
        
        private void OnGUI()
        {
            using (var bar = new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new GUILayout.HorizontalScope(GUILayout.Width(516)))
                {
                    DrawExportButton();
                }
                
                DrawImportButton();
                DrawDiscardButton();
                        
            }
            
            GUILayout.Space(2);
            
            using (var toolArea = new GUILayout.HorizontalScope())
            {
                using (var previewArea = new GUILayout.VerticalScope(GUILayout.Width(512)))
                {
                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        var mode = (GenerateMode)GUILayout.Toolbar((int)_mode, _modeTiles);
                        if (_mode != mode)
                        {
                            _data.ClearWarningsAndMassages();
                            _mode = mode;
                        }
                    }
                    
                    GUI.color = Color.black;
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUI.color = Color.white;
                        
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(_stepViewContent,GUILayout.Width(20));
                            _data.StepView = GUILayout.Toggle(_data.StepView, "Step View", GUILayout.Width(80)) ;
                            
                            if (_data.StepView)
                            {
                                _data.step = EditorGUILayout.Slider(_data.step, 0, 1);
                            }
                        }
                        
                        DrawMessageAndWarning();
                        
                        DrawPreview();
                        
                        GUILayout.FlexibleSpace();
                        
                        DrawPerSingleSDFPageSlider();
                    }
                    
                }
                
                using (var settingsArea = new GUILayout.VerticalScope())
                {
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox,GUILayout.MinHeight(100), GUILayout.MaxHeight(float.MaxValue)))
                    {
                        GUILayout.Label(_sectionSettingsContent, EditorStyles.boldLabel);
                        GUILayout.Space(5);

                        if (_data.inputWarning != null)
                        {
                            EditorGUILayout.HelpBox(_data.inputWarning, MessageType.Warning);
                        }
                        
                        if (_data.sectionList is { Count: > 0 })
                        {
                            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                            {
                                GUILayout.Label("ID", GUILayout.Width(18));
                                GUILayout.Label("Image", GUILayout.Width(37));
                                if (_mode == GenerateMode.CombinedSDF && _data.sdfList is {Count: > 0})
                                {
                                    GUILayout.Label("Time");
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button(_resetContent,GUILayout.Width(55)))
                                    {
                                        _data.ResetFramePositions();
                                    }
                                }
                                else if (_mode == GenerateMode.PerSingleSDF)
                                {
                                    GUILayout.Label("Name");
                                }
                                else
                                {
                                    GUILayout.FlexibleSpace();
                                }
                            }
                            
                            using (var sectionArea = new GUILayout.ScrollViewScope(_rawClipsScroll))
                            {
                                _rawClipsScroll = sectionArea.scrollPosition;
                                for (var i = 0; i < _data.sectionList.Count; i++)
                                {
                                    DrawSingleSectionItem(i);
                                }
                            }
                        }
                    }
                    
                    var lastRect = GUILayoutUtility.GetLastRect();
                    var rect = new Rect(lastRect.min.x, lastRect.min.y,lastRect.max.x, lastRect.max.y);
                    var e = Event.current;
                    switch (e.type)
                    {
                        case EventType.DragUpdated or EventType.DragPerform:
                        {
                            if (rect.Contains(e.mousePosition))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            }
                            break;
                        }
                        case EventType.DragExited when rect.Contains(e.mousePosition):
                            ImageImporter.ImportImage(_data);
                            break;
                    }

                    GUILayout.Space(2);
                    
                    
                    if (_mode == GenerateMode.PerSingleSDF)
                    {
                        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            GUILayout.Label(_sdfSettingsContent, EditorStyles.boldLabel);
                            GUILayout.Space(10);
                            SDFScale = EditorGUILayout.DelayedFloatField("Distance Scale", SDFScale);
                        }
                        
                        RegenerateSDF(SDFScale);
                    }
                    else if (_mode == GenerateMode.CombinedSDF)
                    {
                        RegenerateSDF(1);
                    } 
                    GUILayout.Space(2);
                    

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Generate SDF"))
                    {
                        _data.ClearGeneratedData();
                        GenerateSDF(_data);
                    }

                    if (_mode == GenerateMode.CombinedSDF)
                    {
                        CombineSDF(_data, _combineSDFDriver);
                    }
                    
                }
            }

        }

        private void RegenerateSDF(float scale)
        {
            if (Math.Abs(_data.DistacneScale - scale) > 0)
            {
                _data.DistacneScale = scale;
                _data.ClearGeneratedData();
                GenerateSDF(_data);
            }
        }

        private void DrawDiscardButton()
        {
            if (GUILayout.Button(_discardContent, EditorStyles.toolbarButton))
            {
                Clear();
                Init();
            }
        }

        private void DrawImportButton()
        {
            if (GUILayout.Button(_importGUIContent, EditorStyles.toolbarButton, GUILayout.Width(180)))
            {
                ImageImporter.ImportImage(_data);
            }
        }

        private void DrawExportButton()
        {
            if (_mode == GenerateMode.CombinedSDF)
            {
                        
                if (GUILayout.Button(_saveButtonContent, EditorStyles.toolbarButton, GUILayout.Width(80)))
                    ImageExporter.ExportImage(_data);
            }
            else if (_mode == GenerateMode.PerSingleSDF)
            {
                if (GUILayout.Button(_saveAllButtonContent, EditorStyles.toolbarButton, GUILayout.Width(80)))
                    ImageExporter.SavePerSingleImages(_data);
            }
        }

        private void DrawMessageAndWarning()
        {
            if (!string.IsNullOrEmpty(_data.warnings))
                EditorGUILayout.HelpBox(_data.warnings,MessageType.Warning);

            if (!string.IsNullOrEmpty(_data.messages))
                EditorGUILayout.HelpBox(_data.messages,MessageType.Info);
        }

        private void DrawPreview()
        {
            if (_data.preview)
            {
                var rect = GUILayoutUtility.GetRect(0, 512f, 0, float.MaxValue);
                GUI.DrawTexture(rect, _data.preview, ScaleMode.ScaleToFit);
            }
        }

        private void RenderPreview()
        {
            
            if (_mode == GenerateMode.CombinedSDF)
            {
                RenderCombinedSDFPreview();
            }
            else if (_mode == GenerateMode.PerSingleSDF)
            {
                RenderPerSingleSDFPreview();
            }
        }
        
        private void RenderCombinedSDFPreview()
        {
            if (_data.result)
            {
                _previewDriver.Setup(_data.result,_data.StepView,_data.step);
                _previewDriver.Execute(ref _data.preview);
                
            }
            else if (_data.preview)
            {
                _data.preview.Release();
            }
            
        }
        
        private void RenderPerSingleSDFPreview()
        {
            
            if (_data.sdfList != null && _data.sdfList[_singleSdfPreviewIdx] is RenderTexture sdf)
            {
                _previewDriver.Setup(sdf,_data.StepView,_data.step);
                _previewDriver.Execute(ref _data.preview);
            }
        }
        
        private void DrawPerSingleSDFPageSlider()
        {
            if (_mode == GenerateMode.PerSingleSDF)
            {
                using (new GUILayout.HorizontalScope())
                { 
                    if (_data.sdfList is {Count: > 0})
                    {
                        GUILayout.Label("Page",GUILayout.Width(35));
                        _singleSdfPreviewIdx = EditorGUILayout.IntSlider(_singleSdfPreviewIdx, 0, _data.sectionList.Count - 1);
                    }
                }
            }
        }
        
        private void DrawSingleSectionItem(int idx)
        {
            var sectionImage = _data.sectionList[idx];
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(idx.ToString(), GUILayout.Height(20), GUILayout.Width(25));
                GUILayout.Label(sectionImage, GUILayout.Height(20), GUILayout.Width(32));
                if (_mode == GenerateMode.CombinedSDF && _data.framePositions != null && _data.sectionList.Count == _data.framePositions.Count)
                {
                    float pos = EditorGUILayout.Slider(_data.framePositions[idx], 0, 1,
                        GUILayout.Height(20));
                    if (Math.Abs(pos - _data.framePositions[idx]) > 0)
                    {
                        Undo.RecordObject(_data, "framePositions Change");
                        _data.framePositions[idx] = pos;
                    }
                }
                else if (_mode == GenerateMode.PerSingleSDF)
                {
                    GUILayout.Label(sectionImage.name, GUILayout.Height(20));
                }
            }
        }

        void GenerateSDF(EditorCacheData data)
        {
            if (!data.CheckSectionTextures())
                return;

            if (data.sdfList == null)
                data.sdfList = new List<RenderTexture>();
            else
                data.sdfList.Clear();
            

            data.ResetFramePositions();

            Saito1994Driver saito1994Driver = new Saito1994Driver(data.sectionList, data.sdfList, data.DistacneScale);
            saito1994Driver.Execute();
            saito1994Driver.Clear();
            
        }

        
        private void CombineSDF(EditorCacheData data, CombineSDFDriver driver)
        {
            if (!data.CheckDataCombineValidate()) 
                return;
            
            driver.Setup(data.sectionList, data.sdfList, data.framePositions);
            driver.Execute(ref data.result);
        }

    }
}