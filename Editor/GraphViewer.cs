using AssetReferenceViewer;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
	public class GraphViewer : GraphView
	{
		static readonly Color BGColor = new Color(0.125f, 0.125f, 0.125f, 1f);
		public GraphViewer()
		{
			style.backgroundColor =  BGColor;
			SetupZoom(0.05f, 2f);
			this.StretchToParentSize();
			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new RectangleSelector());
			this.AddManipulator(new ClickSelector());

			contentViewContainer.transform.scale = Vector3.one * 0.75f;
		}

		public void Initialize(Object current)
		{
			foreach (var item in contentViewContainer.Children())
			{
				item.Clear();
			}

			if (current == null)
			{
				return;
			}

			var curNode = new ObjNode(current);
			curNode.Q("node-border").style.borderLeftColor = ObjNode.CurBorderColor;
			curNode.Q("node-border").style.borderRightColor = ObjNode.CurBorderColor;
			curNode.Q("node-border").style.borderBottomColor = ObjNode.CurBorderColor;
			curNode.Q("node-border").style.borderTopColor = ObjNode.CurBorderColor;
			AddElement(curNode);

			const int offsetH = 600;
			const int deltaV = 260;

			#region AddReference
			{
				var deps = AssetDatabase.GetDependencies(curNode.Path,false);
				var half = deps.Length / 2;
				bool even = deps.Length % 2 == 0;

				for (int i = 0; i < deps.Length; i++)
				{
					var item = deps[i];
					var obj = AssetDatabase.LoadAssetAtPath(item, typeof(Object));
					if (obj == null)
					{
						Debug.LogError(item);
					}
					else
					{
						var node = new ObjNode(obj);
						node.style.left = - offsetH;
						node.style.top = (i - half) * deltaV + (even ? deltaV/2 : 0);

						node.AddManipulator(new DoubleClickManipulator(()=>
						{
							Initialize(obj);
							SetPosition(new Vector2((worldBound.width - ObjNode.width) / 2,
								(worldBound.height - ObjNode.heigth) / 2));
						}));

						AddElement(node);

						var edge = curNode.InPort.ConnectTo(node.OutPort);
						AddElement(edge);
					}
				}

			}
			#endregion

			#region AddReferenceBy

			if (ReferenceViewerWindow.CacheTree.TryGetValue(curNode.PathHashCode, out var refby))
			{
				var half = refby.Count / 2;
				bool even = refby.Count % 2 == 0;
				for (int i = 0; i < refby.Count; i++)
				{
					var item = refby[i];
					var path = string.Join("/", item);
					var obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
					if (obj == null)
					{
						Debug.LogError(item);
					}
					else
					{
						var node = new ObjNode(obj);
						node.style.left = offsetH;
						node.style.top = (i - half) * deltaV + (even ? deltaV / 2 : 0);

						node.AddManipulator(new DoubleClickManipulator(() =>
						{
							Initialize(obj);
							SetPosition(new Vector2((worldBound.width - ObjNode.width) / 2,
								(worldBound.height - ObjNode.heigth) / 2));
						}));

						AddElement(node);

						var edge = curNode.OutPort.ConnectTo(node.InPort);
						AddElement(edge);
					}
				}
			}

			#endregion
		}

		public void SetPosition(Vector3 vector)
		{
			contentViewContainer.transform.position = vector;
		}

		protected override bool canCutSelection => false;
		protected override bool canDeleteSelection => false;
		protected override bool canPaste => false;
		protected override bool canDuplicateSelection => false;
	}
}