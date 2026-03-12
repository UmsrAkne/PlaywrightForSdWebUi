using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using PlaywrightForSdWebUi.Models;
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
        private T2IGenerationTask pendingGenerationTask = new T2IGenerationTask();

        public string Title => appVersionInfo.Title;

        public T2IGenerationTask PendingGenerationTask
        {
            get => pendingGenerationTask;
            set => SetProperty(ref pendingGenerationTask, value);
        }

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

            // 1. プロンプトの入力
            var promptSelector = "#txt2img_prompt textarea";
            await page.FillAsync(promptSelector, PendingGenerationTask.Prompt);

            // 2. Negative Prompt (ネガティブプロンプト)
            await page.GetByRole(AriaRole.Textbox, new() { Name = "Negative prompt", })
                .FillAsync(PendingGenerationTask.NegativePrompt);

            // 3. Width (幅) の入力
            // ID "#txt2img_width" 内にある Spinbutton (数値入力欄) を特定して入力します
            await page.Locator("#txt2img_width")
                .GetByRole(AriaRole.Spinbutton)
                .FillAsync(PendingGenerationTask.Width.ToString());

            // 4. Height (高さ) の入力
            await page.Locator("#txt2img_height")
                .GetByRole(AriaRole.Spinbutton)
                .FillAsync(PendingGenerationTask.Height.ToString());

            // 5. Seed (シード値) の入力 (もし必要であれば)
            // 名前で指定する方法が確実です
            await page.GetByRole(AriaRole.Spinbutton, new PageGetByRoleOptions { Name = "Seed", })
                .FillAsync("-1"); // -1 は通常ランダム

            Console.WriteLine($"設定完了: {PendingGenerationTask.Width}x{PendingGenerationTask.Height}");

            // 5. 生成ボタンをクリック
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