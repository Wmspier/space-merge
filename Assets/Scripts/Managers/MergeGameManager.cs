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
        [SerializeField] private int startingDetailsCount = 30;
        [SerializeField] private int startingMountainCount = 1;

        [Header("Spawn Weights")] 
        [SerializeField] private int stoneSpawnWeight = 4; // Chance to spawn stone
        [SerializeField] private int grassSpawnWeight = 10; // Chance to spawn grass
        [SerializeField] private int averageDetailSpawnWeight = 5; // Chance to spawn average detail across all cells
        
        [Space]
        [SerializeField] private HexGridInteractionManager interactionManager;
        [SerializeField] private CellDetailQueue detailQueue;
        [SerializeField] private HexGrid grid;
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

        private int _remainingDetails;
        private readonly MergeGameModel _model = new();
        private readonly List<MergeCellDetailType> _detailList = new();

        #region Setup/Game State
        private void Awake()
        {
            ApplicationManager.RegisterResource(this);
            _random = new Random();

            detailQueue.DetailDequeued = OnDetailDequeued;
            detailQueue.Initialize(GetNextDetailAtIndex);
            
            gameUI.ResetPressed = OnResetPressed;
            moreTilesButton.onClick.AddListener(OnMoreTilesPressed);
            
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

            PreloadDetailQueue();
            
            detailQueue.gameObject.SetActive(true);
            gameUI.DetailQueue.gameObject.SetActive(true);
            
            var existingGrid = grid.Load(GameMode.Merge);
            
            _remainingDetails = startingDetailsCount;
            
            detailQueue.GeneratePreviewQueue();
            gameUI.DetailQueue.Initialize(_detailList.FirstOrDefault(), _remainingDetails);

            if (!existingGrid)
            {
                RandomlyPlaceMountains();
            }
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

        private void RandomlyPlaceMountains()
        {
            var cellsWithStones = ListPool<HexCell>.Get();
            var cellCount = grid.Registry.Count;
            for (var i = 0; i < startingMountainCount; i++)
            {
                if (i == 0)
                {
                    var cell = grid.GetCenterCell();
                    cell.Detail.SetType(MergeCellDetailType.Mountain);
                    cellsWithStones.Add(cell);
                    continue;
                }
                
                var iterationCount = 0;
                HexCell foundCell;
                do
                {
                    var randomIndex = _random.Next(0, cellCount - 1);
                    foundCell = grid.Registry.ElementAt(randomIndex).Value;
                    if (iterationCount >= cellCount)
                    {
                        break;
                    }

                    iterationCount++;
                } while (cellsWithStones.Contains(foundCell));

                if (foundCell == null)
                {
                    return;
                }
                foundCell.Detail.SetType(MergeCellDetailType.Mountain);
                cellsWithStones.Add(foundCell);
            }
            grid.Save(GameMode.Merge);
        }

        private void OnResetPressed()
        {
            ResetGrid();
            detailQueue.gameObject.SetActive(false);
            gameUI.DetailQueue.gameObject.SetActive(false);

            gameUI.TopBar.Clear();
            detailQueue.GeneratePreviewQueue();
            gameUI.DetailQueue.Initialize(_detailList.FirstOrDefault(), _remainingDetails);
            
            detailQueue.gameObject.SetActive(true);
            gameUI.DetailQueue.gameObject.SetActive(true);
            noTilesLeftRoot.SetActive(false);
        }

        private void OnMoreTilesPressed()
        {
            _remainingDetails = startingDetailsCount;
            
            detailQueue.GeneratePreviewQueue();
            gameUI.DetailQueue.Initialize(_detailList.FirstOrDefault(), _remainingDetails);
            
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

        private void ResetGrid()
        {
            foreach (var kvp in grid.Registry)
            {
                kvp.Value.Detail.Clear();
            }
            RandomlyPlaceMountains();
        }
        
        private void TryPlaceDetail(HexCell cell)
        {
            #region Debug
#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.Alpha0))
            {
                cell.Detail.SetType(MergeCellDetailType.Grass);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha1))
            {
                cell.Detail.SetType(MergeCellDetailType.Bush);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha2))
            {
                cell.Detail.SetType(MergeCellDetailType.Tree);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha3))
            {
                cell.Detail.SetType(MergeCellDetailType.House);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha4))
            {
                cell.Detail.SetType(MergeCellDetailType.LumberMill);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha5))
            {
                cell.Detail.SetType(MergeCellDetailType.WindMill);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha6))
            {
                cell.Detail.SetType(MergeCellDetailType.Castle);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha7))
            {
                cell.Detail.SetType(MergeCellDetailType.Stone);
                CheckGameOver();
                return;
            }
            if (Input.GetKey(KeyCode.Alpha8))
            {
                cell.Detail.SetType(MergeCellDetailType.Empty);
                CheckGameOver();
                return;
            }
