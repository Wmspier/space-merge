using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hex.Grid.Cell
{
    public struct HexCellDefinition
    {
        private const char StringDelimiter = ',';
        
        public int3 Coordinates;
        public int Detail;

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Coordinates.x);
            stringBuilder.Append(StringDelimiter);
            stringBuilder.Append(Coordinates.y);
            stringBuilder.Append(StringDelimiter);
            stringBuilder.Append(Coordinates.z);
            stringBuilder.Append(StringDelimiter);
            stringBuilder.Append(Detail);
            return stringBuilder.ToString();
        }

        public static HexCellDefinition FromString(string input)
        {
            var contents = input.Split(StringDelimiter);
            Assert.IsTrue(contents.Length == 4, "Cell Definition Input is invalid.");

            return new HexCellDefinition()
            {
                Coordinates = new int3(
                    int.Parse(contents[0]), 
                    int.Parse(contents[1]), 
                    int.Parse(contents[2])),
                Detail = int.Parse(contents[3])
            };
        }
    }
}