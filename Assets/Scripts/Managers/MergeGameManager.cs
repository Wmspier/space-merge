using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Enemy;
using Hex.Extensions;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.Grid.DetailQueue;
using Hex.Model;
using Hex.UI;
using Hex.Util;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR;
using static Hex.Grid.HexGridInteractionHandler;

namespace Hex.Managers
{
    public class MergeGameManager : MonoBehaviour
    {
        [SerializeField] private List<UnitData> startingDeck;
        [SerializeField] private int startingHandSize = 4;
        
        [Space]
        [SerializeField] private HexGridInteractionHandler _interactionHandler;
        [SerializeField] private DeckPreviewQueue deckPreviewQueue;
        [SerializeField] private HexGrid grid;

        [Space] [Header("Cell Selecting")] 
        [SerializeField] private Color cellOutlineCanCombine;
        [SerializeField] private Color cellOutlineCannotCombine;
        
        [Header("UI")]
        [SerializeField] private GameUI gameUI;
        [SerializeField] private TopBarUI topBarUI;
        
        [Space]
        [SerializeField] private EnemyAttackManager _attackManager;

        [SerializeField] private float mergePulseIntensity = .75f;
        [SerializeField] private float mergeUpgradePulseIntensity = 1.5f;

        private readonly ResourcesModel _resourceModel = new();
        private readonly List<UnitData> _hand = new();
        private readonly List<UnitData> _deck = new();
        private readonly List<UnitData> _discard = new();
        private bool _deckRefilled;
        private GameStateModel _gameStateModel;

        #region Setup/Game State
        private void Awake()
        {
            deckPreviewQueue.DetailDequeued = OnDetailDequeued;
            gameUI.ResetPressed = OnResetPressed;
            
            _resourceModel.Load();
            foreach (var (resource, amount) in _resourceModel.ResourceAmounts)
            {
                topBarUI.SetResourceImmediate(resource, amount);
            }

            _gameStateModel = new GameStateModel(grid);
            
            ApplicationManager.RegisterResource(this);
            ApplicationManager.RegisterResource(_resourceModel);
            ApplicationManager.RegisterResource(_gameStateModel);
        }
        
        public void Play()
        {
            _attackManager.ResetTurns();
            
            _interactionHandler.BlockInteractions = false;
            _interactionHandler.SetSelectionMode(SelectionMode.Outline);
            
            _interactionHandler.CellClicked += TryPlaceUnit;
            _interactionHandler.CellsDragReleased += TryCombineCells;
            _interactionHandler.CellsDragContinue += OnCellsDragContinued;
            
            gameUI.gameObject.SetActive(true);

            FillInitialDeck();
            FillHand();
            SetupPreviewQueue();
            
            grid.Load();
            
            _attackManager.Initialize(grid);
            _attackManager.AttackResolved = OnAttackResolved;
            _attackManager.AssignAttacksToGrid();
        }

        private void SetupPreviewQueue()
        {
            deckPreviewQueue.Initialize(GetUnitFromHand, _hand.Count);
            deckPreviewQueue.GeneratePreviewQueue();
            deckPreviewQueue.gameObject.SetActive(true);
            
            gameUI.PreviewQueueUI.Initialize(_hand.FirstOrDefault(), _hand.Count);
            gameUI.PreviewQueueUI.gameObject.SetActive(true);
        }

        public void Leave()
        {
            _interactionHandler.BlockInteractions = true;
            
            _interactionHandler.CellClicked -= TryPlaceUnit;
            _interactionHandler.CellsDragReleased -= TryCombineCells;
            _interactionHandler.CellsDragContinue -= OnCellsDragContinued;
            
            deckPreviewQueue.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
            
            _attackManager.Cleanup();
            
            _deck.Clear();
            _hand.Clear();
            _discard.Clear();
        }

        private void OnResetPressed()
        {
            ResetGrid();
            deckPreviewQueue.gameObject.SetActive(false);
            gameUI.PreviewQueueUI.gameObject.SetActive(false);

            gameUI.TopBar.Clear();
            deckPreviewQueue.GeneratePreviewQueue();
            gameUI.PreviewQueueUI.Initialize(_hand.FirstOrDefault(), _hand.Count);
            
            deckPreviewQueue.gameObject.SetActive(true);
            gameUI.PreviewQueueUI.gameObject.SetActive(true);
        }
        #endregion

        private void FillInitialDeck()
        {
            foreach (var detail in startingDeck)
            {
                _deck.Add(detail);
            }
            _deck.Shuffle();
        }
        
        private void ResetGrid()
        {
            foreach (var kvp in grid.Registry)
            {
                kvp.Value.InfoHolder.Clear();
            }
        }
        
        private void TryPlaceUnit(HexCell cell)
        {
            // Don't try to place when hand is empty
            // Can only place detail on empty tiles
            if (_hand.Count == 0 || cell.InfoHolder.HeldPlayerUnit || _attackManager.IsAttackPhase)
            {
                return;
            }
            
            cell.InfoHolder.SpawnUnit(GetUnitFromHand(0));
            cell.Impact();
            
            var detail = _hand[0];
            _discard.Add(detail);
            _hand.RemoveAt(0);
            
            // Hand is empty
            if (_hand.Count == 0)
            {
                Debug.Log("Hand is Empty");
            }

            // Try to create a snapshot of the next details in the queue so they can be asynchronously processed
            var queueSnapshot = ListPool<UnitData>.Get();
            queueSnapshot.Clear();
            UnitData unitPreview;
            var index = 0;
            do
            {
                unitPreview = GetUnitFromHand(index);
                if (unitPreview != null) queueSnapshot.Add(unitPreview);
                index++;
            } while (unitPreview != null);
            deckPreviewQueue.Dequeue(queueSnapshot);

            grid.Save(GameMode.Merge);
            
            // if (_attackManager.ElapseTurn())
            // {
            //     deckPreviewQueue.gameObject.SetActive(false);
            //     gameUI.DeckPreviewQueue.gameObject.SetActive(false);
            // }
            
            _attackManager.UpdateDamagePreview();
        }

        private void DrawNewHand()
        {
            _discard.AddRange(_hand);
            _hand.Clear();
            FillHand();
        }
        
        private void FillHand()
        {
            for (var i = 0; i < startingHandSize; i++)
            {
                if (_deck.Count == 0)
                {
                    ShuffleDiscardIntoDeck();
                    if (_deck.Count == 0) return;
                }
                
                _hand.Add(_deck[0]);
                _deck.RemoveAt(0);
            }
        }

        private void ShuffleDiscardIntoDeck()
        {
            _deck.AddRange(_discard);
            _deck.Shuffle();
            _discard.Clear();
        }

        private void OnAttackResolved()
        {
            deckPreviewQueue.gameObject.SetActive(true);
            gameUI.PreviewQueueUI.gameObject.SetActive(true);
            DrawNewHand();
            SetupPreviewQueue();
        }
        
        private async void OnDetailDequeued()
        {
            await gameUI.PreviewQueueUI.SetNextAndDecrement(_hand.FirstOrDefault(), _hand.Count);

            if (!_deckRefilled) return;
            
            while (deckPreviewQueue.ProcessingDequeues)
            {
                await Task.Delay(10);
            }
            gameUI.PreviewQueueUI.Initialize(GetUnitFromHand(0), _hand.Count);
            _deckRefilled = false;
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
            
            _attackManager.UpdateDamagePreview();
        }

        private UnitData GetUnitFromHand(int index)
        {
            return index < _hand.Count ? _hand[index] : null;
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