using Streetcode.BLL.DTO.Media.Images;
using Streetcode.BLL.DTO.Sources;
using Streetcode.BLL.MediatR.Media.Image.Validators;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Create;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Delete;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Update;
using Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Validators;
using Xunit;

namespace Streetcode.XUnitTest.ValidatorTests;

public class SourceValidatorsTests
{
    private readonly SourceCreateDtoValidator _sourceValidator;
    private readonly CreateSourceCommandValidator _commandValidator;
    private readonly DeleteSourceCommandValidator _deleteCommandValidator;
    private readonly SourceUpdateDtoValidator _updateSourceValidator;
    private readonly UpdateSourceCommandValidator _updateCommandValidator;

    public SourceValidatorsTests()
    {
        var imageValidator =
            new ImageFileBaseCreateDtoValidator();

        _sourceValidator =
            new SourceCreateDtoValidator(imageValidator);

        _commandValidator =
            new CreateSourceCommandValidator(_sourceValidator);

        _deleteCommandValidator = new DeleteSourceCommandValidator();

        _updateSourceValidator = new SourceUpdateDtoValidator();
        _updateCommandValidator =
            new UpdateSourceCommandValidator(_updateSourceValidator);
    }

    [Fact]
    public void Validate_WhenExistingCategoryIsValid_ShouldBeValid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: 10,
            NewCategoryTitle: null,
            NewCategoryImage: null);

        var result = _sourceValidator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNewCategoryIsValid_ShouldBeValid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: "Книги",
            NewCategoryImage: CreateValidImage());

        var result = _sourceValidator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenStreetcodeIdIsInvalid_ShouldBeInvalid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 0,
            Text: "Source text",
            SourceLinkCategoryId: 10,
            NewCategoryTitle: null,
            NewCategoryImage: null);

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(SourceCreateDTO.StreetcodeId));
    }

    [Fact]
    public void Validate_WhenTextExceedsMaximumLength_ShouldBeInvalid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: new string('a', 4001),
            SourceLinkCategoryId: 10,
            NewCategoryTitle: null,
            NewCategoryImage: null);

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(SourceCreateDTO.Text));
    }

    [Fact]
    public void Validate_WhenExistingCategoryIdIsInvalid_ShouldBeInvalid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: 0,
            NewCategoryTitle: null,
            NewCategoryImage: null);

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(SourceCreateDTO.SourceLinkCategoryId));
    }

    [Fact]
    public void Validate_WhenExistingCategoryAndNewCategoryDataAreProvided_ShouldBeInvalid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: 10,
            NewCategoryTitle: "Книги",
            NewCategoryImage: CreateValidImage());

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(SourceCreateDTO.NewCategoryTitle));
        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(SourceCreateDTO.NewCategoryImage));
    }

    [Fact]
    public void Validate_WhenNewCategoryTitleIsMissing_ShouldBeInvalid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: null,
            NewCategoryImage: CreateValidImage());

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(SourceCreateDTO.NewCategoryTitle));
    }

    [Fact]
    public void Validate_WhenNewCategoryTitleExceedsMaximumLength_ShouldBeInvalid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: new string('a', 24),
            NewCategoryImage: CreateValidImage());

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(SourceCreateDTO.NewCategoryTitle));
    }

    [Fact]
    public void Validate_WhenNewCategoryImageIsMissing_ShouldBeInvalid()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: "Книги",
            NewCategoryImage: null);

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(SourceCreateDTO.NewCategoryImage));
    }

    [Fact]
    public void Validate_WhenNewCategoryImageBase64IsInvalid_ShouldBeInvalid()
    {
        var image = CreateValidImage();
        image.BaseFormat = "not-base64";

        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: "Книги",
            NewCategoryImage: image);

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName.EndsWith(
                    nameof(ImageFileBaseCreateDTO.BaseFormat),
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenNewCategoryImageExceedsThreeMegabytes_ShouldBeInvalid()
    {
        var image = CreateValidImage();
        image.BaseFormat = Convert.ToBase64String(
            new byte[(3 * 1024 * 1024) + 1]);

        var dto = new SourceCreateDTO(
            StreetcodeId: 1,
            Text: "Source text",
            SourceLinkCategoryId: null,
            NewCategoryTitle: "Книги",
            NewCategoryImage: image);

        var result = _sourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error =>
                error.ErrorMessage ==
                "New category image must not exceed 3 MB.");
    }

    [Fact]
    public void ValidateCommand_WhenSourceIsNull_ShouldBeInvalid()
    {
        var command = new CreateSourceCommand(null!);

        var result = _commandValidator.Validate(command);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(CreateSourceCommand.SourceCreateDto));
    }

    [Fact]
    public void ValidateCommand_WhenSourceIsInvalid_ShouldContainNestedValidationError()
    {
        var dto = new SourceCreateDTO(
            StreetcodeId: 0,
            Text: "Source text",
            SourceLinkCategoryId: 10,
            NewCategoryTitle: null,
            NewCategoryImage: null);
        var command = new CreateSourceCommand(dto);

        var result = _commandValidator.Validate(command);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                $"{nameof(CreateSourceCommand.SourceCreateDto)}.{nameof(SourceCreateDTO.StreetcodeId)}");
    }

    [Fact]
    public void ValidateDeleteCommand_WhenIdsAreValid_ShouldBeValid()
    {
        var command = new DeleteSourceCommand(
            StreetcodeId: 1,
            SourceLinkCategoryId: 2);

        var result = _deleteCommandValidator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDeleteCommand_WhenIdsAreInvalid_ShouldContainBothErrors()
    {
        var command = new DeleteSourceCommand(
            StreetcodeId: 0,
            SourceLinkCategoryId: -1);

        var result = _deleteCommandValidator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(DeleteSourceCommand.StreetcodeId));
        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(DeleteSourceCommand.SourceLinkCategoryId));
    }

    [Fact]
    public void ValidateUpdate_WhenDataIsValid_ShouldBeValid()
    {
        var dto = new SourceUpdateDTO(
            StreetcodeId: 1,
            SourceLinkCategoryId: 2,
            Text: "Updated source text");

        var result = _updateSourceValidator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateUpdate_WhenDataIsInvalid_ShouldContainAllErrors()
    {
        var dto = new SourceUpdateDTO(
            StreetcodeId: 0,
            SourceLinkCategoryId: -1,
            Text: new string('a', 4001));

        var result = _updateSourceValidator.Validate(dto);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(SourceUpdateDTO.StreetcodeId));
        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(SourceUpdateDTO.SourceLinkCategoryId));
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(SourceUpdateDTO.Text));
    }

    [Fact]
    public void ValidateUpdateCommand_WhenSourceIsNull_ShouldBeInvalid()
    {
        var command = new UpdateSourceCommand(null!);

        var result = _updateCommandValidator.Validate(command);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(UpdateSourceCommand.SourceUpdateDto));
    }

    [Fact]
    public void ValidateUpdateCommand_WhenSourceIsInvalid_ShouldContainNestedError()
    {
        var dto = new SourceUpdateDTO(
            StreetcodeId: 0,
            SourceLinkCategoryId: 2,
            Text: "Updated source text");
        var command = new UpdateSourceCommand(dto);

        var result = _updateCommandValidator.Validate(command);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                $"{nameof(UpdateSourceCommand.SourceUpdateDto)}.{nameof(SourceUpdateDTO.StreetcodeId)}");
    }

    private static ImageFileBaseCreateDTO CreateValidImage()
    {
        return new ImageFileBaseCreateDTO
        {
            BaseFormat = "AQID",
            MimeType = "image/png",
            Extension = "png",
        };
    }
}
