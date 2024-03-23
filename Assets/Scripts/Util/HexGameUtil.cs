using System.Collections.Generic;
using System.Linq;
using Hex.Data;
using Hex.Grid.Cell;

namespace Hex.Util
{
    public static class HexGameUtil
    {
	    public const int MaxRarityZeroBased = 2;
	    
	    public static (UnitData masterUnit, bool createsUpgrade, int finalPower, int finalShield, int finalRarity) TryCombineUnits(IEnumerable<HexCell> toCombine, int maxMergeCount)
	    {
		    var hexCells = toCombine as HexCell[] ?? toCombine.ToArray();
		    // Can't combine if list has one or fewer cells or there are any empty spaces
		    if (hexCells.Length <= 1 || hexCells.Any(c => c.InfoHolder.HeldPlayerUnit == null))
		    {
			    return (null, false, -1, -1, -1);
		    }
		    
		    // Can't combine if beyond max merge count
		    if (hexCells.Length > maxMergeCount)
		    {
			    return (null, false, -1, -1, -1);
		    }
		    
		    var createsUpgrade = false;

			// Group cells into potential upgrade groups
		    var upgradeGroups = new List<List<HexCell>>();
		    var upgradeGroupIndex = 0;
		    
		    for (var index = 0; index < hexCells.Length; index++)
		    {
			    var cell = hexCells[index];
			    // first cell, add to first potential upgrade group
			    if (upgradeGroupIndex == 0 && index == 0)
			    {
				    upgradeGroups.Add(new List<HexCell>{cell});
				    continue;
			    }

			    var currentUpgradeGroup = upgradeGroups[upgradeGroupIndex];

			    // Cell is the same as the last, they are in the same upgrade group
			    if (cell.InfoHolder.HoldingSameUnitType(currentUpgradeGroup.Last()))
			    {
				    currentUpgradeGroup.Add(cell);
			    }
			    // Cell is different, place in a new potential upgrade group
			    else
			    {
				    upgradeGroupIndex++;
				    upgradeGroups.Add(new List<HexCell>{cell});
			    }
		    }

		    // Iterate through upgrade groups and collapse them into single units
		    var collapsedHexCells = new (HexCell hexCells, UnitData data, int power, int shield)[upgradeGroups.Count];
		    for (var index = 0; index < upgradeGroups.Count; index++)
		    {
			    var group = upgradeGroups[index];
			    // Upgrade group contains one unit, so nothing to combine
			    if (group.Count == 1)
			    {
				    var cell = group.First();
				    collapsedHexCells[index] = (cell, cell.InfoHolder.HeldPlayerUnit, cell.InfoHolder.PlayerPower,
					    cell.InfoHolder.PlayerShield);
			    }
			    else
			    {
				    var combinedPower = 0;
				    var combinedShield = 0;
				    foreach (var cell in group)
				    {
					    combinedPower += cell.InfoHolder.PlayerPower;
					    combinedShield += cell.InfoHolder.PlayerShield;
				    }

				    var masterCell = group.First();
				    combinedPower *= 2;
				    combinedShield *= 2;
				    collapsedHexCells[index] = (masterCell, masterCell.InfoHolder.HeldPlayerUnit, combinedPower, combinedShield);
				    createsUpgrade = true;
			    }
		    }
		    
		    UnitData masterUnit = null;
		    var finalPower = 0;
		    var finalShield = 0;

			// Iterate through all collapsed cells to find the master unit and the final power/shield
		    for (var index = 0; index < collapsedHexCells.Length; index++)
		    {
			    var (cell, _, power, shield) = collapsedHexCells[index];
			    
			    // Master unit not set or was support unit
			    if (masterUnit == null || masterUnit.IsSupport && !cell.InfoHolder.IsSupportUnit)
			    {
				    masterUnit = cell.InfoHolder.HeldPlayerUnit;
			    }
			    if (index == 0)
			    {
				    finalPower += power;
				    finalShield += shield;
				    continue;
			    }

			    // Add the cells power/shield to the resulting power/shield
			    finalPower += power;
			    finalShield += shield;
		    }

		    return (masterUnit, createsUpgrade, finalPower, finalShield, -1);
	    }
	    
	    public static bool IsValidMerge(IEnumerable<HexCell> cells, int maxMergeCount)
	    {
		    var (_, _, finalPower, _, _) = TryCombineUnits(cells, maxMergeCount);
		    return finalPower >= 0;
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