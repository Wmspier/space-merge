using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Extensions;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.Grid.DetailQueue;
using Hex.Model;
using Hex.UI;
using Hex.Util;
using UnityEngine;
using UnityEngine.Pool;
using static Hex.Grid.HexGridInteractionHandler;

namespace Hex.Managers
{
    public class MergeGameManager : MonoBehaviour, IGameManager
    {
        [SerializeField] private List<UnitData> startingDeck;
        
        [Space]
        [SerializeField] private HexGridInteractionHandler _interactionHandler;
        [SerializeField] private DeckPreviewQueue deckPreviewQueue;
        [SerializeField] private HexGrid grid;

        [Space] [Header("Cell Selecting")] 
        [SerializeField] private Color cellOutlineCanCombine;
        [SerializeField] private Color cellOutlineCannotCombine;
        
        [Header("UI")]
        [SerializeField] private GameUI gameUI;
        [SerializeField] private PopupsUI popupUI;
        [SerializeField] private TopBarUI topBarUI;

        private readonly MergeGameModel _model = new();
        private readonly List<UnitData> _deck = new();
        private readonly List<UnitData> _discard = new();
        private bool _deckRefilled;

        #region Setup/Game State
        private void Awake()
        {
            ApplicationManager.RegisterResource(this);

            deckPreviewQueue.DetailDequeued = OnDetailDequeued;
            gameUI.ResetPressed = OnResetPressed;
            
            _model.Load();
            foreach (var (resource, amount) in _model.ResourceAmounts)
            {
                topBarUI.SetResourceImmediate(resource, amount);
            }
        }
        
        public void Play()
        {
            _interactionHandler.BlockInteractions = false;
            _interactionHandler.SetSelectionMode(SelectionMode.Outline);
            
            _interactionHandler.CellClicked += TryPlaceUnit;
            _interactionHandler.CellsDragReleased += TryCombineCells;
            _interactionHandler.CellsDragContinue += OnCellsDragContinued;
            
            gameUI.gameObject.SetActive(true);

            FillInitialDeck();
            deckPreviewQueue.Initialize(GetUnitAtIndex);
            
            deckPreviewQueue.gameObject.SetActive(true);
            gameUI.DeckPreviewQueue.gameObject.SetActive(true);
            
            grid.Load(GameMode.Merge);
            grid.Empty();

            deckPreviewQueue.GeneratePreviewQueue();
            gameUI.DeckPreviewQueue.Initialize(_deck.FirstOrDefault(), startingDeck.Count);
        }

        public void Leave()
        {
            _interactionHandler.BlockInteractions = true;
            
            _interactionHandler.CellClicked -= TryPlaceUnit;
            _interactionHandler.CellsDragReleased -= TryCombineCells;
            _interactionHandler.CellsDragContinue -= OnCellsDragContinued;
            
            deckPreviewQueue.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
        }

        private void OnResetPressed()
        {
            ResetGrid();
            deckPreviewQueue.gameObject.SetActive(false);
            gameUI.DeckPreviewQueue.gameObject.SetActive(false);

            gameUI.TopBar.Clear();
            deckPreviewQueue.GeneratePreviewQueue();
            gameUI.DeckPreviewQueue.Initialize(_deck.FirstOrDefault(), startingDeck.Count);
            
            deckPreviewQueue.gameObject.SetActive(true);
            gameUI.DeckPreviewQueue.gameObject.SetActive(true);
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
            // Don't try to place when deck is refilling
            // Can only place detail on empty tiles
            if (_deckRefilled || cell.InfoHolder.HeldUnit)
            {
                return;
            }
            cell.InfoHolder.SpawnUnit(GetUnitAtIndex(0));
            
            var detail = _deck[0];
            _discard.Add(detail);
            _deck.RemoveAt(0);
            
            // Deck is empty
            if (_deck.Count == 0)
            {
                // Shuffle discard into deck
                for (var i = _discard.Count - 1; i >= 0; i--)
                {
                    var detailIndex = Random.Range(0, _discard.Count);
                    _deck.Add(_discard[detailIndex]);
                    _discard.RemoveAt(detailIndex);
                }

                _deckRefilled = true;
            }

            // Try to create a snapshot of the next 3 details in the queue so they can be asynchronously processed
            var queueSnapshot = ListPool<UnitData>.Get();
            queueSnapshot.Clear();
            for (var i = 0; i < 3; i++)
            {
                queueSnapshot.Add(GetUnitAtIndex(i));
            }
            deckPreviewQueue.Dequeue(queueSnapshot);
            
            grid.Save(GameMode.Merge);
        }
        
        private async void OnDetailDequeued()
        {
            await gameUI.DeckPreviewQueue.SetNextAndDecrement(_deck.FirstOrDefault(), _deck.Count);

            if (!_deckRefilled) return;
            
            while (deckPreviewQueue.ProcessingDequeues)
            {
                await Task.Delay(10);
            }
            deckPreviewQueue.GeneratePreviewQueue();
            gameUI.DeckPreviewQueue.Initialize(GetUnitAtIndex(0), _deck.Count);
            _deckRefilled = false;
        }
        
        private static async void TryCombineCells(List<HexCell> cells)
        {
            // Check if the cells can combine
            var (resultsInUpgrade, finalPower, finalRarity) = HexGameUtil.TryCombineUnits(cells);
            
            // Invalid combine
            if (finalPower <= 0)
            {
                return;
            }
            
            var firstUnit = cells.First().InfoHolder.HeldUnit;
            var last = cells.Last();
            
            var tasks = ListPool<Task>.Get();
                
            foreach (var c in cells)
            {
                tasks.Add(MoveAndCombineDetail(c, last, c != last));
            }
                
            await Task.WhenAll(tasks);
            
            // Resolve the combination, using the first unit as a unit override
            last.InfoHolder.ResolveCombine(finalPower, finalRarity, resultsInUpgrade, firstUnit);
        }

        private UnitData GetUnitAtIndex(int index)
        {
            return index < _deck.Count ? _deck[index] : null;
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
            
            if (removeAsResult) fromCell.InfoHolder.Clear();

            void DoProgress(float progress)
            {
                fromCell.InfoHolder.UnitAnchor.transform.position = MathUtil.SmoothLerp(startPosition, endPosition, progress);
            }
        }
    }
}