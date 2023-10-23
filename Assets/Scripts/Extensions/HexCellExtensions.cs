using Hex.Grid;

namespace Hex.Extensions
{
    public static class HexCellExtensions
    {
        public static bool IsNeighborOf(this HexCell left, HexCell right) => left.Neighbors.Contains(right);

        public static HexCellDirection GetDirectionOfNeighbor(this HexCell left, HexCell right)
        {
            var leftCoord = left.Coordinates;
            var rightCoord = right.Coordinates;

            if (leftCoord.x == rightCoord.x && 
                leftCoord.y > rightCoord.y && 
                leftCoord.z < rightCoord.z)
            {
                return HexCellDirection.Top;
            }

            if (leftCoord.x < rightCoord.x &&
                leftCoord.y > rightCoord.y &&
                leftCoord.z == rightCoord.z)
            {
                return HexCellDirection.TopRight;
            }

            if (leftCoord.x < rightCoord.x &&
                leftCoord.y == rightCoord.y &&
                leftCoord.z > rightCoord.z)
            {
                return HexCellDirection.BottomRight;
            }

            if (leftCoord.x == rightCoord.x &&
                leftCoord.y < rightCoord.y &&
                leftCoord.z > rightCoord.z)
            {
                return HexCellDirection.Bottom;
            }

            if (leftCoord.x > rightCoord.x &&
                leftCoord.y < rightCoord.y &&
                leftCoord.z == rightCoord.z)
            {
                return HexCellDirection.BottomLeft;
            }

            if (leftCoord.x > rightCoord.x &&
                leftCoord.y == rightCoord.y &&
                leftCoord.z < rightCoord.z)
            {
                return HexCellDirection.TopLeft;
            }

            return HexCellDirection.None;
        }
    }
}