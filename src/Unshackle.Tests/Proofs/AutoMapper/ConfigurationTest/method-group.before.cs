using AutoMapper;

namespace Recipes.Tests
{
    public class Recipe
    {
        public string Name { get; set; } = "";
    }

    public class RecipeContract
    {
        public string Name { get; set; } = "";
    }

    public abstract class ToContractConverterTestBase
    {
        public RecipeContract Expected(Recipe source)
        {
            var mapper = new MapperConfiguration(AddMapping).CreateMapper();
            return mapper.Map<RecipeContract>(source);
        }

        protected abstract void AddMapping(IMapperConfigurationExpression cfg);
    }
}
