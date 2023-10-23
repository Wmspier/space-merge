using System.Collections.Generic;

namespace Hex.Grid.Serialization
{
    public interface IGridSerializer
    {
        string Serialize(List<HexCellDefinition> cellDefinitions);
        List<HexCellDefinition> Deserialize(string input);
    }
}