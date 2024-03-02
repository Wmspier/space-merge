using Hex.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Hex.Editor.Tools
{
	public class LevelEditorWindow : EditorWindow
	{
		private EditorHexGrid _grid;
		
		[MenuItem("Hex/Level Editor Window")]
		public static void ShowExample()
		{
			var window = GetWindow<LevelEditorWindow>();
			window.titleContent = new GUIContent("Level Editor");
		}

		private void OnEnable()
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				SceneVisibilityManager.instance.Hide(SceneManager.GetSceneAt(i));
			}
			
			EditorSceneManager.OpenScene($"{Application.dataPath}/Scenes/Editor/scn_LevelEditor.unity", OpenSceneMode.Additive);
			SceneVisibilityManager.instance.DisableAllPicking();
			
			_grid = FindFirstObjectByType<EditorHexGrid>();
			_grid.SpawnGrid();
		}

		public void CreateGUI()
		{
			var createButton = new Button(() => _grid.SpawnGrid())
			{
				text = "Spawn Grid"
			};
			var clearButton = new Button(() => _grid.ForceClear())
			{
				text = "Clear Grid"
			};
			
			rootVisualElement.Add(createButton);
			rootVisualElement.Add(clearButton);
		}
	}
}