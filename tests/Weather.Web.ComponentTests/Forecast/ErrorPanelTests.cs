using AngleSharp.Dom;
using Bunit;
using Shouldly;
using Weather.Web.Components.Forecast;

namespace Weather.Web.ComponentTests.Forecast;

public sealed class ErrorPanelTests : BunitContext
{
    [Fact]
    public void Panel_AnnouncesItselfToScreenReaders()
    {
        IRenderedComponent<ErrorPanel> panel = Render<ErrorPanel>(parameters => parameters
            .Add(component => component.Message, "Сервис недоступен."));

        IElement section = panel.Find("section");

        section.GetAttribute("role").ShouldBe("alert");
        section.GetAttribute("aria-live").ShouldBe("assertive");
    }

    [Fact]
    public void RetryButton_RaisesCallback()
    {
        var clicks = 0;

        IRenderedComponent<ErrorPanel> panel = Render<ErrorPanel>(parameters => parameters
            .Add(component => component.Message, "Сервис недоступен.")
            .Add(component => component.OnRetry, () => clicks++));

        panel.Find("button").Click();

        clicks.ShouldBe(1);
    }

    [Fact]
    public void WhileRetrying_ButtonIsDisabledToPreventDoubleRequests()
    {
        IRenderedComponent<ErrorPanel> panel = Render<ErrorPanel>(parameters => parameters
            .Add(component => component.Message, "Сервис недоступен.")
            .Add(component => component.IsRetrying, true));

        IElement button = panel.Find("button");

        button.HasAttribute("disabled").ShouldBeTrue();
        button.TextContent.Trim().ShouldBe("Повторяем…");
    }
}
