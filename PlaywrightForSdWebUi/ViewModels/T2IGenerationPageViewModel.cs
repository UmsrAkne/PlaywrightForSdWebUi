using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using PlaywrightForSdWebUi.Models;
using PlaywrightForSdWebUi.Playwrights;
using Prism.Mvvm;

namespace PlaywrightForSdWebUi.ViewModels
{
    public class T2IGenerationPageViewModel : BindableBase
    {
        private T2IGenerationTask pendingGenerationTask = new T2IGenerationTask();

        public PlaywrightContext PlaywrightContext { get; init; }

        public T2IGenerationTask PendingGenerationTask
        {
            get => pendingGenerationTask;
            set => SetProperty(ref pendingGenerationTask, value);
        }

        public AsyncRelayCommand GenerateImageCommand => new (async () =>
        {
            if (PlaywrightContext.Page == null)
            {
                return;
            }

            // 1. プロンプトの入力
            var promptSelector = "#txt2img_prompt textarea";
            await PlaywrightContext.Page.FillAsync(promptSelector, PendingGenerationTask.Prompt);

            // 2. Negative Prompt (ネガティブプロンプト)
            await PlaywrightContext.Page.GetByRole(AriaRole.Textbox, new() { Name = "Negative prompt", })
                .FillAsync(PendingGenerationTask.NegativePrompt);

            // 3. Width (幅) の入力
            // ID "#txt2img_width" 内にある Spinbutton (数値入力欄) を特定して入力します
            await PlaywrightContext.Page.Locator("#txt2img_width")
                .GetByRole(AriaRole.Spinbutton)
                .FillAsync(PendingGenerationTask.Width.ToString());

            // 4. Height (高さ) の入力
            await PlaywrightContext.Page.Locator("#txt2img_height")
                .GetByRole(AriaRole.Spinbutton)
                .FillAsync(PendingGenerationTask.Height.ToString());

            // 5. Seed (シード値) の入力 (もし必要であれば)
            // 名前で指定する方法が確実です
            await PlaywrightContext.Page.GetByRole(AriaRole.Spinbutton, new PageGetByRoleOptions { Name = "Seed", })
                .FillAsync("-1"); // -1 は通常ランダム

            Console.WriteLine($"設定完了: {PendingGenerationTask.Width}x{PendingGenerationTask.Height}");

            // 5. 生成ボタンをクリック
            await PlaywrightContext.Page.ClickAsync("#txt2img_generate");
            Console.WriteLine("生成ボタンを押しました");
        });
    }
}