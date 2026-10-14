using System;
using AutoMapper;

namespace Shop;

public class Ingredient
{
    public string Name { get; set; } = "";
}

public class RecipeIngredient
{
    public int Amount { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
}

public class IngredientLine
{
    public string Name { get; set; } = "";
    public int Amount { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static RecipeIngredient ToRecipeIngredient(this IngredientLine source) => new()
    {
        Amount = source.Amount,
        Ingredient = new() { Name = source.Name },
    };
}
