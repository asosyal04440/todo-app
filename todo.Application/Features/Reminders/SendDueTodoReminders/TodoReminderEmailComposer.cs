using System;
using System.Globalization;
using System.Net;
using System.Text;
using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Abstractions.Persistence;

namespace TodoApi.Application.Features.Reminders.SendDueTodoReminders;

public static class TodoReminderEmailComposer
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static EmailMessage Compose(TodoReminderCandidate candidate, TimeZoneInfo displayTimeZone)
    {
        if (candidate == null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        if (displayTimeZone == null)
        {
            throw new ArgumentNullException(nameof(displayTimeZone));
        }

        DateTime dueAtUtc = candidate.Todo.DueAtUtc
            ?? throw new InvalidOperationException("A reminder can only be composed for a todo with a due date.");
        DateTime dueAtLocal = TimeZoneInfo.ConvertTimeFromUtc(dueAtUtc, displayTimeZone);
        string dueAtText = dueAtLocal.ToString("dd.MM.yyyy HH:mm", TurkishCulture);

        string title = RemoveLineBreaks(candidate.Todo.Title);
        string subject = $"Hatırlatma: \"{title}\" görevinin bitişi yaklaşıyor";

        StringBuilder plainText = new StringBuilder();
        plainText.AppendLine($"Merhaba {candidate.OwnerDisplayName},");
        plainText.AppendLine();
        plainText.AppendLine($"\"{candidate.ListName}\" listendeki \"{title}\" görevinin bitiş zamanı yaklaşıyor.");
        plainText.AppendLine($"Bitiş: {dueAtText}");

        if (string.IsNullOrWhiteSpace(candidate.Todo.Description) == false)
        {
            plainText.AppendLine($"Açıklama: {candidate.Todo.Description}");
        }

        plainText.AppendLine();
        plainText.AppendLine("Bu e-posta Todo uygulaması tarafından otomatik olarak gönderildi.");

        StringBuilder html = new StringBuilder();
        html.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#26362e;line-height:1.6\">");
        html.Append($"<p>Merhaba {Encode(candidate.OwnerDisplayName)},</p>");
        html.Append($"<p><strong>{Encode(candidate.ListName)}</strong> listendeki ");
        html.Append($"<strong>{Encode(title)}</strong> görevinin bitiş zamanı yaklaşıyor.</p>");
        html.Append($"<p>Bitiş: <strong>{Encode(dueAtText)}</strong></p>");

        if (string.IsNullOrWhiteSpace(candidate.Todo.Description) == false)
        {
            html.Append($"<p style=\"color:#536158\">{Encode(candidate.Todo.Description)}</p>");
        }

        html.Append("<p style=\"color:#8c9790;font-size:12px\">Bu e-posta Todo uygulaması tarafından otomatik olarak gönderildi.</p>");
        html.Append("</div>");

        EmailMessage message = new EmailMessage(
            candidate.OwnerEmail,
            candidate.OwnerDisplayName,
            subject,
            plainText.ToString(),
            html.ToString());

        return message;
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string RemoveLineBreaks(string value)
    {
        return value.Replace("\r", " ").Replace("\n", " ");
    }
}
