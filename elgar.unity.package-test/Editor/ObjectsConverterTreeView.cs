using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Elgar.Unity.Util.ObjectsConverter
{

    [Serializable]
    public class HierarchyObjectItem : TreeViewItem
    {
        public string objectName { get; set; }
        public string objectPath { get; set; }
        public GameObject targetObject { get; set; }
        public StaticEditorFlags staticFlags { get; set; }
    }

    public class ObjectsConverterTreeView : TreeView
    {

        public List<HierarchyObjectItem> selectedItems = new List<HierarchyObjectItem>();

        public ObjectsConverterTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            columnIndexForTreeFoldouts = 0;
            // extraSpaceBeforeIconAndLabel = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            Reload();
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new HierarchyObjectItem { id = 0, depth = -1, displayName = "Root" };
            
            for (int i=0; i< UnityEditor.SceneManagement.EditorSceneManager.sceneCount; ++i)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                var sceneRoot = new HierarchyObjectItem { id = scene.GetHashCode(), depth = 0, displayName = scene.name };

                foreach (GameObject rootObj in scene.GetRootGameObjects())
                {
                    var sceneItems = CreateTreeViewItemRecursive(rootObj, 1);

                    if(sceneItems != null)
                    {
                        sceneRoot.AddChild(sceneItems);
                    }
                }

                root.AddChild(sceneRoot);
            }
            
            SetupDepthsFromParentsAndChildren(root);

            return root;
        }


        protected override void RowGUI(RowGUIArgs args)
        {
            HierarchyObjectItem item = (HierarchyObjectItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }

        }

        void CellGUI(Rect cellRect, HierarchyObjectItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case 0:
                    var newRect = cellRect;
                    float depthWidth = ((item.depth + 1) * 14);
                    
                    newRect.x += depthWidth;
                    newRect.width -= depthWidth;
                    GUI.Label(newRect, item.displayName);
                    break;

                case 1:
                    GUI.Label(cellRect, item.staticFlags.ToString());
                    break;
            }
        }


        public HierarchyObjectItem CreateTreeViewItemRecursive(GameObject rootObj, int depth, HierarchyObjectItem parent = null)
        {
            if (parent == null)
            {
                parent = new HierarchyObjectItem();
                parent.id = parent.GetHashCode();
                parent.depth = depth;
                parent.displayName = rootObj.name;
                parent.targetObject = rootObj;
                parent.staticFlags = GameObjectUtility.GetStaticEditorFlags(rootObj);
            }

            for (int i = 0; i < rootObj.transform.childCount; ++i)
            {
                var childObj = rootObj.transform.GetChild(i).gameObject;
                if(childObj != null)
                {
                    var child = CreateTreeViewItemRecursive(childObj, depth + 1);

                    if (parent != null)
                    {
                        parent.AddChild(child);
                    }
                }
            }
            return parent;

        }


        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            selectedItems.Clear();
            if (selectedIds.Count > 0)
            {
                foreach(var selectedId in selectedIds)
                {
                    var item = FindItem(selectedId, rootItem) as HierarchyObjectItem;
                    if(item != null && item.targetObject != null)
                    {
                        selectedItems.Add(item);
                    }
                }
            }
        }

        public void ReloadData()
        {
            if(rootItem != null)
            {
                RecursiveReloadItem(rootItem.children);
            }
            
        }

        void RecursiveReloadItem(List<TreeViewItem> childItems)
        {
            if(childItems != null)
            {
                foreach (HierarchyObjectItem item in childItems)
                {
                    if (item.targetObject != null)
                    {
                        item.staticFlags = GameObjectUtility.GetStaticEditorFlags(item.targetObject);
                    }
                    RecursiveReloadItem(item.children);
                }
            }
        }
        

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Objects"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 300,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Status", ""),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 180,
                    minWidth = 60,
                    autoResize = true
                }
            };

            //Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
    }

    public class PFObjectConverterColumnHeader : MultiColumnHeader
    {
        public PFObjectConverterColumnHeader(MultiColumnHeaderState state) : base(state)
        {
            canSort = false;
            height = DefaultGUI.minimumHeight;
        }

        protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            base.ColumnHeaderGUI(column, headerRect, columnIndex);
        }
    }
}


