using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetReferenceViewer
{
	public class NodeMaker : Node
	{
		const int width = 136;

		static readonly Color InColor = Color.red * 0.95f;
		static readonly Color OutColor = Color.green * 0.95f;

        readonly string path;

		public NodeMaker(UnityEngine.Object obj)
		{
			path = AssetDatabase.GetAssetPath(obj);

			style.width = width;
			if (titleContainer.Q("title-label")?.style is IStyle tempStyle)
			{
				tempStyle.fontSize = 14;
				tempStyle.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
				tempStyle.color = Color.white * 0.95f;
			}

			title = obj.name;
			tooltip = path;

			var controlsContainer = new VisualElement {name = "resource"};
			mainContainer.Add(controlsContainer);

			var objectField = new ObjectField {value = obj};
            var selector = objectField.Q<VisualElement>(className: ObjectField.selectorUssClassName);
            if (selector != null)
				selector.style.display = DisplayStyle.None;
            controlsContainer.Add(objectField);

			var img = new Image
			{
				style = { height = 128, width = 128 },
				image = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj)
			};
			controlsContainer.Add(img);

			OutPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(object));
			OutPort.portName = "  dep  ";
			OutPort.portColor = InColor;
			outputContainer.Add(OutPort);

			InPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(object));
			InPort.portName = "  ref  ";
			InPort.portColor = OutColor;
			inputContainer.Add(InPort);
		}

		public Port OutPort { get; private set; }
		public Port InPort { get; private set; }

		public override bool IsMovable() => false;
	}

	public class DoubleClickManipulator : ClickSelector
	{
		private readonly Action callback;
		private DateTime lastClick = DateTime.MinValue;

		public DoubleClickManipulator(Action callback)
		{
			this.callback = callback;
		}

		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<MouseDownEvent>(OnMouseDown2);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown2);
		}

		void OnMouseDown2(MouseDownEvent evt)
		{
			if ((DateTime.Now - lastClick).TotalSeconds < 0.8f)
			{
				callback?.Invoke();
				lastClick = DateTime.MinValue;
			}
			else
			{
				lastClick = DateTime.Now;
			}
		}
	}
}