using System.Collections.Generic;
using System.Linq;
using Hex.Grid;
using Hex.Grid.Cell;
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

		[SerializeField] private string TestFileName;

		private void Awake()
		{
			_interactionHandler.CellClicked += OnCellClicked;
			_interactionHandler.CellsDragContinue += OnCellDragContinue;

			if (LevelEditorUtility.LevelExists(TestFileName))
			{
				Load(true, LevelEditorUtility.LoadLevel(TestFileName));
			}
			else
			{
				Load(true);
			}
		}

		public override bool Load(bool immediateDestroy = false, List<HexCellDefinition> definitions = null)
		{
			var baseLoad = base.Load(immediateDestroy, definitions);

			foreach (var (coord, cell) in Registry)
			{
				SetCellMaterialByState(cell, cell.State);
			}

			return baseLoad;
		}
		
		[ContextMenu("Save Grid")]
		public void Save()
		{
			LevelEditorUtility.SaveLevel(TestFileName, GetCellDefinitions());
			Debug.Log($"Grid Saved: {TestFileName}");
		}

		private List<HexCellDefinition> GetCellDefinitions()
		{
			var definitions = new List<HexCellDefinition>();
			foreach (var (coord, cell) in Registry)
			{
				definitions.Add(new HexCellDefinition
				{
					Coordinates = coord,
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