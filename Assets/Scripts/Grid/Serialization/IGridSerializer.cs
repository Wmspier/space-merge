using System.Collections.Generic;
using Hex.Grid.Cell;

namespace Hex.Grid.Serialization
{
    public interface IGridSerializer
    {
        string Serialize(List<HexCellDefinition> cellDefinitions);
        List<HexCellDefinition> Deserialize(string input);
    }
}