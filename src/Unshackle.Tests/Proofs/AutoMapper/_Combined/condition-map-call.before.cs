using System;
using AutoMapper;

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
            CreateMap<Line, LineDto>()
                .ForMember(d => d.Id, o => o.Condition(s => s.Id != Guid.Empty))
                .ForPath(d => d.Ingredient.Name, o => o.MapFrom(s => s.Name));
        }
    }
}

namespace Shop.Recipes
{
    public class RecipeService
    {
        private readonly IMapper _mapper;

        public RecipeService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public Mapping.LineDto Get(Mapping.Line line) => _mapper.Map<Mapping.LineDto>(line);
    }
}
