namespace SearchPulse.DemoSite;

internal sealed record DemoContentNode(
    string Name,
    string? ParentName,
    string ContentTypeAlias,
    string Heading,
    string Introduction,
    string ActionName,
    string ActionLabel,
    string Detail);

internal static class DemoContentTree
{
    public const string HomeAlias = "searchPulseDemoHome";
    public const string PageAlias = "searchPulseDemoPage";
    public const string TemplateAlias = "searchPulseDemoPage";

    public static readonly IReadOnlyList<DemoContentNode> Nodes =
    [
        new(
            "Home",
            null,
            HomeAlias,
            "SearchPulse demo home",
            "A small Umbraco site for testing anonymous engagement signals without collecting visitor identity.",
            "book-consultation",
            "Book a consultation",
            "Use the navigation, choose an action, open the external resource, download the guide, then scroll to the end of a page."),
        new(
            "Services",
            "Home",
            PageAlias,
            "Services and SEO planning",
            "This page represents a service page where a visitor can request a pricing conversation.",
            "request-pricing",
            "Request pricing",
            "The action is a fixed anonymous label. No form values, email addresses, or visitor identifiers are sent to SearchPulse."),
        new(
            "Contact",
            "Home",
            PageAlias,
            "Contact and newsletter",
            "This page models a marketing conversion without adding a form or capturing personal data.",
            "newsletter-signup",
            "Track newsletter interest",
            "The button records only the newsletter-signup action. A real site should send form submissions to its own consent-aware form handler."),
    ];
}
