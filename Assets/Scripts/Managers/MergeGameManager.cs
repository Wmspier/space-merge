using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hex.Configuration;
using Hex.Extensions;
using Hex.Grid;
using Hex.Grid.DetailQueue;
using Hex.Model;
using Hex.UI;
using Hex.UI.Popup;
using Hex.Util;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using static Hex.Grid.HexGridInteractionManager;
using Random = System.Random;

namespace Hex.Managers
{
    public class MergeGameManager : MonoBehaviour, IGameManager
    {
        private static Random _random;
        
        [Header("Starting Configuration")]
        [SerializeField] private List<MergeCellDetailType> startingDeck;
        
        [Space]
        [SerializeField] private HexGridInteractionManager interactionManager;
        [SerializeField] private CellDetailQueue detailQueue;
        [SerializeField] private HexGrid grid;
        [SerializeField] private HexGrid enemyGrid;
        [SerializeField] private HexDetailConfiguration config;

        [Space] [Header("Cell Selecting")] 
        [SerializeField] private Color cellOutlineCanCombine;
        [SerializeField] private Color cellOutlineCannotCombine;
        
        [Header("UI")]
        [SerializeField] private GameUI gameUI;
        [SerializeField] private PopupsUI popupUI;
        [SerializeField] private TopBarUI topBarUI;
        [SerializeField] private Button moreTilesButton;
        [SerializeField] private GameObject noTilesLeftRoot;
        
        [SerializeField] private TownCompletePopup townCompletePopupPrefab;

        private readonly MergeGameModel _model = new();
        private readonly List<MergeCellDetailType> _deck = new();
        private readonly List<MergeCellDetailType> _discard = new();
        private bool _deckRefilled;

        #region Setup/Game State
        private void Awake()
        {
            ApplicationManager.RegisterResource(this);
            _random = new Random();

            detailQueue.DetailDequeued = OnDetailDequeued;
            
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
            detailQueue.Initialize(GetNextDetailAtIndex);
            
            detailQueue.gameObject.SetActive(true);
            gameUI.DetailQueue.gameObject.SetActive(true);
            
            var existingGrid = grid.Load(GameMode.Merge);
            enemyGrid.Load(GameMode.Merge);

            detailQueue.GeneratePreviewQueue();
            gameUI.DetailQueue.Initialize(_deck.FirstOrDefault(), startingDeck.Count);
        }

        public void Leave()
        {
            interactionManager.BlockInteractions = true;
            
            interactionManager.CellClicked -= TryPlaceDetail;
            interactionManager.CellsDragReleased -= TryCombineCells;
            interactionManager.CellsDragContinue -= OnCellsDragContinued;
            
            detailQueue.gameObject.SetActive(false);
            gameUI.gameObject.SetActive(false);
        }

        private void OnResetPressed()
        {
            ResetGrid();
            detailQueue.gameObject.SetActive(false);
            gameUI.DetailQueue.gameObject.SetActive(false);

            gameUI.TopBar.Clear();
            detailQueue.GeneratePreviewQueue();
            gameUI.DetailQueue.Initialize(_deck.FirstOrDefault(), startingDeck.Count);
            
            detailQueue.gameObject.SetActive(true);
            gameUI.DetailQueue.gameObject.SetActive(true);
            noTilesLeftRoot.SetActive(false);
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
                kvp.Value.Detail.Clear();
            }
        }
        
        private void TryPlaceDetail(HexCell cell)
        {
            // Don't try to place when deck is refilling
            // Can only place detail on empty tiles
            if (_deckRefilled || cell.Detail.Type != MergeCellDetailType.Empty)
            {
                return;
            }
            cell.Detail.SetType(GetNextDetailAtIndex(0));
            
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
            var queueSnapshot = ListPool<MergeCellDetailType>.Get();
            queueSnapshot.Clear();
            for (var i = 0; i < 3; i++)
            {
                queueSnapshot.Add(GetNextDetailAtIndex(i));
            }
            detailQueue.Dequeue(queueSnapshot);
            CheckGameOver();
            
            grid.Save(GameMode.Merge);

            void CheckGameOver()
            {
                // Check Game Over
                var allCells = grid.Registry.Values;
                if (allCells.Any(c => c.Detail.Type == MergeCellDetailType.Empty))
                {
                    return;
                }
                // TODO Fix the can any combine check if needed
                // if (!HexGameUtil.CanAnyCombineOnGrid(allCells.ToList(), config))
                // {
                //     TownComplete();
                // }
            }
        }
        
