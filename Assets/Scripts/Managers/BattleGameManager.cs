using Hex.Grid;
using UnityEngine;
using static Hex.Grid.HexGridInteractionManager;

namespace Hex.Managers
{
    public class BattleGameManager : MonoBehaviour, IGameManager
    {
        [SerializeField] private HexGrid grid;
        [SerializeField] private UnitManager unitManager;
        [SerializeField] private HexGridInteractionManager interactionManager;
        
        private void Awake()
        {
            ApplicationManager.RegisterResource(this);
        }

        public void Play()
        {
            grid.Load(GameMode.Battle);
            interactionManager.SetSelectionMode(SelectionMode.Path);

            interactionManager.CellClicked += OnCellClicked;
            interactionManager.BlockInteractions = false;
        }

        public void Leave() 
        { 
            interactionManager.CellClicked -= OnCellClicked;
            interactionManager.BlockInteractions = true;
        }

        private void OnCellClicked(HexCell hexCell)
        {
            unitManager.SpawnUnitOnCell(hexCell, "grunt");
        }
    }
}