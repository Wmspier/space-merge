namespace Hex.Grid
{
    public enum MergeCellDetailType
    {
        // Un-combinable
         Empty = -2,
         Mountain = -1,
         
        // Combinable at larger amount
        Stone,
        
        // Basic
        Grass,
        Bush,
        Tree,
        House,
        
        // Special
        LumberMill,
        WindMill,
        Castle
    }
}