using System.Collections.Generic;

namespace WOLF
{
    public interface IRecipe
    {
        Dictionary<string, int> InputIngredients { get; }
        Dictionary<string, int> OutputIngredients { get; }
    }
}
