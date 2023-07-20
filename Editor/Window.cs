using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using Object = UnityEngine.Object;

namespace AssetReferenceViewer
{
	public class Window : EditorWindow, IHasCustomMenu
	{
		private VisualTreeAsset visualTree = null;
		private TemplateContainer container;
		private GraphViewer graphViewer;

		[MenuItem("Assets/Asset Reference Viewer", true)]
		public static bool ShowWindowValidate()
		{
			return Selection.activeObject != null;
		}

		[MenuItem("Assets/Asset Reference Viewer", false, 31)]
		public static void ShowWindow()
		{
			var window = GetWindow<Window>();
			window.titleContent = new GUIContent("Asset Reference Viewer");
		}

		public Window()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		private void OnDestroy()
		{
			Selection.selectionChanged -= OnSelectionChanged;
		}

		private void OnSelectionChanged()
		{
			Initialize();
		}

		private void OnEnable()
		{
			Initialize();
		}

		private void Initialize()
		{
			if (Selection.activeObject == null) return;

			visualTree = Resources.Load<VisualTreeAsset>("AssetReferenceViewer");
			var rootView = rootVisualElement;
			var root = visualTree.CloneTree().Q<VisualElement>("Root");
			rootView.Clear();
			rootView.Add(root);

			graphViewer = new GraphViewer();
			graphViewer.Initialize(Selection.activeObject);
			var graph = rootView.Q<VisualElement>("Graph");
			graph.Add(graphViewer);

			var helpBox = root.Q<VisualElement>("HelpBox");
			helpBox.style.display = DisplayStyle.None;

			string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

			var objName = root.Q<Label>("ObjName");
			objName.text = Selection.activeObject.name;
			var objPath = root.Q<Label>("Path");
			objPath.text = selectedPath;
			var icon = root.Q<VisualElement>("AssetIcon");
			icon.style.backgroundImage = new StyleBackground((Texture2D) AssetDatabase.GetCachedIcon(selectedPath));

			if (!selectedPath.StartsWith("Assets/"))
			{
				var helpB = new HelpBox("This asset is outside Assets folder or you selected something in the scene", HelpBoxMessageType.Warning);
				graphViewer.Add(helpB);
				return;
			}

			var scroll = root.Q<ScrollView>("Scroll");

			if (Directory.Exists(selectedPath))
				return;

			AssetInfo selectedAssetInfo = AssetReferenceViewer.GetAsset(selectedPath);

			if (selectedAssetInfo == null)
			{
				AssetReferenceViewer.RebuildDatabase();
				EditorApplication.delayCall = Initialize;
				return;
			}

			var buildStatusIcon = root.Q<VisualElement>("BuildStatusIcon");
			buildStatusIcon.style.backgroundImage =
				selectedAssetInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack;
			buildStatusIcon.tooltip = selectedAssetInfo.IncludedStatus.ToString();

			var dependenciesFoldout = root.Q<Foldout>("Dependencies");
			dependenciesFoldout.Clear();
			dependenciesFoldout.text = "Dependencies (" + selectedAssetInfo.dependencies.Count + ")";
			scroll.Add(dependenciesFoldout);
			var referencesFoldout = root.Q<Foldout>("References");
			referencesFoldout.Clear();
			referencesFoldout.text = "References (" + selectedAssetInfo.references.Count + ")";
			scroll.Add(referencesFoldout);

			foreach (var d in selectedAssetInfo.dependencies)
			{
				var item = visualTree.CloneTree().Q<VisualElement>("Item");
				var itemIcon = item.Q<VisualElement>("ItemIcon");
				var itemLabel = item.Q<Label>("ItemLabel");
				var inBuildIcon = item.Q<VisualElement>("InBuildIcon");

				itemLabel.text = Path.GetFileName(d);
				itemLabel.tooltip = d;
				item.RegisterCallback<MouseUpEvent>(e =>
				{
					Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(d);
				});
				itemIcon.style.backgroundImage = AssetDatabase.GetCachedIcon(d) as Texture2D;
				AssetInfo depInfo = AssetReferenceViewer.GetAsset(d);
				inBuildIcon.style.backgroundImage =
					depInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack;
				inBuildIcon.tooltip = depInfo.IncludedStatus.ToString();
				item.style.display = DisplayStyle.Flex;
				dependenciesFoldout.contentContainer.Add(item);
			}

			foreach (var r in selectedAssetInfo.references)
			{
				var item = visualTree.CloneTree().Q<VisualElement>("Item");
				var itemIcon = item.Q<VisualElement>("ItemIcon");
				var itemLabel = item.Q<Label>("ItemLabel");
				var inBuildIcon = item.Q<VisualElement>("InBuildIcon");

				itemLabel.text = Path.GetFileName(r);
                itemLabel.tooltip = r;
                item.RegisterCallback<MouseUpEvent>(e =>
				{
					Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(r);
				});
				itemIcon.style.backgroundImage = AssetDatabase.GetCachedIcon(r) as Texture2D;
				AssetInfo depInfo = AssetReferenceViewer.GetAsset(r);
				inBuildIcon.style.backgroundImage =
					depInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack;
				inBuildIcon.tooltip = depInfo.IncludedStatus.ToString();
				item.style.display = DisplayStyle.Flex;
				referencesFoldout.contentContainer.Add(item);
			}

			if (!selectedAssetInfo.IsIncludedInBuild)
			{
				helpBox.style.display = DisplayStyle.Flex;
				var helpLabel = root.Q<Label>("HelpBoxLabel");
				var helpButton = root.Q<Button>("HelpButton");
				var helpIcon = root.Q<VisualElement>("HelpIcon");
				helpButton.text = "Delete Asset";
				helpButton.style.display = DisplayStyle.Flex;
				helpLabel.text = "This asset is not referenced and never used. Would you like to delete it ?";
				helpIcon.style.backgroundImage = (Texture2D) EditorGUIUtility.IconContent("console.warnicon").image;
				helpButton.clicked += ()=>
				{
					File.Delete(selectedPath);
					AssetDatabase.Refresh();
					AssetReferenceViewer.RemoveAssetFromDatabase(selectedPath);
				};
			}

			EditorApplication.delayCall = ()=>
			{
				graphViewer.FrameAll();
			};
		}

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Rebuild Database"), false, ()=>
			{
				AssetReferenceViewer.RebuildDatabase();
				Initialize();
			});
			menu.AddItem(new GUIContent("Clear Database"), false, AssetReferenceViewer.ClearDatabase);
			menu.AddItem(new GUIContent("Project Overlay"), WindowOverlay.Enabled, () => { WindowOverlay.Enabled = !WindowOverlay.Enabled; });
		}
	}
}
