using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WOLF
{
    public class HopperMetadata : IPersistenceAware
    {
        private static readonly string HOPPER_NODE_NAME = "HOPPER";

        public string Id { get; set; }
        public IDepot Depot { get; set; }
        public IRecipe Recipe { get; set; }

        public HopperMetadata(IDepot depot)
        {
            Id = Guid.NewGuid().ToString();
            Depot = depot;
        }

        public HopperMetadata(IDepot depot, IRecipe recipe)
            : this(depot)
        {
            Recipe = recipe;
        }

        public void OnLoad(ConfigNode node)
        {
            Id = node.GetValue("Id");
            var recipeValue = node.GetValue("Recipe");

            var inputIngredients = ParseRecipeIngredientList(recipeValue);
            Recipe = new Recipe(inputIngredients, new Dictionary<string, int>());
        }

        public void OnSave(ConfigNode node)
        {
            var hopperNode = node.AddNode(HOPPER_NODE_NAME);
            hopperNode.AddValue("Id", Id);
            hopperNode.AddValue("Body", Depot.Body);
            hopperNode.AddValue("Biome", Depot.Biome);

            var ingredients = Recipe.InputIngredients
                .Select(i => string.Format("{0},{1}", i.Key, i.Value));
            var ingredientsList = string.Join(",", ingredients.ToArray());
            Debug.Log("[WOLF] HopperMetadata.OnSave: Saving recipe => " + ingredientsList);
            hopperNode.AddValue("Recipe", ingredientsList);
        }

        private Dictionary<string, int> ParseRecipeIngredientList(string ingredients)
        {
            var ingredientList = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(ingredients))
            {
                var tokens = ingredients.Split(',');
                if (tokens.Length % 2 != 0)
                {
                    return null;
                }
                for (int i = 0; i < tokens.Length - 1; i = i + 2)
                {
                    var resource = tokens[i];
                    var quantityString = tokens[i + 1];

                    if (!int.TryParse(quantityString, out int quantity))
                    {
                        return null;
                    }

                    ingredientList.Add(resource, quantity);
                }
            }

            return ingredientList;
        }
    }
}
