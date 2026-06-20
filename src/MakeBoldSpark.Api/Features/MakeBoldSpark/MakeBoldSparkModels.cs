using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MakeBoldSpark.Core.Data;

namespace MakeBoldSpark.Api.Features.MakeBoldSpark;

public sealed record AuthorResponse(
    [property: Description("Author identifier.")]
    int Id,
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(160)]
    [property: Description("Author email address.")]
    string Email,
    [property: Required]
    [property: StringLength(160)]
    [property: Description("Author display name.")]
    string DisplayName,
    [property: StringLength(2000)]
    [property: Description("Optional public author biography.")]
    string? Bio,
    [property: StringLength(400)]
    [property: Description("Optional avatar URL or image reference.")]
    string? Avatar,
    [property: Description("Indicates whether the author has administrator privileges.")]
    bool IsAdmin)
{
    public static AuthorResponse FromAuthor(Author author) =>
        new(
            author.Id,
            author.Email,
            author.DisplayName,
            author.Bio,
            author.Avatar,
            author.IsAdmin);
}

public sealed record MailSettingResponse(
    [property: Description("Mail setting identifier.")]
    int Id,
    [property: Required]
    [property: StringLength(160)]
    [property: Description("SMTP server host name.")]
    string Host,
    [property: Range(1, 65535)]
    [property: Description("SMTP server port.")]
    int Port,
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(120)]
    [property: Description("SMTP authentication email address.")]
    string UserEmail,
    [property: Required]
    [property: StringLength(120)]
    [property: Description("Sender display name.")]
    string FromName,
    [property: Required]
    [property: EmailAddress]
    [property: StringLength(120)]
    [property: Description("Sender email address.")]
    string FromEmail,
    [property: Required]
    [property: StringLength(120)]
    [property: Description("Default recipient display name.")]
    string ToName,
    [property: Description("Whether this mail setting is enabled.")]
    bool Enabled,
    [property: Description("Associated CMS blog identifier.")]
    int BlogId)
{
    public static MailSettingResponse FromMailSetting(MailSetting setting) =>
        new(
            setting.Id,
            setting.Host,
            setting.Port,
            setting.UserEmail,
            setting.FromName,
            setting.FromEmail,
            setting.ToName,
            setting.Enabled,
            setting.BlogId);
}
