using AutoMapper;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Mapping.Streetcode.TextContent;
using Streetcode.DAL.Entities.Streetcode.TextContent;
using Xunit;
using Streetcode.BLL.Constants;
using Streetcode.BLL.MediatR.Streetcode.Text.Create;

namespace Streetcode.XUnitTest.Mappings.Streetcode.TextContent;

public class TextProfileTests
{
    private readonly IMapper _mapper;

    public TextProfileTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TextProfile>();
        });

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void Map_TextCreateDTO_To_Text_MapsAdditionalText()
    {
        var dto = new TextCreateDTO
        {
            Title = "Test title",
            TextContent = "Test content",
            AdditionalText = TextConstants.DefaultAdditionalText
        };

        var result = _mapper.Map<Text>(dto);

        Assert.Equal(dto.AdditionalText, result.AdditionalText);
    }

   
}