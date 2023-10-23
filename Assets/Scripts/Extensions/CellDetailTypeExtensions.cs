using Hex.Grid;

namespace Hex.Extensions
{
    public static class CellDetailTypeExtensions
    {
        public static bool IsBasic(this MergeCellDetailType type)
        {
            return type > MergeCellDetailType.Mountain;
        }
        
        public static bool IsCombinableBasic(this MergeCellDetailType type)
        {
            return type is > MergeCellDetailType.Mountain and < MergeCellDetailType.Castle;
        }

        public static bool IsSpecial(this MergeCellDetailType type)
        {
            return false; //type >= CellDetailType.LumberMill;
        }

        public static bool IsCombinable(this MergeCellDetailType type)
        {
            return type > MergeCellDetailType.Mountain;
        }
    }
}