using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Hex.Configuration;
using Hex.Extensions;
using Hex.Grid;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;

namespace Hex.Util
{
    public static class HexGameUtil
    {
        private static (bool canCombine, MergeCellDetailType? newType) TryCombineBasic(IEnumerable<HexCell> toCombine)
        {
            var cellsWithDetails = toCombine.Where(c => c.Detail.Type > MergeCellDetailType.Empty);
            var withDetails = cellsWithDetails as List<HexCell> ?? cellsWithDetails.ToList();
            if (withDetails.Count < 3 || withDetails.Any(c => !c.Detail.Type.IsCombinableBasic()))
            {
                return (false, null);
            }

            const int requiredToCombine = 3;
            if (withDetails.Count < requiredToCombine)
            {
                return (false, null);
            }

            if (withDetails.Any(c => c.Detail.Type != withDetails.First().Detail.Type))
            {
                return (false, null);
            }

            return (true, withDetails.First().Detail.Type + 1);
        }

        private static (bool canCombine, MergeCellDetailType? newType) TryCombineSpecial(IReadOnlyCollection<HexCell> toCombine, HexDetailConfiguration config)
        {
            var typeListLeft = ListPool<MergeCellDetailType>.Get();
            var typeListRight = ListPool<MergeCellDetailType>.Get();
            foreach (var specialDetail in config.SpecialDetails)
            {
                typeListLeft.AddRange(toCombine.Select(c => c.Detail.Type));
                typeListRight.AddRange(specialDetail.Components);
                foreach (var c in toCombine)
                {
                    var detail = c.Detail.Type;
                    if (typeListRight.Contains(detail))
                    {
                        typeListLeft.Remove(detail);
                        typeListRight.Remove(detail);
                    }
                }

                if (typeListLeft.Count == 0 && typeListRight.Count == 0)
                {
                    return (true, specialDetail.Type);
                }
                typeListLeft.Clear();
                typeListRight.Clear();
            }

            ListPool<MergeCellDetailType>.Release(typeListLeft);
            ListPool<MergeCellDetailType>.Release(typeListRight);
            return (false, null);
        }

        public static (bool canCombine, MergeCellDetailType? newType) TryCombine(IReadOnlyCollection<HexCell> toCombine, HexDetailConfiguration config)
        {
            var (canCombine, newType) = TryCombineBasic(toCombine);
            // if (!canCombine)
            // {
            //     (canCombine, newType) = TryCombineSpecial(toCombine, config);
            // }

            return (canCombine, newType);
        }
        
        private static bool CanCombine(IReadOnlyCollection<HexCell> toCombine, HexDetailConfiguration config)
        {
            return TryCombineBasic(toCombine).canCombine || TryCombineSpecial(toCombine, config).canCombine;
        }
        
        // This is a very expensive check
        public static bool CanAnyCombineOnGrid(List<HexCell> cellsInGrid, HexDetailConfiguration config)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            const int maxRecursion = 10;
            var count = 0;
            var cellsToCheck = ListPool<HexCell>.Get();
            
            // Iterate over all cells that are not stone or empty
            foreach (var cell in cellsInGrid)
            {
                if (!cell.Detail.Type.IsCombinable())
                {
                    continue;
                }
                cellsToCheck.Add(cell);
                // Recursive check found successful result
                if (CheckNeighborsRecursive(ref count, cell, cellsToCheck))
                {
                    Debug.Log($"Can combine: {GetDebugList(cellsToCheck)}");
                    ListPool<HexCell>.Release(cellsToCheck);
                    stopWatch.Stop();
                    Debug.Log($"Checked in {stopWatch.Elapsed.TotalSeconds} seconds");
                    return true;
                }

                // Found no combination with this cell included
                count = 0;
                cellsToCheck.Clear();
            }

            ListPool<HexCell>.Release(cellsToCheck);
            
            stopWatch.Stop();
            Debug.Log($"Checked in {stopWatch.Elapsed.TotalSeconds} seconds");
            return false;

            bool CheckNeighborsRecursive(ref int recursionCount, HexCell cell, in List<HexCell> cellList)
            {
                // Increase recursion level
                recursionCount++;
                
                // Early break if we've exceeded max recursion
                if (recursionCount > maxRecursion)
                {
                    Debug.Log("Hit recursion limit");
                    return false;
                }
                
                // Check all neighbors
                var neighborsCanCombine = false;
                foreach (var neighbor in cell.Neighbors)
                {
                    // Avoid adding duplicate cells into list
                    if (cellList.Contains(neighbor))
                    {
                        continue;
                    }
                    
                    cellList.Add(neighbor);
                    if (cellList.Count > 2 && CanCombine(cellsToCheck, config))
                    {
                        // Cells can combine
                        return true;
                    }

                    // Avoid checking recursively if list contains at least 3 basic and can't combine
                    if (cellList.All(c => c.Detail.Type.IsBasic() || c.Detail.Type == MergeCellDetailType.Empty) &&
                        cellList.Count(c => c.Detail.Type.IsBasic()) > 2)
                    {
                        cellList.Remove(neighbor);
                        continue;
                    }
                    
                    // Avoid checking recursively if list contains 3 or more special
                    if (cellList.Count(c => c.Detail.Type.IsSpecial()) > 2)
                    {
                        cellList.Remove(neighbor);
                        continue;
                    }
                    
                    // If current cells cannot combine progress to next level of recursion
                    neighborsCanCombine |= CheckNeighborsRecursive(ref recursionCount, neighbor, cellList);

                    // Early return from recursion to avoid checking more neighbors
                    if (neighborsCanCombine)
                    {
                        return true;
                    }
                }
                
                // Recursive check finished. Remove this cell and step up one level
                cellList.Remove(cell);
                recursionCount--;
                
                return false;
            }

            string GetDebugList(IEnumerable<HexCell> cells)
            {
                return cells.Aggregate("", (current, cell) => current + $"{cell.Detail.Type},");
            }
        }
    }
}