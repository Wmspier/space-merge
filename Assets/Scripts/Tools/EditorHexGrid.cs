using System.Collections.Generic;
using System.Linq;
using Hex.Grid;
using Hex.Grid.Cell;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Tools
{
	public class EditorHexGrid : HexGrid
	{
		[Header("Level Editor")]
		[SerializeField] private Material _disabledCellMaterial; // Cell should not be displayed
		[SerializeField] private Material _unplayableCellMaterial; // Cell is displayed but not playable
		[SerializeField] private Material _playableCellMaterial; // Cell is enable and playable

		[SerializeField] private EditorHexGridInteractionHandler _interactionHandler;

		public string LevelFileName { get; set; }
		
		private void Awake()
		{
			_interactionHandler.CellClicked += OnCellClicked;
			_interactionHandler.CellsDragContinue += OnCellDragContinue;

			ForceClear();
		}

		public override void Load(bool immediateDestroy = false, List<HexCellDefinition> definitions = null, bool forceSpawnCells = true)
		{
			base.Load(immediateDestroy, definitions, forceSpawnCells);

			foreach (var (coord, cell) in Registry)
			{
				SetCellMaterialByState(cell, cell.State);
			}
		}

		public void LoadFromFile(string fileName)
		{
			if (LevelEditorUtility.LevelExists(fileName))
			{
				Debug.Log($"Found level {fileName}. Loading");
				Load(true, LevelEditorUtility.LoadLevel(fileName));
			}
			else
			{
				Debug.LogError($"Failed to find file with name: {fileName}");
			}
		}
		
		public void Save(string fileName)
		{
			LevelEditorUtility.SaveLevel(fileName, GetCellDefinitions());
			Debug.Log($"Grid Saved: {fileName}");
		}

		private List<HexCellDefinition> GetCellDefinitions()
		{
			var definitions = new List<HexCellDefinition>();
			foreach (var (coord, cell) in Registry)
			{
				definitions.Add(new HexCellDefinition
				{
					Coordinates = new int3(coord.x - numEdgeCells, coord.y - numEdgeCells, coord.z - numEdgeCells),
					State = (int)cell.State
				});
			}

			return definitions;
		}


		private void OnCellDragContinue(List<HexCell> hexCells)
		{
			OnCellClicked(hexCells.Last());
		}
		
		private void OnCellClicked(HexCell cell)
		{
			var state = Registry[cell.Coordinates].State;
			var newState = state switch
			{
				CellState.Playable => CellState.Unplayable,
				CellState.Unplayable => CellState.Disabled,
				CellState.Disabled => CellState.Playable
			};

			cell.ApplyState(newState);
			SetCellMaterialByState(cell, newState);
		}

		private void SetCellMaterialByState(HexCell cell, CellState state)
		{
			cell.PrimaryMesh.material = state switch
			{
				CellState.Disabled => _disabledCellMaterial,
				CellState.Unplayable => _unplayableCellMaterial,
				CellState.Playable => _playableCellMaterial
			};
		}
	}
}