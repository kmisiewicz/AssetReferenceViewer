using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AssetReferenceViewer
{
    public class Window : EditorWindow, IHasCustomMenu
	{
		GraphViewer graphViewer;
		VisualTreeAsset itemTemplate;
		bool locked = false;

		[MenuItem("Assets/Asset Reference Viewer", true)]
		public static bool ShowWindowValidate()
		{
			return Selection.activeObject != null;
		}

		[MenuItem("Assets/Asset Reference Viewer", false, 31)]
		public static void ShowWindow()
		{
			var window = GetWindow<Window>();
            var content = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ?
                "d_Linked" : "Linked");
			content.text = "Asset Reference Viewer";
			window.titleContent = content;
		}

		public Window()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		void OnDestroy()
		{
			Selection.selectionChanged -= OnSelectionChanged;
		}

		void OnSelectionChanged()
		{
			if (!locked)
				Initialize();
		}

        void CreateGUI()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("AssetReferenceViewer");
            VisualElement root = visualTree.Instantiate();
            root.style.flexGrow = 1;
            rootVisualElement.Add(root);

			CreateGraph();			

			itemTemplate = Resources.Load<VisualTreeAsset>("Item");

            Initialize();
        }

		void CreateGraph()
		{
            var graph = rootVisualElement.Q<VisualElement>("Graph");
			graph.Clear();
            graphViewer = new GraphViewer();
			graph.Add(graphViewer);
            EditorApplication.delayCall += () =>
            {
                graphViewer.FrameAll();
                EditorApplication.delayCall += () => graphViewer.FrameAll();
            };
        }

        void Initialize()
		{
			if (Selection.activeObject == null)
				return;

			if (graphViewer == null)
				CreateGraph();
			graphViewer.Initialize(Selection.activeObject);

			var helpBox = rootVisualElement.Q<VisualElement>("HelpBox");
			helpBox.style.display = DisplayStyle.None;

			string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (Directory.Exists(selectedPath))
                return;

			HelpBox helpB = rootVisualElement.Q<HelpBox>("HelpboxOutsideAssets");
            if (!selectedPath.StartsWith("Assets/"))
            {
				if (helpB == null)
				{
					helpB = new HelpBox("This asset is outside Assets folder or you selected something in the scene", HelpBoxMessageType.Warning);
					helpB.name = "HelpboxOutsideAssets";
					graphViewer.Add(helpB);
				}
				else
				{
					helpB.style.display = DisplayStyle.Flex;
				}
                return;
            }
			else if (helpB != null)
			{
                helpB.style.display = DisplayStyle.None;
            }

            AssetInfo selectedAssetInfo = AssetReferenceViewer.GetAsset(selectedPath);

            if (selectedAssetInfo == null)
            {
                AssetReferenceViewer.RebuildDatabase();
                EditorApplication.delayCall += Initialize;
                return;
            }

            var objName = rootVisualElement.Q<Label>("ObjName");
			objName.text = Selection.activeObject.name;
			var objPath = rootVisualElement.Q<Label>("Path");
			objPath.text = selectedPath;
			var icon = rootVisualElement.Q<VisualElement>("AssetIcon");
			icon.style.backgroundImage = new StyleBackground((Texture2D) AssetDatabase.GetCachedIcon(selectedPath));

			var scroll = rootVisualElement.Q<ScrollView>("Scroll");
						
			var buildStatusIcon = rootVisualElement.Q<VisualElement>("BuildStatusIcon");
			buildStatusIcon.style.backgroundImage =
				selectedAssetInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack;
			buildStatusIcon.tooltip = selectedAssetInfo.IncludedStatus.ToString();

			var dependenciesFoldout = rootVisualElement.Q<Foldout>("Dependencies");
			dependenciesFoldout.Clear();
			dependenciesFoldout.text = "Dependencies (" + selectedAssetInfo.dependencies.Count + ")";
			scroll.Add(dependenciesFoldout);
			var referencesFoldout = rootVisualElement.Q<Foldout>("References");
			referencesFoldout.Clear();
			referencesFoldout.text = "References (" + selectedAssetInfo.references.Count + ")";
			scroll.Add(referencesFoldout);

			foreach (var d in selectedAssetInfo.dependencies)
			{
				var item = itemTemplate.Instantiate().Q<VisualElement>("Item");
                var itemIcon = item.Q<VisualElement>("ItemIcon");
				var itemLabel = item.Q<Label>("ItemLabel");
				var inBuildIcon = item.Q<VisualElement>("InBuildIcon");

				itemLabel.text = Path.GetFileName(d);
                itemLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                itemLabel.tooltip = d;
				item.RegisterCallback<MouseUpEvent>(e =>
				{
					if (e.button == 0)
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
				var item = itemTemplate.Instantiate().Q<VisualElement>("Item");
				var itemIcon = item.Q<VisualElement>("ItemIcon");
				var itemLabel = item.Q<Label>("ItemLabel");
				var inBuildIcon = item.Q<VisualElement>("InBuildIcon");

				itemLabel.text = Path.GetFileName(r);
                itemLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                itemLabel.tooltip = r;
                item.RegisterCallback<MouseUpEvent>(e =>
				{
                    if (e.button == 0)
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
				var helpLabel = rootVisualElement.Q<Label>("HelpBoxLabel");
				var helpButton = rootVisualElement.Q<Button>("HelpButton");
				var helpIcon = rootVisualElement.Q<VisualElement>("HelpIcon");
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
        }

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Rebuild Database"), false, () =>
			{
				AssetReferenceViewer.RebuildDatabase();
				Initialize();
			});
			menu.AddItem(new GUIContent("Clear Database"), false, AssetReferenceViewer.ClearDatabase);
			menu.AddItem(new GUIContent("Project Overlay"), WindowOverlay.Enabled, () => { WindowOverlay.Enabled = !WindowOverlay.Enabled; });
			menu.AddItem(new GUIContent("Lock"), locked, () => 
			{
				locked = !locked;
				if (!locked)
					Initialize();
			});
		}
	}
}