#endif
            #endregion
            
            // Can only place detail on empty tiles
            if (cell.Detail.Type != MergeCellDetailType.Empty || _remainingDetails <= 0)
            {
                return;
            }
            cell.Detail.SetType(GetNextDetailAtIndex(0));
            
            _remainingDetails--;
            _detailList.RemoveAt(0);

            // Create a snapshot of the next 3 details in the queue so they can be asynchronously processed
            var queueSnapshot = ListPool<MergeCellDetailType>.Get();
            queueSnapshot.Clear();
            queueSnapshot.Add(GetNextDetailAtIndex(0));
            queueSnapshot.Add(GetNextDetailAtIndex(1));
            queueSnapshot.Add(GetNextDetailAtIndex(2));
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
                if (!HexGameUtil.CanAnyCombineOnGrid(allCells.ToList(), config))
                {
                    TownComplete();
                }
            }
        }
        
        private async void OnDetailDequeued()
        {
            await gameUI.DetailQueue.SetNextAndDecrement(_detailList.FirstOrDefault(), _remainingDetails);
            if (_remainingDetails != 0)
            {
                return;
            }

            while (detailQueue.ProcessingDequeues)
            {
                await Task.Delay(10);
            }
            gameUI.DetailQueue.gameObject.SetActive(false);
            detailQueue.gameObject.SetActive(false);
            noTilesLeftRoot.SetActive(true);
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

            // Stones are destroyed rather than combined
            if (withDetails.All(c => c.Detail.Type == MergeCellDetailType.Stone))
            {
                foreach (var cell in withDetails)
                {
                    cell.Detail.SpawnEffect("stone_break");
                    cell.Detail.Clear();
                }
            }
            // Any other detail type is combined and awards points
            else
            {
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

            }
            grid.Save(GameMode.Merge);
        }

        private MergeCellDetailType GetDetailToSpawn()
        {
            var numDetails = 0;
            var detailTotal = 0;
            foreach (var (_, cell) in grid.Registry)
            {
                // Only count basic details towards average
                if (!cell.Detail.Type.IsBasic())
                {
                    continue;
                }

                numDetails++;
                detailTotal += (int)cell.Detail.Type;
            }

            var averageDetail = numDetails > 0 ? detailTotal / numDetails : 1;

            var totalSpawnWeights = stoneSpawnWeight
                                     + grassSpawnWeight
                                     + averageDetailSpawnWeight;
            var randomWithinRange = _random.Next(0, totalSpawnWeights);

            if (randomWithinRange < stoneSpawnWeight)
            {
                return MergeCellDetailType.Stone;
            }
            randomWithinRange -= stoneSpawnWeight;
            
            if (randomWithinRange < grassSpawnWeight)
            {
                return MergeCellDetailType.Grass;
            }

            // Will only ever give a tree as the highest detail
            return (MergeCellDetailType)Mathf.Min(averageDetail, (int)MergeCellDetailType.Tree);
        }

        private MergeCellDetailType GetNextDetailAtIndex(int index)
        {
            if (index + 1 > _remainingDetails)
            {
                // No tiles left
                return MergeCellDetailType.Empty;
            }

            if (index < _detailList.Count)
            {
                return _detailList[index];
            }

            while (index > _detailList.Count - 1 && _detailList.Count <= _remainingDetails)
            {
                _detailList.Add(GetDetailToSpawn());
                LocalSaveManager.SaveDetailQueue(_detailList);
                if (_detailList.Count - 1 == index)
                {
                    return _detailList[index];
                }
            }

            return MergeCellDetailType.Empty;
        }

        private void PreloadDetailQueue()
        {
            _detailList.Clear();
            var loadedDetails = LocalSaveManager.LoadDetailQueue();
            foreach (var detail in loadedDetails)
            {
                _detailList.Add((MergeCellDetailType)detail);
            }

            var preloadRemaining = Mathf.Min(3, startingDetailsCount) - _detailList.Count;
            for (var i = 0; i < preloadRemaining; i++)
            {
                _detailList.Add(GetDetailToSpawn());
            }
            LocalSaveManager.SaveDetailQueue(_detailList);
        }

        private void OnCellsDragContinued(List<HexCell> cells)
        {
            var cellsWithDetails = cells.Where(c => c.Detail.Type > MergeCellDetailType.Mountain);
            var withDetails = cellsWithDetails as List<HexCell> ?? cellsWithDetails.ToList();

            // Check if the cells can combine
            var (canCombine, _) = HexGameUtil.TryCombine(withDetails, config);

            // Check if the cells are all of the same time (to see if they can contribute to a combination)
            var allSameType = withDetails.All(c => c.Detail.Type == withDetails.First().Detail.Type);
            // Number to match is 3 for regular details and 4 for stones
            var numToMatch = withDetails.Count > 0
                ? withDetails.First().Detail.Type == MergeCellDetailType.Stone ? 4 : 3 
                : 3;
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