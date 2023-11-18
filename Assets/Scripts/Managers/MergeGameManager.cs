using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hex.Data;
using Hex.Grid;
using Hex.Grid.Cell;
using Hex.Grid.DetailQueue;
using Hex.Model;
using Hex.UI;
using Hex.UI.Popup;
using Hex.Util;
using UnityEngine;
using UnityEngine.Pool;
using static Hex.Grid.HexGridInteractionManager;
using Random = System.Random;

namespace Hex.Managers
{
    public class MergeGameManager : MonoBehaviour, IGameManager
    {
        private static Random _random;
        
        [Header("Starting Configuration")]
        [SerializeField] private List<UnitData> startingDeck;
        
        [Space]
        [SerializeField] private HexGridInteractionManager interactionManager;
        [SerializeField] private DeckPreviewQueue deckPreviewQueue;
        [SerializeField] private HexGrid grid;
        [SerializeField] private HexGrid enemyGrid;

        [Space] [Header("Cell Selecting")] 
        [SerializeField] private Color cellOutlineCanCombine;
        [SerializeField] private Color cellOutlineCannotCombine;
        
        [Header("UI")]
        [SerializeField] private GameUI gameUI;
        [SerializeField] private PopupsUI popupUI;
        [SerializeField] private TopBarUI topBarUI;
        
        [SerializeField] private TownCompletePopup townCompletePopupPrefab;

        private readonly MergeGameModel _model = new();
        private readonly List<UnitData> _deck = new();
        private readonly List<UnitData> _discard = new();
        private bool _deckRefilled;

        #region Setup/Game State
        private void Awake()
        {
            ApplicationManager.RegisterResource(this);
            _random = new Random();

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
            interactionManager.BlockInteractions = false;
            interactionManager.SetSelectionMode(SelectionMode.Outline);
            
            interactionManager.CellClicked += TryPlaceDetail;
            interactionManager.CellsDragReleased += TryCombineCells;
            interactionManager.CellsDragContinue += OnCellsDragContinued;
            
            gameUI.gameObject.SetActive(true);

            FillInitialDeck();
            deckPreviewQueue.Initialize(GetUnitAtIndex);
            
            deckPreviewQueue.gameObject.SetActive(true);
            gameUI.DeckPreviewQueue.gameObject.SetActive(true);
            
            var existingGrid = grid.Load(GameMode.Merge);
            grid.Empty();
            enemyGrid.Load(GameMode.Merge);
            enemyGrid.Empty();

            deckPreviewQueue.GeneratePreviewQueue();
            gameUI.DeckPreviewQueue.Initialize(_deck.FirstOrDefault(), startingDeck.Count);
        }

