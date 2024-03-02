using UnityEditor;
using UnityEngine.UIElements;

namespace Hex.Tools
{
	[CustomEditor(typeof(EditorHexGrid))]
	public class EditorHexGridInspector : Editor
	{
		private EditorHexGrid _target;
		
		private void Awake()
		{
			_target = (EditorHexGrid)target;
		}

		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			var defaultInspector = new IMGUIContainer(() => DrawDefaultInspector());

			root.Add(defaultInspector);
			
			root.Add(new VisualElement { style = { height = 10, flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)} });
			
			var fileField = new TextField("File Name")
			{
				value = _target.LevelFileName
			};
			fileField.RegisterValueChangedCallback(evt => _target.LevelFileName = evt.newValue);
			root.Add(fileField);
			
			var saveButton = new Button { text = "Save" };
			saveButton.clicked += () => _target.Save(fileField.text);
			root.Add(saveButton);
			
			var loadButton = new Button { text = "Load New" };
			loadButton.clicked += () => _target.Load(true);
			root.Add(loadButton);
			
			var loadFile = new Button { text = "Load From File" };
			loadFile.clicked += () => _target.LoadFromFile(fileField.text);
			root.Add(loadFile);
			
			var clearButton = new Button { text = "Clear Scene" };
			clearButton.clicked += () => _target.ForceClear();
			root.Add(clearButton);

			return root;
		}
	}
}