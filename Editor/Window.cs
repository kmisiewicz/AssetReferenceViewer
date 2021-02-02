using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace AssetReferenceViewer
{
	public class Window : EditorWindow, IHasCustomMenu
	{
		private VisualTreeAsset visualTree = null;
		private TemplateContainer container;
		private ReferenceViewer referenceViewer;
		static bool needReCal = true;

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

			Action tempcallback = () =>
			{
				window.referenceViewer.Initialize(Selection.activeObject);

				window.referenceViewer.SetPosition(new Vector2((window.position.width - ObjNode.width) / 2,
					(window.position.height - ObjNode.heigth) / 2));
			};

			MainThreadInvoke(() =>
			{
				if (needReCal)
				{
					CalculateRef(tempcallback);
				}
				else
				{
					tempcallback.Invoke();
				}
			});
		}

		public Window()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		private void OnSelectionChanged()
		{

		}

		private void OnEnable()
		{
			string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			//if (string.IsNullOrEmpty(selectedPath))
			//	return;

			visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/project-curator/Editor/AssetReferenceViewer.uxml");
			var rootView = rootVisualElement;
			var root = visualTree.CloneTree().Q<VisualElement>("Root");
			rootView.Add(root);
			referenceViewer = new ReferenceViewer();
			rootView.Q<VisualElement>("Graph").Add(referenceViewer);

			var objName = root.Q<Label>("ObjName");
			objName.text = Selection.activeObject.name;
			var objPath = root.Q<Label>("Path");
			objPath.text = selectedPath;
			var icon = root.Q<VisualElement>("AssetIcon");
			icon.style.backgroundImage = new StyleBackground((Texture2D) AssetDatabase.GetCachedIcon(selectedPath));

			if (Directory.Exists(selectedPath))
				return;

			AssetInfo selectedAssetInfo = AssetReferenceViewer.GetAsset(selectedPath);


			var helpBox = root.Q<VisualElement>("HelpBox");
			if (selectedAssetInfo == null)
			{
				/*helpBox.style.display = DisplayStyle.Flex;
				var helpLabel = root.Q<Label>("HelpBoxLabel");
				var helpButton = root.Q<Button>("HelpButton");
				var helpIcon = root.Q<VisualElement>("HelpIcon");
				helpButton.text = "Rebuild Database";
				helpLabel.text = "You must rebuild database to obtain information on this asset";
				helpIcon.style.backgroundImage = (Texture2D) EditorGUIUtility.IconContent("console.warnicon").image;
				helpButton.clicked += AssetReferenceViewer.RebuildDatabase;*/
				AssetReferenceViewer.RebuildDatabase();
			}

			helpBox.style.display = DisplayStyle.None;

			var buildStatusIcon = root.Q<VisualElement>("BuildStatusIcon");
			buildStatusIcon.style.backgroundImage =
				selectedAssetInfo.IsIncludedInBuild ? ProjectIcons.LinkBlue : ProjectIcons.LinkBlack;
			buildStatusIcon.tooltip = selectedAssetInfo.IncludedStatus.ToString();

			var dependenciesFoldout = root.Q<Foldout>("Dependencies");
			dependenciesFoldout.text = "Dependencies (" + selectedAssetInfo.dependencies.Count + ")";
			var referencesFoldout = root.Q<Foldout>("References");
			referencesFoldout.text = "References (" + selectedAssetInfo.referencers.Count + ")";

			foreach (var d in selectedAssetInfo.dependencies)
			{
				var item = visualTree.CloneTree().Q<VisualElement>("Item");
				var itemIcon = item.Q<VisualElement>("ItemIcon");
				var itemLabel = item.Q<Label>("ItemLabel");
				var inBuildIcon = item.Q<VisualElement>("InBuildIcon");

				itemLabel.text = Path.GetFileName(d);
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

			foreach (var r in selectedAssetInfo.referencers)
			{
				var item = visualTree.CloneTree().Q<VisualElement>("Item");
				var itemIcon = item.Q<VisualElement>("ItemIcon");
				var itemLabel = item.Q<Label>("ItemLabel");
				var inBuildIcon = item.Q<VisualElement>("InBuildIcon");

				itemLabel.text = Path.GetFileName(r);
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

		static void MainThreadInvoke(Action action)
		{
			invokeQ.Enqueue(action);
		}

		static readonly ConcurrentQueue<Action> invokeQ = new ConcurrentQueue<Action>();

		void Update()
		{
			while (invokeQ.Count > 0)
			{
				if (invokeQ.TryDequeue(out var action))
				{
					action?.Invoke();
				}
			}
		}

		private static readonly Dictionary<int, List<string[]>> CacheTree = new Dictionary<int, List<string[]>>();
		static readonly object insertLock = new object();

		static void CalculateRef(Action tempcallback)
		{
			CacheTree.Clear();
			var allpath = AssetDatabase.GetAllAssetPaths();
			List<Task> alltask = new List<Task>();
			foreach (var item in allpath)
			{
				var deps = AssetDatabase.GetDependencies(item, false);
				alltask.Add(Task.Run(() =>
				{
					var dep = item.Split('/');

					foreach (var ref_by in deps)
					{
						var hashcode = ref_by.GetHashCode();
						lock (insertLock)
						{
							if (CacheTree.TryGetValue(hashcode, out var list))
							{
								list.Add(dep);
							}
							else
							{
								CacheTree[hashcode] = new List<string[]>();
								CacheTree[hashcode].Add(dep);
							}
						}
					}
				}));

				EditorUtility.DisplayProgressBar("Calculate Reference Tree",
					$"{alltask.Count}/{allpath.Length}", (float) alltask.Count / allpath.Length);
			}

			Task.Run(() =>
			{
				Task.WaitAll(alltask.ToArray());
				needReCal = false;
				MainThreadInvoke(() =>
				{
					EditorUtility.ClearProgressBar();
					tempcallback?.Invoke();
				});
			});
		}

		void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Rebuild Database"), false, AssetReferenceViewer.RebuildDatabase);
			menu.AddItem(new GUIContent("Clear Database"), false, AssetReferenceViewer.ClearDatabase);
			menu.AddItem(new GUIContent("Project Overlay"), WindowOverlay.Enabled, () => { WindowOverlay.Enabled = !WindowOverlay.Enabled; });
		}
	}
}
