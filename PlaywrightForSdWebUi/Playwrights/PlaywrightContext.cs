using Microsoft.Playwright;

namespace PlaywrightForSdWebUi.Playwrights
{
    public class PlaywrightContext
    {
        public IPlaywright Playwright { get; set; }

        public IBrowser Browser { get; set; }

        public IPage Page { get; set; }
    }
}