        private async void OnDetailDequeued()
        {
            await gameUI.DetailQueue.SetNextAndDecrement(_deck.FirstOrDefault(), _deck.Count);

            if (!_deckRefilled) return;
            
            while (detailQueue.ProcessingDequeues)
            {
                await Task.Delay(10);
            }
            detailQueue.GeneratePreviewQueue();
            gameUI.DetailQueue.Initialize(GetNextDetailAtIndex(0), _deck.Count);
            _deckRefilled = false;
        }
        
        private async void TryCombineCells(List<HexCell> cells)
        {
            var cellsWithDetails = cells.Where(c => c.Detail.Type > MergeCellDetailType.Empty);
            var withDetails = cellsWithDetails as List<HexCell> ?? cellsWithDetails.ToList();

            var (canCombine, newType) = HexGameUtil.TryCombine(withDetails, config);

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
            last.Detail.SetType(newType.Value);
            var combinedTiles = withDetails.Count + 1;
            
            // Get score for combined tiles
            var (resource, amount) = config.GetPointsForType(last.Detail.Type);
            var multiplierForCombine = config.GetMultiplierForCombination(combinedTiles);
            var toAdd = Mathf.RoundToInt(amount * multiplierForCombine);
            _model.ModifyResourceAmount(resource, toAdd);
            
            gameUI.TopBar.AddResourceFromTile(last, combinedTiles, resource, amount, multiplierForCombine, toAdd);

            grid.Save(GameMode.Merge);
        }

        private MergeCellDetailType GetNextDetailAtIndex(int index)
        {
            if (index < _deck.Count)
            {
                return _deck[index];
            }
            return MergeCellDetailType.Empty;
        }

        private void OnCellsDragContinued(List<HexCell> cells)
        {
            var cellsWithDetails = cells.Where(c => c.Detail.Type > MergeCellDetailType.Empty);
            var withDetails = cellsWithDetails as List<HexCell> ?? cellsWithDetails.ToList();

            // Check if the cells can combine
            var (canCombine, _) = HexGameUtil.TryCombine(withDetails, config);

            // Check if the cells are all of the same time (to see if they can contribute to a combination)
            var allSameType = withDetails.All(c => c.Detail.Type == withDetails.First().Detail.Type);
            // Number to match is 3 for regular details and 4 for stones
            const int numToMatch = 3;
            // Iterate through all cells and set their outline color
            for (int index = 0, withDetailIndex = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                cell.SetOutlineColor(canCombine ? cellOutlineCanCombine : cellOutlineCannotCombine);
                cell.ToggleCanvas(false);

                if (!allSameType) continue;
                
                // increment the amount of cells contributing to a combination
                cell.SetMatchCount(withDetailIndex + 1, numToMatch);
                if (cell.Detail.Type > MergeCellDetailType.Empty) withDetailIndex++;
            }

            // Toggle the canvas to display the match count over the last relevant cell
            for (var index = cells.Count - 1; index >= 0; index--)
            {
                var cell = cells[index];
                if (cell.Detail.Type == MergeCellDetailType.Empty) continue;
                cell.ToggleCanvas(true);
                cell.ToggleCanCombine(allSameType);
                break;
            }
        }
        
        private static async Task MoveAndCombineDetail(HexCell fromCell, HexCell toCell)
        {
            const float lerpTimeSeconds = .25f;
            var startPosition = fromCell.Detail.Anchor.position;
            var endPosition = toCell.Detail.Anchor.position;

            await MathUtil.DoInterpolation(lerpTimeSeconds, DoProgress);

            fromCell.Detail.Anchor.transform.position = endPosition;
            fromCell.Detail.Clear();

            void DoProgress(float progress)
            {
                fromCell.Detail.Anchor.transform.position = MathUtil.SmoothLerp(startPosition, endPosition, progress);
            }
        }
    }
}