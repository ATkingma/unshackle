using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Category
{
    public string Name { get; set; } = "";
    public List<Category> Children { get; set; } = [];
}

public class CategoryDto
{
    public string Name { get; set; } = "";
    public List<CategoryDto> Children { get; set; } = [];
}

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();
    }
}
