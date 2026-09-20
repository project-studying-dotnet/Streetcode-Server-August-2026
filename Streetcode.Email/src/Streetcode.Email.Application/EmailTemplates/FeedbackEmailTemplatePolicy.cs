namespace Streetcode.Email.Application.EmailTemplates;

public static class FeedbackEmailTemplatePolicy
{
    public const string TemplateName = "feedback.v1";

    public const string SenderEmailKey = "From";
    public const string ContentKey = "Content";

    public const int RequiredTemplateDataCount = 2;
    public const int SenderEmailMaxLength = 80;
    public const int ContentMaxLength = 500;

    public static bool IsAllowedKey(string key)
    {
        return key is SenderEmailKey or ContentKey;
    }
}
