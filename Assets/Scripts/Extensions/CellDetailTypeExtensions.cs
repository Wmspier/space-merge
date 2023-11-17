using Hex.Grid;

namespace Hex.Extensions
{
    public static class CellDetailTypeExtensions
    {
        public static bool IsBasic(this MergeCellDetailType type)
        {
            return type > MergeCellDetailType.Empty;
        }
        
        public static bool IsCombinableBasic(this MergeCellDetailType type)
        {
            return type is > MergeCellDetailType.Empty;
        }

        public static bool IsSpecial(this MergeCellDetailType type)
        {
            return false; //type >= CellDetailType.LumberMill;
        }

        public static bool IsCombinable(this MergeCellDetailType type)
        {
            return type > MergeCellDetailType.Empty;
        }
    }
}