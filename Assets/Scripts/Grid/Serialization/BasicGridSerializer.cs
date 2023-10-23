using System.Collections.Generic;
using System.Text;

namespace Hex.Grid.Serialization
{
    public class BasicGridSerializer : IGridSerializer
    {
        private const char CellDelimiter = '|';

        public string Serialize(List<HexCellDefinition> cellDefinitions)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < cellDefinitions.Count; i++)
            {
                var cell = cellDefinitions[i];
                stringBuilder.Append(cell);
                if (i < cellDefinitions.Count - 1)
                {
                    stringBuilder.Append(CellDelimiter);
                }
            }

            return stringBuilder.ToString();
        }

        public List<HexCellDefinition> Deserialize(string input)
        {
            var definitionStrings = input.Split(CellDelimiter);
            var cellDefinitions = new List<HexCellDefinition>();
            foreach (var defString in definitionStrings)
            {
                cellDefinitions.Add(HexCellDefinition.FromString(defString));
            }

            return cellDefinitions;
        }
    }
}