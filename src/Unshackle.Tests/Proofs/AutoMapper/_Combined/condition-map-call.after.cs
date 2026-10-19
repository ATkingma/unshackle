using System;
using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Ingredient
    {
        public string Name { get; set; } = "";
    }

    public class Line
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int Amount { get; set; }
    }

    public class LineDto
    {
        public Guid Id { get; set; }
        public int Amount { get; set; }
        public Ingredient Ingredient { get; set; } = null!;
    }

    public class LineProfile : Profile
    {
        public LineProfile()
        {
        }
    }

    public static class LineProfileExtensions
    {
        public static LineDto ToLineDto(this Line source) => new()
        {
            Id = source.Id != Guid.Empty ? source.Id : default,
            Amount = source.Amount,
            Ingredient = new() { Name = source.Name },
        };
    }
}

namespace Shop.Recipes
{
    public class RecipeService
    {
        public RecipeService()
        {
        }

        public Mapping.LineDto Get(Mapping.Line line) => line.ToLineDto();
    }
}