        public void Leave()
        {
            interactionManager.BlockInteractions = true;
            
            interactionManager.CellClicked -= TryPlaceDetail;
            interactionManager.CellsDragReleased -= TryCombineCells;
            interactionManager.CellsDragContinue -= OnCellsDragContinued;
            
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

        private void TownComplete()
        {
            var popup = Instantiate(townCompletePopupPrefab);
            popup.TotalScore.SetScoreImmediate(_model.ResourceAmounts[ResourceType.CoinSilver], true);
            popup.ClaimPressed = OnClaimPressed;
            
            popupUI.AddChild(popup);
            popupUI.ToggleInputBlock(true);

            async void OnClaimPressed()
            {
                await popup.DoClaim(topBarUI, _model);
                
                _model.SetResourceAmount(ResourceType.CoinGold, _model.ResourceAmounts[ResourceType.CoinSilver]);
                _model.SetResourceAmount(ResourceType.CoinSilver, 0);
                topBarUI.SetResourceImmediate(ResourceType.CoinSilver, 0);
                
                Destroy(popup.gameObject);
                popupUI.ToggleInputBlock(false);
                ResetGrid();
                ApplicationManager.GetResource<NavigationManager>().GoToMainMenu();
            }
        }
        #endregion

        private void FillInitialDeck()
        {
            foreach (var detail in startingDeck)
            {
                _deck.Add(detail);
            }
        }
        
        private void ResetGrid()
        {
            foreach (var kvp in grid.Registry)
            {
                kvp.Value.InfoHolder.Clear();
            }
        }
        
        private void TryPlaceDetail(HexCell cell)
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
            Debug.Log($"Removing detail. New deck size: {_deck.Count}");
            
            // Deck is empty
            if (_deck.Count == 0)
            {
                Debug.Log("Deck is empty, shuffling in discard");
                // Shuffle discard into deck
                for (var i = _discard.Count - 1; i >= 0; i--)
                {
                    var detailIndex = UnityEngine.Random.Range(0, _discard.Count);
                    _deck.Add(_discard[detailIndex]);
                    _discard.RemoveAt(detailIndex);
                }

                _deckRefilled = true;
                Debug.Log($"New deck size: {_deck.Count}");
            }

            // Try to create a snapshot of the next 3 details in the queue so they can be asynchronously processed
            var queueSnapshot = ListPool<UnitData>.Get();
            queueSnapshot.Clear();
            for (var i = 0; i < 3; i++)
            {
                queueSnapshot.Add(GetUnitAtIndex(i));
            }
            deckPreviewQueue.Dequeue(queueSnapshot);
            CheckGameOver();
            
            grid.Save(GameMode.Merge);

            void CheckGameOver()
            {
                // Check Game Over
                // var allCells = grid.Registry.Values;
                // if (allCells.Any(c => c.Detail.Type == MergeCellDetailType.Empty))
                // {
                //     return;
                // }
                // TODO Fix the can any combine check if needed
                // if (!HexGameUtil.CanAnyCombineOnGrid(allCells.ToList(), config))
                // {
                //     TownComplete();
                // }
            }
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
        
        private async void TryCombineCells(List<HexCell> cells)
        {
            var cellsWithDetails = cells.Where(c => c.InfoHolder.HeldUnit);
            var withDetails = cellsWithDetails as List<HexCell> ?? cellsWithDetails.ToList();

            var canCombine = false;//HexGameUtil.TryCombine(withDetails, config);

            if (!canCombine)
            {
                return;
            }
            
            var tasks = ListPool<Task>.Get();
            var last = withDetails.Last();
            withDetails.Remove(last);
                
            foreach (var c in withDetails)
            {
                tasks.Add(MoveAndCombineDetail(c, last));
            }
                
            await Task.WhenAll(tasks);
            //last.Detail.SetType(newType.Value);
            //var combinedTiles = withDetails.Count + 1;
            
            // Get score for combined tiles
            // var (resource, amount) = config.GetPointsForType(last.Detail.Type);
            // var multiplierForCombine = config.GetMultiplierForCombination(combinedTiles);
            // var toAdd = Mathf.RoundToInt(amount * multiplierForCombine);
            // _model.ModifyResourceAmount(resource, toAdd);
            // 
            // gameUI.TopBar.AddResourceFromTile(last, combinedTiles, resource, amount, multiplierForCombine, toAdd);
// 
            // grid.Save(GameMode.Merge);
        }

        private UnitData GetUnitAtIndex(int index)
        {
            if (index < _deck.Count)
            {
                return _deck[index];
            }
            return null;
        }

        private void OnCellsDragContinued(List<HexCell> cells)
        {
            var cellsWithDetails = cells.Where(c => c.InfoHolder.HeldUnit);
            var withDetails = cellsWithDetails as List<HexCell> ?? cellsWithDetails.ToList();

            // Check if the cells can combine
            var canCombine = false;// HexGameUtil.TryCombine(withDetails, config);

            // Check if the cells are all of the same type (to see if they can contribute to a combination)
            var allSameType = withDetails.All(c => c.InfoHolder.HeldUnit.UniqueId == withDetails.First().InfoHolder.HeldUnit.UniqueId);
            // Number to match is 3 for regular details and 4 for stones
            const int numToMatch = 3;
            // Iterate through all cells and set their outline color
            for (int index = 0, withDetailIndex = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                cell.SetOutlineColor(canCombine ? cellOutlineCanCombine : cellOutlineCannotCombine);
                cell.UI.ToggleMergeCanvas(false);

                if (!allSameType) continue;
                
                // increment the amount of cells contributing to a combination
                cell.SetMatchCount(withDetailIndex + 1, numToMatch);
                if (cell.InfoHolder.HeldUnit) withDetailIndex++;
            }

            // Toggle the canvas to display the match count over the last relevant cell
            for (var index = cells.Count - 1; index >= 0; index--)
            {
                var cell = cells[index];
                if (cell.InfoHolder.HeldUnit) continue;
                cell.UI.ToggleMergeCanvas(true);
                cell.ToggleCanCombine(allSameType);
                break;
            }
        }
        
        private static async Task MoveAndCombineDetail(HexCell fromCell, HexCell toCell)
        {
            const float lerpTimeSeconds = .25f;
            var startPosition = fromCell.InfoHolder.UnitAnchor.position;
            var endPosition = toCell.InfoHolder.UnitAnchor.position;

            await MathUtil.DoInterpolation(lerpTimeSeconds, DoProgress);

            fromCell.InfoHolder.UnitAnchor.transform.position = endPosition;
            fromCell.InfoHolder.Clear();

            void DoProgress(float progress)
            {
                fromCell.InfoHolder.UnitAnchor.transform.position = MathUtil.SmoothLerp(startPosition, endPosition, progress);
            }
        }
    }
}