using AutoMapper;
using Streetcode.BLL.DTO.Streetcode.RelatedFigure;
using Streetcode.BLL.Mapping.AdditionalContent;
using Streetcode.BLL.Mapping.Streetcode;
using Streetcode.DAL.Entities.AdditionalContent;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Entities.Streetcode.Types;
using Xunit;

namespace Streetcode.XUnitTest.Mapping;

public class RelatedFigureProfileTests
{
    [Fact]
    public void Map_ShouldMapPersonAndEventStreetcodesTypedAsStreetcodeContentToRelatedFigureDtos()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile<RelatedFigureProfile>();
            config.AddProfile<TagProfile>();
        });
        var mapper = configuration.CreateMapper();
        IEnumerable<StreetcodeContent> streetcodes = new List<StreetcodeContent>
        {
            new PersonStreetcode
            {
                Id = 1,
                Title = "Person title",
                TransliterationUrl = "person-title",
                Alias = "Alias",
                Tags = new List<Tag> { new() { Id = 5, Title = "poet" } },
            },
            new EventStreetcode
            {
                Id = 2,
                Title = "Event title",
                TransliterationUrl = "event-title",
            },
        };

        var result = mapper.Map<IEnumerable<RelatedFigureDTO>>(streetcodes).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Person title", result[0].Title);
        Assert.Equal("person-title", result[0].Url);
        Assert.Equal("Alias", result[0].Alias);
        var tag = Assert.Single(result[0].Tags);
        Assert.Equal("poet", tag.Title);
        Assert.Equal(2, result[1].Id);
        Assert.Equal("event-title", result[1].Url);
        Assert.Empty(result[1].Tags);
    }
}
