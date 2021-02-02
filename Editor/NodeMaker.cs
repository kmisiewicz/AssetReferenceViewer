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
		public const int width = 136;
		public const int heigth = 230;
		public static readonly Color InColor = Color.red * 0.95f;
		public static readonly Color OutColor = Color.green * 0.95f;
		public static readonly Color CurBorderColor = new Color(0.849f, 0.514f, 0.1f, 1);
		public string Path { get; }
		public int PathHashCode { get; }

		public NodeMaker(UnityEngine.Object obj)
		{
			Path = AssetDatabase.GetAssetPath(obj);
			PathHashCode = Path.GetHashCode();

			style.width = width;
			if (titleContainer.Q("title-label")?.style is IStyle tempstyle)
			{
				tempstyle.fontSize = 14;
				tempstyle.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
				tempstyle.color = Color.white * 0.95f;
			}

			title = obj.name;

			var controlsContainer = new VisualElement {name = "resource"};
			mainContainer.Add(controlsContainer);

			var of = new ObjectField {value = obj};
			of.SetCanSelect(false);
			controlsContainer.Add(of);

			var img = new Image {style = {height = 128, width = 128}};
			img.image = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj);
			controlsContainer.Add(img);

			OutPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(object));
			OutPort.portName = "ref-by";
			OutPort.portColor = InColor;
			outputContainer.Add(OutPort);

			InPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(object));
			InPort.portName = "  ref  ";
			InPort.portColor = OutColor;
			inputContainer.Add(InPort);
		}

		public Port OutPort { get; private set; }
		public Port InPort { get; private set; }

		public override bool IsMovable()
		{
			return false;
		}
	}

	public class DoubleClickManipulator : ClickSelector
	{
		private Action callback;

		public DoubleClickManipulator(Action callback)
		{
			this.callback = callback;
		}

		public DateTime LastClick { get; private set; } = DateTime.MinValue;

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
			if ((DateTime.Now - LastClick).TotalSeconds < 0.8f)
			{
				//Debug.LogError("test");
				callback?.Invoke();
			}
			else
			{
				LastClick = DateTime.Now;
			}
		}
	}

	public static class Ex
	{
		public static void SetCanSelect(this ObjectField field, bool isCan)
		{
			if (field == null)
			{
				return;
			}

			foreach (var item in field.Children())
			{
				if (item.GetType().Name == "ObjectFieldSelector")
				{
					item.style.width = isCan ? 18 : 0;
				}
			}
		}
	}
}