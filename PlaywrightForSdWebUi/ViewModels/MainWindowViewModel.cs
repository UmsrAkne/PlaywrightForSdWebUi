using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using PlaywrightForSdWebUi.Models;
using PlaywrightForSdWebUi.Playwrights;
using PlaywrightForSdWebUi.Utils;
using Prism.Mvvm;

namespace PlaywrightForSdWebUi.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly AppVersionInfo appVersionInfo = new ();

        public MainWindowViewModel()
        {
            T2IGenerationPageViewModel = new ()
            {
                PlaywrightContext = PlaywrightContext,
            };
        }

        public string Title => appVersionInfo.Title;

        public T2IGenerationPageViewModel T2IGenerationPageViewModel { get; set; }

        public AsyncRelayCommand OpenBrowserCommand => new (async () =>
        {
            var url = "http://127.0.0.1:7860";

            // --- 防護策 1: そもそも WebUI 起動してる？ ---
            if (!await IsWebUiRunning(url))
            {
                Console.WriteLine("WebUI (127.0.0.1:7860) が見つかりません。先に WebUI を起動してください");
                return;
            }

            try
            {
                PlaywrightContext.Playwright ??= await Playwright.CreateAsync();
                PlaywrightContext.Browser ??= await PlaywrightContext.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, });
                PlaywrightContext.Page = await PlaywrightContext.Browser.NewPageAsync();

                // --- 防護策 2: 読み込み完了まで待つ ---
                await PlaywrightContext.Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, });

                Console.WriteLine("接続完了");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"起動中にエラーが発生しました: {ex.Message}");
            }
        });

        private PlaywrightContext PlaywrightContext { get; set; } = new ();

        private async Task<bool> IsWebUiRunning(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2); // 短めのタイムアウト
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}