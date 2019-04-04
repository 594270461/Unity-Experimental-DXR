using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityEditorInternal
{
    class BindingTreeViewDataSource : TreeViewDataSource
    {
        AnimationClip m_Clip;
        public BindingTreeViewDataSource(TreeViewController treeView, AnimationClip clip)
            : base(treeView)
        {
            m_Clip = clip;
            showRootItem = false;
            rootIsCollapsable = false;
        }

        void SetupRootNodeSettings()
        {
            showRootItem = false;
            SetExpanded(root, true);
        }

        static string GroupName(EditorCurveBinding binding)
        {
            string property = AnimationWindowUtility.GetNicePropertyGroupDisplayName(binding.type, binding.propertyName);
            if (!string.IsNullOrEmpty(binding.path))
            {
                property = binding.path + " : " + property;
            }
            return property;
        }

        static string PropertyName(EditorCurveBinding binding)
        {
            return AnimationWindowUtility.GetPropertyDisplayName(binding.propertyName);
        }

        public override void FetchData()
        {
            if (m_Clip == null)
                return;

            var bindings = AnimationUtility.GetCurveBindings(m_Clip).Union(AnimationUtility.GetObjectReferenceCurveBindings(m_Clip));
            var results = bindings.GroupBy(p => GroupName(p), p => p, (key, g) => new
            {
                parent = key,
                bindings = g.ToList()
            }
            );

            m_RootItem = new CurveTreeViewNode(-1, null, "root", null);
            m_RootItem.children = new List<TreeViewItem>();
            int id = Guid.NewGuid().GetHashCode();
            foreach (var r in results)
            {
                var newNode = new CurveTreeViewNode(id++, m_RootItem, r.parent, r.bindings.ToArray());
                m_RootItem.children.Add(newNode);
                if (r.bindings.Count > 1)
                {
                    for (int b = 0; b < r.bindings.Count; b++)
                    {
                        if (newNode.children == null)
                            newNode.children = new List<TreeViewItem>();

                        var bindingNode = new CurveTreeViewNode(id++, newNode, PropertyName(r.bindings[b]), new[] {r.bindings[b]});
                        newNode.children.Add(bindingNode);
                    }
                }
            }

            SetupRootNodeSettings();
            m_NeedRefreshRows = true;
        }

        public void UpdateData()
        {
            m_TreeView.ReloadData();
        }
    }

    class CurveTreeViewNode : TreeViewItem
    {
        EditorCurveBinding[] m_Bindings;

        public EditorCurveBinding[] bindings
        {
            get { return m_Bindings; }
        }

        public CurveTreeViewNode(int id, TreeViewItem parent, string displayName, EditorCurveBinding[] bindings)
            : base(id, parent != null ? parent.depth + 1 : -1, parent, displayName)
        {
            m_Bindings = bindings;
        }
    }
}