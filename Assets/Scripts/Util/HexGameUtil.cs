using System.Collections.Generic;
using System.Linq;
using Hex.Grid.Cell;

namespace Hex.Util
{
    public static class HexGameUtil
    {
	    public const int MaxRarityZeroBased = 2;
	    
	    public static (bool createsUpgrade, int finalPower, int finalRarity) TryCombineUnits(IEnumerable<HexCell> toCombine)
	    {
		    var hexCells = toCombine as HexCell[] ?? toCombine.ToArray();
		    // Can't combine if list has one or fewer cells or there are any empty spaces
		    if (hexCells.Length <= 1 || hexCells.Any(c => c.InfoHolder.HeldPlayerUnit == null))
		    {
			    return (false, -1, -1);
		    }

		    var createsUpgrade = false;
		    var masterCellId = hexCells.First().InfoHolder.HeldPlayerUnit.UniqueId;
		    var masterCellRarity = hexCells.First().InfoHolder.PlayerRarity;
		    var finalPower = 0;
		    var finalRarity = masterCellRarity;
		    var currentRarity = masterCellRarity;

		    for (var index = 0; index < hexCells.Length; index++)
		    {
			    var cell = hexCells[index];
			    if (index == 0)
			    {
				    finalPower += cell.InfoHolder.PlayerPower;
				    continue;
			    }

			    // Can't combine lower rarities with higher rarities
			    if (cell.InfoHolder.PlayerRarity > currentRarity) return (false, -1, -1); 

			    // Add the cells power to the resulting power
			    finalPower += cell.InfoHolder.PlayerPower;
			    
			    // If the cell shares a rarity with the rarity at this point in the merge,
			    // and the cell shares the same type as the master,
			    // and the master cell is not at max rarity,
			    // this cell creates an upgrade and doubles the final power
			    if (cell.InfoHolder.PlayerRarity == currentRarity &&
			        cell.InfoHolder.HeldPlayerUnit.UniqueId == masterCellId &&
			        currentRarity < MaxRarityZeroBased)
			    {
				    createsUpgrade = true;
				    currentRarity++;
				    finalRarity = currentRarity;
				    finalPower *= 2;
			    }
		    }

		    return (createsUpgrade, finalPower, finalRarity);
	    }
	    
	    public static bool IsValidMerge(IEnumerable<HexCell> cells)
	    {
		    var (_, finalPower, _) = TryCombineUnits(cells);
		    return finalPower > 0;
	    }

	    public static bool IsValidMove(List<HexCell> cells)
	    {
		    return cells.Count > 1 &&
		           !cells.Last().HoldingUnit && 
		           cells.GetRange(0, cells.Count-1)
			           .All(c => c.HoldingUnit);
	    }
	    
       // // This is a very expensive check
       // public static bool CanAnyCombineOnGrid(List<HexCell> cellsInGrid, HexDetailConfiguration config)
       // {
       //     var stopWatch = new Stopwatch();
       //     stopWatch.Start();
       //     
       //     const int maxRecursion = 10;
       //     var count = 0;
       //     var cellsToCheck = ListPool<HexCell>.Get();
       //     
       //     // Iterate over all cells that are not stone or empty
       //     foreach (var cell in cellsInGrid)
       //     {
       //         if (!cell.Detail.Type.IsCombinable())
       //         {
       //             continue;
       //         }
       //         cellsToCheck.Add(cell);
       //         // Recursive check found successful result
       //         if (CheckNeighborsRecursive(ref count, cell, cellsToCheck))
       //         {
       //             Debug.Log($"Can combine: {GetDebugList(cellsToCheck)}");
       //             ListPool<HexCell>.Release(cellsToCheck);
       //             stopWatch.Stop();
       //             Debug.Log($"Checked in {stopWatch.Elapsed.TotalSeconds} seconds");
       //             return true;
       //         }

       //         // Found no combination with this cell included
       //         count = 0;
       //         cellsToCheck.Clear();
       //     }

       //     ListPool<HexCell>.Release(cellsToCheck);
       //     
       //     stopWatch.Stop();
       //     Debug.Log($"Checked in {stopWatch.Elapsed.TotalSeconds} seconds");
       //     return false;

       //     bool CheckNeighborsRecursive(ref int recursionCount, HexCell cell, in List<HexCell> cellList)
       //     {
       //         // Increase recursion level
       //         recursionCount++;
       //         
       //         // Early break if we've exceeded max recursion
       //         if (recursionCount > maxRecursion)
       //         {
       //             Debug.Log("Hit recursion limit");
       //             return false;
       //         }
       //         
       //         // Check all neighbors
       //         var neighborsCanCombine = false;
       //         foreach (var neighbor in cell.Neighbors)
       //         {
       //             // Avoid adding duplicate cells into list
       //             if (cellList.Contains(neighbor))
       //             {
       //                 continue;
       //             }
       //             
       //             cellList.Add(neighbor);
       //             if (cellList.Count > 2 && CanCombine(cellsToCheck, config))
       //             {
       //                 // Cells can combine
       //                 return true;
       //             }

       //             // Avoid checking recursively if list contains at least 3 basic and can't combine
       //             if (cellList.All(c => c.Detail.Type.IsBasic() || c.Detail.Type == MergeCellDetailType.Empty) &&
       //                 cellList.Count(c => c.Detail.Type.IsBasic()) > 2)
       //             {
       //                 cellList.Remove(neighbor);
       //                 continue;
       //             }
       //             
       //             // Avoid checking recursively if list contains 3 or more special
       //             if (cellList.Count(c => c.Detail.Type.IsSpecial()) > 2)
       //             {
       //                 cellList.Remove(neighbor);
       //                 continue;
       //             }
       //             
       //             // If current cells cannot combine progress to next level of recursion
       //             neighborsCanCombine |= CheckNeighborsRecursive(ref recursionCount, neighbor, cellList);

       //             // Early return from recursion to avoid checking more neighbors
       //             if (neighborsCanCombine)
       //             {
       //                 return true;
       //             }
       //         }
       //         
       //         // Recursive check finished. Remove this cell and step up one level
       //         cellList.Remove(cell);
       //         recursionCount--;
       //         
       //         return false;
       //     }

       //     string GetDebugList(IEnumerable<HexCell> cells)
       //     {
       //         return cells.Aggregate("", (current, cell) => current + $"{cell.Detail.Type},");
       //     }
       // }
    }
}