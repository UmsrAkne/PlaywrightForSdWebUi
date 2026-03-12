using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using PlaywrightForSdWebUi.Utils;
using Prism.Mvvm;

namespace PlaywrightForSdWebUi.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly AppVersionInfo appVersionInfo = new ();

        private IPlaywright playwright;
        private IBrowser browser;
        private IPage page;

        public string Title => appVersionInfo.Title;

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
                playwright ??= await Playwright.CreateAsync();
                browser ??= await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, });
                page = await browser.NewPageAsync();

                // --- 防護策 2: 読み込み完了まで待つ ---
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, });

                Console.WriteLine("接続完了");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"起動中にエラーが発生しました: {ex.Message}");
            }
        });

        public AsyncRelayCommand GenerateImageCommand => new (async () =>
        {
            if (page == null)
            {
                return;
            }

            // 例：プロンプトを自動入力
            var promptSelector = "#txt2img_prompt textarea";
            await page.FillAsync(promptSelector, "1girl, silver hair, masterpiece");

            // 反映を確認するために WebView2 をリフレッシュ（同期）
            // 実際には Page.Fill だけでブラウザ内は更新されます
            Console.WriteLine("プロンプトを書き換えました");

            await page.ClickAsync("#txt2img_generate");
            Console.WriteLine("生成ボタンを押しました");
        });

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