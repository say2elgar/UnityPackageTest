using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Text;
using UnityEditor.IMGUI.Controls;

namespace Elgar.Unity.Util.ObjectsConverter
{
    public class ObjectsConverter : EditorWindow
    {
        #region 외부 함수

        [MenuItem("Window/Util/Object Converter", false, 39)]
        public static void OpenEditor()
        {
            var window = EditorWindow.GetWindow<ObjectsConverter>();

            window.Show();
        }

        #endregion


        #region Private 

        private static float TREEVIEW_WIDTH = 500;
        private static float MIN_PROPERTY_WIDTH = 300;
        private static float WINDOW_PADDING = 10;

        private float MIN_WIDTH
        {
            get { return TREEVIEW_WIDTH + MIN_PROPERTY_WIDTH + (WINDOW_PADDING * 2); }
        }


        List<GameObject> _curGameObjectList = new List<GameObject>();
        Dictionary<GameObject, StaticEditorFlags> _curStaticEditorFlags = new Dictionary<GameObject, StaticEditorFlags>();

        [NonSerialized] bool m_Initialized;
        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
        SearchField m_SearchField;
        ObjectsConverterTreeView m_TreeView;

        private StaticEditorFlags _curStaticFlags = 0;
        private bool _includeChildren = false;


        #endregion

        #region Properties


        #endregion


        #region Editor Functions

        void OnEnable()
        {
            titleContent.text = "Objects Converter";
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (position.width < MIN_WIDTH)
            {
                var newPosition = position;
                newPosition.width = MIN_WIDTH;
                position = newPosition;
            }

            minSize = new Vector2(MIN_WIDTH, 0);

            UpdateGameObjectList();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredEditMode || mode == PlayModeStateChange.EnteredPlayMode)
            {
            }
        }
        
        void OnFocus()
        {
            
        }

        void OnLostFocus()
        {
        }

        void OnHierarchyChange()
        {

        }

        void OnGUI()
        {
            
            InitIfNeeded();
            
            DrawTopMenu();

            if (m_TreeView != null && position != null)
            {
                m_TreeView.OnGUI(treeViewRect);
            }
            DrawDetailView();
        }

        #endregion


        #region Private methods



        Rect treeViewRect
        {
            get { return new Rect(WINDOW_PADDING, 20, TREEVIEW_WIDTH, position.height - 40); }
        }

        Rect topMenuRect
        {
            get { return new Rect(0, 0, position.width, 20); }
        }

        Rect detailViewRect
        {
            get { return new Rect(WINDOW_PADDING + TREEVIEW_WIDTH, 20, (position.width) - (WINDOW_PADDING + TREEVIEW_WIDTH) - WINDOW_PADDING, position.height - 40); }
        }

        void DrawTopMenu()
        {
            GUILayout.BeginArea(topMenuRect);
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
                    {
                        UpdateGameObjectList();
                    }
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }


