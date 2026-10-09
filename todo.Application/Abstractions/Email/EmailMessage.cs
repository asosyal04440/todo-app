using System;

namespace TodoApi.Application.Abstractions.Email;

public sealed class EmailMessage
{
    private readonly string _toAddress;
    private readonly string _toName;
    private readonly string _subject;
    private readonly string _plainTextBody;
    private readonly string _htmlBody;

    public EmailMessage(
        string toAddress,
        string toName,
        string subject,
        string plainTextBody,
        string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            throw new ArgumentException("A recipient address is required.", nameof(toAddress));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("A subject is required.", nameof(subject));
        }

        _toAddress = toAddress;
        _toName = toName ?? string.Empty;
        _subject = subject;
        _plainTextBody = plainTextBody ?? string.Empty;
        _htmlBody = htmlBody ?? string.Empty;
    }

    public string ToAddress
    {
        get
        {
            return _toAddress;
        }
    }

    public string ToName
    {
        get
        {
            return _toName;
        }
    }

    public string Subject
    {
        get
        {
            return _subject;
        }
    }

    public string PlainTextBody
    {
        get
        {
            return _plainTextBody;
        }
    }

    public string HtmlBody
    {
        get
        {
            return _htmlBody;
        }
    }
}
