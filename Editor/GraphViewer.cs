using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetReferenceViewer
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
		}

		public void Initialize(Object current)
		{
			contentViewContainer.Clear();

			if (current == null)
			{
				return;
			}

			var curNode = new NodeMaker(current);
			curNode.Q("node-border").style.borderLeftColor = NodeMaker.CurBorderColor;
			curNode.Q("node-border").style.borderRightColor = NodeMaker.CurBorderColor;
			curNode.Q("node-border").style.borderBottomColor = NodeMaker.CurBorderColor;
			curNode.Q("node-border").style.borderTopColor = NodeMaker.CurBorderColor;
			AddElement(curNode);

			const int offsetH = 600;
			const int deltaV = 260;
			AssetInfo selectedAssetInfo = AssetReferenceViewer.GetAsset(AssetDatabase.GetAssetPath(current));
			if (selectedAssetInfo != null)
			{

				var i = 0;

				{
					var deps = selectedAssetInfo.dependencies;
					var half = deps.Count / 2;
					bool even = deps.Count % 2 == 0;

					foreach (var d in deps)
					{
						var item = d;
						var obj = AssetDatabase.LoadAssetAtPath(item, typeof(Object));
						if (obj == null)
						{
							AssetReferenceViewer.RebuildDatabase();
							return;
						}
						else
						{
							var node = new NodeMaker(obj);
							node.style.left = - offsetH;
							node.style.top = (i - half) * deltaV + (even ? deltaV/2 : 0);

							node.AddManipulator(new DoubleClickManipulator(()=>
							{
								Selection.activeObject = obj;
								Initialize(obj);
							}));

							AddElement(node);

							var edge = curNode.InPort.ConnectTo(node.OutPort);
							AddElement(edge);
						}

						i++;
					}

					i = 0;
				}

				{
					var refs = selectedAssetInfo.references;
					var half = refs.Count / 2;
					bool even = refs.Count % 2 == 0;
					foreach (var r in refs)
					{
						var item = r;
						var path = string.Join("/", item);
						var obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
						if (obj == null)
						{
							Debug.LogError(item);
						}
						else
						{
							var node = new NodeMaker(obj);
							node.style.left = offsetH;
							node.style.top = (i - half) * deltaV + (even ? deltaV / 2 : 0);

							node.AddManipulator(new DoubleClickManipulator(() =>
							{
								Selection.activeObject = obj;
								Initialize(obj);
							}));

							AddElement(node);

							var edge = curNode.OutPort.ConnectTo(node.InPort);
							AddElement(edge);
						}

						i++;
					}
				}

			}
		}

		protected override bool canCutSelection => false;
		protected override bool canDeleteSelection => false;
		protected override bool canPaste => false;
		protected override bool canDuplicateSelection => false;
	}
}