        void DrawDetailView()
        {
            GUILayout.BeginArea(detailViewRect);
            {
                EditorGUILayout.BeginVertical();
                {
                    var selectedItems = m_TreeView.GetSelection();
                    //var rows = GetRows().Where(t => DoesItemMatchSearch(t, matchString)).Select(t => t.id).ToList();

                    GUILayout.Label("선택된 항목 (" + selectedItems.Count.ToString() + ")");
                    EditorGUILayout.BeginVertical("box");
                    {
                        //bool bConflicted = false;
                        StaticEditorFlags selectedFlags = 0;

                        bool bFirst = true;
                        foreach (var selectedItem in m_TreeView.selectedItems)
                        {
                            if (bFirst)
                            {
                                selectedFlags = selectedItem.staticFlags;
                                bFirst = false;
                            }
                            else
                            {
                                if (selectedFlags != selectedItem.staticFlags)
                                {
                                    //bConflicted = true;
                                }
                                selectedFlags |= selectedItem.staticFlags;
                            }

                            GUILayout.Label(selectedItem.displayName);
                        }

                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space();


                    EditorGUILayout.LabelField("Static Flags", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            _curStaticFlags = (StaticEditorFlags)EditorGUILayout.EnumFlagsField(_curStaticFlags);
                            if (GUILayout.Button("="))
                            {
                                ApplyTargetFlag();
                            }

                            if (GUILayout.Button("+"))
                            {
                                ApplyTargetFlagPlus();
                            }

                            if (GUILayout.Button("-"))
                            {
                                ApplyTargetFlagMinus();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        _includeChildren = EditorGUILayout.ToggleLeft("자식노드 전체 적용", _includeChildren);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.LabelField("Object Remove", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("Delete"))
                            {
                                DeleteGameObjects();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();



                }
                EditorGUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        void ApplyTargetFlag()
        {
            foreach (var selectedItem in m_TreeView.selectedItems)
            {
                if(selectedItem.targetObject != null)
                {
                    if(_includeChildren)
                    {
                        SetStaticFlagsRecursively(selectedItem.targetObject);
                    }
                    else
                    {
                        GameObjectUtility.SetStaticEditorFlags(selectedItem.targetObject, _curStaticFlags);
                    }
                    
                }
            }

            ReloadData();
            Repaint();
        }

        void ApplyTargetFlagPlus()
        {
            foreach (var selectedItem in m_TreeView.selectedItems)
            {
                if (selectedItem.targetObject != null)
                {
                    if (_includeChildren)
                    {
                        SetStaticFlagsRecursivelyPlus(selectedItem.targetObject);
                    }
                    else
                    {
                        var curFlags = GameObjectUtility.GetStaticEditorFlags(selectedItem.targetObject);
                        GameObjectUtility.SetStaticEditorFlags(selectedItem.targetObject, curFlags | _curStaticFlags);
                    }
                }
            }

            ReloadData();
            Repaint();
        }

        void ApplyTargetFlagMinus()
        {
            foreach (var selectedItem in m_TreeView.selectedItems)
            {
                if (selectedItem.targetObject != null)
                {
                    if (_includeChildren)
                    {
                        SetStaticFlagsRecursivelyMinus(selectedItem.targetObject);
                    }
                    else
                    {
                        var curFlags = GameObjectUtility.GetStaticEditorFlags(selectedItem.targetObject);
                        GameObjectUtility.SetStaticEditorFlags(selectedItem.targetObject, curFlags & (~_curStaticFlags));
                    }
                }
            }

            ReloadData();
            Repaint();
        }

        void DeleteGameObjects()
        {
            foreach (var selectedItem in m_TreeView.selectedItems)
            {
                if (selectedItem.targetObject != null)
                {
                    GameObject.DestroyImmediate(selectedItem.targetObject);
                }
            }

            UpdateGameObjectList();
            Repaint();
        }


        void InitIfNeeded()
        {
            if (!m_Initialized)
            {
                m_Initialized = true;

                // Check if it already exists (deserialized from window layout file or scriptable object)
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();

                bool firstInit = m_MultiColumnHeaderState == null;
                var headerState = ObjectsConverterTreeView.CreateDefaultMultiColumnHeaderState(treeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                m_MultiColumnHeaderState = headerState;

                var multiColumnHeader = new PFObjectConverterColumnHeader(headerState);
                if (firstInit)
                    multiColumnHeader.ResizeToFit();

                m_TreeView = new ObjectsConverterTreeView(m_TreeViewState, multiColumnHeader);
                //m_TreeView.OnSelectionChangedItemCallback += OnSelectionChangedItem;

                m_SearchField = new SearchField();
                m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
            }
        }

        bool isPlayMode
        {
            get
            {
                return EditorApplication.isPlaying ||
                    EditorApplication.isPlayingOrWillChangePlaymode;
            }
        }

        public void SetStaticFlagsRecursively(GameObject go)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(trans.gameObject, _curStaticFlags);
            }
        }


        public void SetStaticFlagsRecursivelyPlus(GameObject go)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                var curFlags = GameObjectUtility.GetStaticEditorFlags(trans.gameObject);
                GameObjectUtility.SetStaticEditorFlags(trans.gameObject, curFlags | _curStaticFlags);
            }
        }

        public void SetStaticFlagsRecursivelyMinus(GameObject go)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                var curFlags = GameObjectUtility.GetStaticEditorFlags(trans.gameObject);
                GameObjectUtility.SetStaticEditorFlags(trans.gameObject, curFlags & (~_curStaticFlags));
            }
        }


        

        void UpdateGameObjectList()
        {
            m_Initialized = false;
        }

        void ReloadData()
        {
            m_TreeView.ReloadData();
        }
        
        #endregion

    }
}