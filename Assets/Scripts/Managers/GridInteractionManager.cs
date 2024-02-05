using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Enemy;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.Model;
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

        [Space] 
        [Header("Cell Selecting")] 
        [SerializeField] private Color cellOutlineCanCombine;
        [SerializeField] private Color cellOutlineCannotCombine;

        [Space]
        [SerializeField] private EnemyAttackManager _attackManager;

        [SerializeField] private float mergePulseIntensity = .75f;
        [SerializeField] private float mergeUpgradePulseIntensity = 1.5f;

        private BattleModel _battleModel;

        public Func<UnitData> SpawnUnit;
        public event Action GridStateChanged;
        
        public HexGrid Grid => grid;

        #region Setup/Game State
        
        public void Initialize()
        {
            _interactionHandler.BlockInteractions = false;
            _interactionHandler.SetSelectionMode(SelectionMode.Outline);
            
            _interactionHandler.CellClicked += TryPlaceUnit;
            _interactionHandler.CellsDragReleased += TryCombineCells;
            _interactionHandler.CellsDragContinue += OnCellsDragContinued;
            
            grid.Load();

            _battleModel = ApplicationManager.GetResource<BattleModel>();
        }

        public void Dispose()
        {
            _interactionHandler.BlockInteractions = true;

            _interactionHandler.CellClicked -= TryPlaceUnit;
            _interactionHandler.CellsDragReleased -= TryCombineCells;
            _interactionHandler.CellsDragContinue -= OnCellsDragContinued;
        }
        #endregion

        private void TryPlaceUnit(HexCell cell)
        {
            // Don't try to place when hand is empty
            // Can only place detail on empty tiles
            if (_battleModel.IsHandEmpty || cell.InfoHolder.HeldPlayerUnit || _attackManager.IsAttackPhase)
            {
                return;
            }
            
            cell.InfoHolder.SpawnUnit(SpawnUnit?.Invoke());
            cell.Impact();

            grid.Save(GameMode.Merge);
            
            GridStateChanged?.Invoke();
        }
        
        private async void TryCombineCells(List<HexCell> cells)
        {
            // Check if the cells can combine
            var (resultsInUpgrade, finalPower, finalRarity) = HexGameUtil.TryCombineUnits(cells);
            
            // Invalid combine
            if (finalPower <= 0)
            {
                return;
            }
            
            var firstUnit = cells.First().InfoHolder.HeldPlayerUnit;
            var last = cells.Last();
            
            var tasks = ListPool<Task>.Get();
                
            foreach (var c in cells)
            {
                tasks.Add(MoveAndCombineDetail(c, last, c != last));
            }
                
            await Task.WhenAll(tasks);
            last.Pulse(resultsInUpgrade ? mergeUpgradePulseIntensity : mergePulseIntensity);
            
            // Resolve the combination, using the first unit as a unit override
            last.InfoHolder.ResolveCombine(finalPower, finalRarity, resultsInUpgrade, firstUnit);
            
            GridStateChanged?.Invoke();
        }

        private void OnCellsDragContinued(List<HexCell> cells)
        {
            // Check if the cells can combine
            var (resultsInUpgrade,finalPower, _) = HexGameUtil.TryCombineUnits(cells);

            // Iterate through all cells and set their outline color
            foreach (var cell in cells)
            {
                cell.SetOutlineColor(finalPower > 0 ? cellOutlineCanCombine : cellOutlineCannotCombine);
                cell.UI.ToggleMergeCanvas(false);
            }

            // Invalid combine, don't show any merge info
            if (finalPower == -1) return;

            var finalCell = cells.Last();
            finalCell.UI.ToggleMergeCanvas(true);
            finalCell.UI.SetMergeInfo(finalPower, resultsInUpgrade);
        }
        
        private static async Task MoveAndCombineDetail(HexCell fromCell, HexCell toCell, bool removeAsResult)
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