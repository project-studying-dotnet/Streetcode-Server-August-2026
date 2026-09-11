using AutoMapper;
using Streetcode.BLL.DTO.Media.Video;
using Streetcode.BLL.Mapping.Media;
using Streetcode.DAL.Entities.Media;
using Xunit;

namespace Streetcode.XUnitTest.Mappings.Media;

public class VideoProfileTests
{
    private readonly IMapper _mapper;

    public VideoProfileTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<VideoProfile>();
        });

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void Video_To_VideoDTO_MapsCorrectly()
    {
        var video = new Video
        {
            Id = 1,
            Description = "Test video",
            Url = "https://www.youtube.com/watch?v=test",
            StreetcodeId = 10
        };

        var result = _mapper.Map<VideoDTO>(video);

        Assert.Equal(video.Id, result.Id);
        Assert.Equal(video.Description, result.Description);
        Assert.Equal(video.Url, result.Url);
        Assert.Equal(video.StreetcodeId, result.StreetcodeId);
    }

    [Fact]
    public void VideoDTO_To_Video_MapsCorrectly()
    {
        var videoDto = new VideoDTO
        {
            Id = 1,
            Description = "Test video",
            Url = "https://www.youtube.com/watch?v=test",
            StreetcodeId = 10
        };

        var result = _mapper.Map<Video>(videoDto);

        Assert.Equal(videoDto.Id, result.Id);
        Assert.Equal(videoDto.Description, result.Description);
        Assert.Equal(videoDto.Url, result.Url);
        Assert.Equal(videoDto.StreetcodeId, result.StreetcodeId);
    }
}