using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Enemy;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.Model;
using Hex.UI;
using Hex.Util;
using UnityEngine;
using UnityEngine.Pool;
using static Hex.Grid.HexGridInteractionHandler;

namespace Hex.Managers
{
    public class GridInteractionManager : MonoBehaviour
    {
        [Space]
        [SerializeField] private HexGridInteractionHandler _interactionHandler;
        [SerializeField] private HexGrid grid;
        [SerializeField] private GridMoveUI moveUi;

        [Space] 
        [Header("Cell Selecting")] 
        [SerializeField] private Color cellOutlineCanCombine;
        [SerializeField] private Color cellOutlineCannotCombine;

        [Space]
        [SerializeField] private EnemyAttackManager _attackManager;

        [SerializeField] private float mergePulseIntensity = .75f;
        [SerializeField] private float mergeUpgradePulseIntensity = 1.5f;

        private BattleModel _battleModel;
        private BattleConfig _battleConfig;

        public Func<UnitData> SpawnUnit;
        public event Action GridStateChanged;
        
        public HexGrid Grid => grid;

        #region Setup/Game State
        
        public void Initialize()
        {
            _interactionHandler.BlockInteractions = false;
            _interactionHandler.SetSelectionMode(SelectionMode.Outline);
            
            _interactionHandler.CellClicked += TryPlaceUnit;
            _interactionHandler.CellsDragReleased += OnCellDragReleased;
            _interactionHandler.CellsDragContinue += OnCellsDragContinued;
            
            grid.Load();

            _battleModel = ApplicationManager.GetResource<BattleModel>();
        }

        public void Dispose()
        {
            _interactionHandler.BlockInteractions = true;

            _interactionHandler.CellClicked -= TryPlaceUnit;
            _interactionHandler.CellsDragReleased -= OnCellDragReleased;
            _interactionHandler.CellsDragContinue -= OnCellsDragContinued;
        }
        #endregion

        private void TryPlaceUnit(HexCell cell)
        {
            // Don't try to place when hand is empty
            // Can only place detail on empty tiles
            if (_battleModel.IsHandEmpty || cell.InfoHolder.HeldPlayerUnit)
            {
                return;
            }
            
            cell.InfoHolder.SpawnUnit(SpawnUnit?.Invoke());
            cell.Impact();

            grid.Save(GameMode.Merge);
            
            GridStateChanged?.Invoke();
        }
        
        private void OnCellDragReleased(List<HexCell> cells)
        {
            // Check if the cells can combine
            var (resultsInUpgrade, finalPower, finalRarity) = HexGameUtil.TryCombineUnits(cells, _battleModel.MaxMergeCount);

            var isValidMerge = finalPower > 0;
            var isValidMove = HexGameUtil.IsValidMove(cells) && _battleModel.RemainingUnitMoves > 0;
            
            if(isValidMerge) DoMerge(cells, resultsInUpgrade, finalPower, finalRarity);

            if (isValidMove)
            {
                // More than a simple move
                if (cells.Count > 2)
                {
                    // Check if cells must be merged, then moved
                    var subChain = cells.GetRange(0, cells.Count - 1);
                    (resultsInUpgrade, finalPower, finalRarity) = HexGameUtil.TryCombineUnits(subChain, _battleModel.MaxMergeCount);
                    if (finalPower > 0)
                    {
                        DoMerge(cells, resultsInUpgrade, finalPower, finalRarity);
                    }
                    DoMove(subChain[^1], cells[^1]);
                }
                else
                {
                    // Simple move
                    DoMove(cells[^2], cells[^1]);
                }
            }
        }
        
        private void OnCellsDragContinued(List<HexCell> cellsInChain)
        {
            // Clear all move arrows
            foreach (var cell in cellsInChain)
            {
                cell.ToggleMoveArrow(false);
            }
            
            // Check if the cells can combine
            var (resultsInUpgrade,finalPower, _) = HexGameUtil.TryCombineUnits(cellsInChain, _battleModel.MaxMergeCount);
            var validMerge = finalPower > 0;
            var validMove = HexGameUtil.IsValidMove(cellsInChain) && _battleModel.RemainingUnitMoves > 0;
            
            // Iterate through all cells and set their outline color
            foreach (var cell in cellsInChain)
            {
                cell.SetOutlineColor(validMerge || validMove ? cellOutlineCanCombine : cellOutlineCannotCombine);
                cell.UI.ToggleMergeCanvas(false);
            }

            // This is a merge
            if (validMerge)
            {
                var finalCell = cellsInChain.Last();
                finalCell.UI.ToggleMergeCanvas(true);
                finalCell.UI.SetMergeInfo(finalPower, resultsInUpgrade);
            }
            else if (validMove)
            {
                cellsInChain[^2].ToggleMoveArrow(true, cellsInChain[^1]);
            }
        }
        
        private async void DoMerge(List<HexCell> cells, bool resultsInUpgrade, int finalPower, int finalRarity)
        {
            var firstUnit = cells.First().InfoHolder.HeldPlayerUnit;
            var last = cells.Last();
            
            var tasks = ListPool<Task>.Get();
                
            foreach (var c in cells)
            {
                tasks.Add(MoveAndCombineUnit(c, last, c != last));
            }
                
            await Task.WhenAll(tasks);
            last.Pulse(resultsInUpgrade ? mergeUpgradePulseIntensity : mergePulseIntensity);
            
            // Resolve the combination, using the first unit as a unit override
            last.InfoHolder.ResolveCombine(finalPower, finalRarity, resultsInUpgrade, firstUnit);
            
            GridStateChanged?.Invoke();
        }

        private async void DoMove(HexCell fromCell, HexCell toCell)
        {
            toCell.InfoHolder.ResolveCombine(fromCell.InfoHolder.PlayerPower, fromCell.InfoHolder.PlayerRarity, false, fromCell.InfoHolder.HeldPlayerUnit);
            await MoveAndCombineUnit(fromCell, toCell, true);

            moveUi.SetCount(--_battleModel.RemainingUnitMoves);
        }
        
        private static async Task MoveAndCombineUnit(HexCell fromCell, HexCell toCell, bool removeAsResult)
        {
            const float lerpTimeSeconds = .25f;
            var startPosition = fromCell.InfoHolder.UnitAnchor.position;
            var endPosition = toCell.InfoHolder.UnitAnchor.position;
            
            if (removeAsResult) fromCell.UI.ToggleUnitInfoCanvas(false);

            await MathUtil.DoInterpolation(lerpTimeSeconds, DoProgress);
            fromCell.InfoHolder.UnitAnchor.transform.position = endPosition;
            
            if (removeAsResult) fromCell.InfoHolder.ClearUnit();

            void DoProgress(float progress)
            {
                fromCell.InfoHolder.UnitAnchor.transform.position = MathUtil.SmoothLerp(startPosition, endPosition, progress);
            }
        }
    }
}