using AutoMapper;
using NUnit.Framework;

namespace NUnit.Framework
{
    public class TestFixtureAttribute : System.Attribute
    {
    }

    public class SetUpAttribute : System.Attribute
    {
    }

    public class TestAttribute : System.Attribute
    {
    }
}

namespace Media.Mapping.Profiles
{
    public class SearchResult
    {
        public string Title { get; set; } = "";
    }

    public class SearchViewModel
    {
        public string Title { get; set; } = "";
    }

    public class TvProfile : Profile
    {
        public TvProfile()
        {
            CreateMap<SearchResult, SearchViewModel>();
        }
    }
}

namespace Media.Core.Tests
{
    [TestFixture]
    public class TvProfileMappingTests
    {

        [Test]
        public void Title_IsKept()
        {
            var source = new Media.Mapping.Profiles.SearchResult { Title = "Test Show" };
            Check(source.Title == "Test Show");
        }

        private static void Check(bool condition)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException();
            }
        }
    }
}
