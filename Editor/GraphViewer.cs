using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetReferenceViewer
{
	public class GraphViewer : GraphView
	{
        const int OFFSET_H = 600;
		const int PADDING_RIGHT = 136;
        const int DELTA_V = 230;

        static readonly Color BGColor = new Color(0.125f, 0.125f, 0.125f, 1f);
        static readonly Color CurrentBorderColor = new Color(0.849f, 0.514f, 0.1f, 1);

        public GraphViewer()
		{
			style.backgroundColor = BGColor;
			SetupZoom(0.05f, 2f);
			this.StretchToParentSize();
			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new ClickSelector());
		}

		public void Initialize(Object currentAsset)
		{
			graphElements.ForEach(e => RemoveElement(e));

			if (currentAsset == null)
				return;

			var currentNode = new NodeMaker(currentAsset);
			var nodeBorder = currentNode.Q("node-border");
			nodeBorder.style.borderLeftColor = nodeBorder.style.borderRightColor =
                nodeBorder.style.borderBottomColor = nodeBorder.style.borderTopColor = CurrentBorderColor;
			nodeBorder.style.borderLeftWidth = nodeBorder.style.borderRightWidth =
				nodeBorder.style.borderTopWidth = nodeBorder.style.borderBottomWidth = 4;
			AddElement(currentNode);

			AssetInfo selectedAssetInfo = AssetReferenceViewer.GetAsset(AssetDatabase.GetAssetPath(currentAsset));
			if (selectedAssetInfo == null)
				return;

            Rect fullRect = new Rect
            {
                xMin = selectedAssetInfo.dependencies.Count > 0 ? -OFFSET_H : 0,
                xMax = selectedAssetInfo.references.Count > 0 ? OFFSET_H + PADDING_RIGHT : PADDING_RIGHT,
                yMax = DELTA_V
            };

            int i = 0;

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
						Selection.activeObject = currentAsset;
						return;
					}
					
					var node = new NodeMaker(obj);
					node.style.left = - OFFSET_H;
					node.style.top = (i - half) * DELTA_V + (even ? DELTA_V * 0.5f : 0);

					fullRect.yMin = Mathf.Min(fullRect.yMin, node.style.top.value.value);
					fullRect.yMax = Mathf.Max(fullRect.yMax, node.style.top.value.value + DELTA_V);

					node.AddManipulator(new DoubleClickManipulator(()=>
					{
						Selection.activeObject = obj;
					}));

					AddElement(node);

					var edge = currentNode.InPort.ConnectTo(node.OutPort);
					AddElement(edge);

					i++;
				}

				i = 0;
			}

			{
				var references = selectedAssetInfo.references;
				var half = references.Count / 2;
				bool even = references.Count % 2 == 0;
				foreach (var reference in references)
				{
					var obj = AssetDatabase.LoadAssetAtPath(reference, typeof(Object));
					if (obj == null)
					{
                        AssetReferenceViewer.RebuildDatabase();
                        Selection.activeObject = currentAsset;
                        return;
                    }

					var node = new NodeMaker(obj);
					node.style.left = OFFSET_H;
					node.style.top = (i - half) * DELTA_V + (even ? DELTA_V * 0.5f : 0);

                    fullRect.yMin = Mathf.Min(fullRect.yMin, node.style.top.value.value);
                    fullRect.yMax = Mathf.Max(fullRect.yMax, node.style.top.value.value + DELTA_V);

                    node.AddManipulator(new DoubleClickManipulator(() =>
					{
						Selection.activeObject = obj;
					}));

					AddElement(node);

					var edge = currentNode.OutPort.ConnectTo(node.InPort);
					AddElement(edge);

					i++;
				}
			}

			CalculateFrameTransform(fullRect, layout, 5, out var frameTranslation, out var frameScaling);
			UpdateViewTransform(frameTranslation, frameScaling);
		}

		protected override bool canCutSelection => false;
		protected override bool canDeleteSelection => false;
		protected override bool canPaste => false;
		protected override bool canDuplicateSelection => false;
	}
}