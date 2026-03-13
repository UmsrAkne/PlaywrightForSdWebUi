using System;
using System.Collections.ObjectModel;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using PlaywrightForSdWebUi.Models;
using PlaywrightForSdWebUi.Playwrights;
using Prism.Mvvm;

namespace PlaywrightForSdWebUi.ViewModels
{
    public class T2IGenerationPageViewModel : BindableBase
    {
        private readonly Channel<T2IGenerationTask> taskQueue = Channel.CreateUnbounded<T2IGenerationTask>();
        private T2IGenerationTask pendingGenerationTask = new T2IGenerationTask();
        private bool _isProcessing;

        public T2IGenerationPageViewModel()
        {
            _ = ProcessQueueAsync();
        }

        public PlaywrightContext PlaywrightContext { get; init; }

        public T2IGenerationTask PendingGenerationTask
        {
            get => pendingGenerationTask;
            set => SetProperty(ref pendingGenerationTask, value);
        }

        public ObservableCollection<T2IGenerationTask> QueuedTasks { get; } = new();

        public AsyncRelayCommand GenerateImageCommand => new (async () =>
        {
            // 現在の入力をコピーしてキューに追加
            var taskCopy = PendingGenerationTask.Clone();
            await taskQueue.Writer.WriteAsync(taskCopy);
            QueuedTasks.Add(taskCopy);

            Console.WriteLine("タスクを予約しました。");
        });

        public AsyncRelayCommand FetchSettingsCommand => new (async () =>
        {
            if (PlaywrightContext.Page == null)
            {
                return;
            }

            // 1. プロンプトの取得
            var promptSelector = "#txt2img_prompt textarea";
            PendingGenerationTask.Prompt = await PlaywrightContext.Page.InputValueAsync(promptSelector);

            // 2. Negative Prompt (ネガティブプロンプト) の取得
            PendingGenerationTask.NegativePrompt
                = await PlaywrightContext.Page.GetByRole(AriaRole.Textbox, new() { Name = "Negative prompt", })
                    .InputValueAsync();

            // 3. Width (幅) の取得
            var widthStr = await PlaywrightContext.Page.Locator("#txt2img_width")
                .GetByRole(AriaRole.Spinbutton)
                .InputValueAsync();
            if (int.TryParse(widthStr, out var width))
            {
                PendingGenerationTask.Width = width;
            }

            // 4. Height (高さ) の取得
            var heightStr = await PlaywrightContext.Page.Locator("#txt2img_height")
                .GetByRole(AriaRole.Spinbutton)
                .InputValueAsync();
            if (int.TryParse(heightStr, out var height))
            {
                PendingGenerationTask.Height = height;
            }

            Console.WriteLine($"設定を取得しました: {PendingGenerationTask.Width}x{PendingGenerationTask.Height}");
        });

        private async Task ExecuteGenerationAsync(T2IGenerationTask task)
        {
            if (PlaywrightContext.Page == null)
            {
                return;
            }

            task.Status = GenerationStatus.InProgress;

            // 1. プロンプトの入力
            var promptSelector = "#txt2img_prompt textarea";
            await PlaywrightContext.Page.FillAsync(promptSelector, task.Prompt);

            // 2. Negative Prompt (ネガティブプロンプト)
            await PlaywrightContext.Page.GetByRole(AriaRole.Textbox, new() { Name = "Negative prompt", })
                .FillAsync(PendingGenerationTask.NegativePrompt);

            // 3. Width (幅) の入力
            // ID "#txt2img_width" 内にある Spinbutton (数値入力欄) を特定して入力します
            await PlaywrightContext.Page.Locator("#txt2img_width")
                .GetByRole(AriaRole.Spinbutton)
                .FillAsync(task.Width.ToString());

            // 4. Height (高さ) の入力
            await PlaywrightContext.Page.Locator("#txt2img_height")
                .GetByRole(AriaRole.Spinbutton)
                .FillAsync(task.Height.ToString());

            // 5. Seed (シード値) の入力 (もし必要であれば)
            // 名前で指定する方法が確実です
            await PlaywrightContext.Page.GetByRole(AriaRole.Spinbutton, new PageGetByRoleOptions { Name = "Seed", })
                .FillAsync("-1"); // -1 は通常ランダム

            Console.WriteLine($"設定完了: {task.Width}x{task.Height}");

            // 5. 生成ボタンをクリック
            await PlaywrightContext.Page.ClickAsync("#txt2img_generate");
            Console.WriteLine("生成ボタンを押しました");

            // 【重要】生成が終わるまで待機するロジックが必要
            // 例: 生成ボタンが「Interrupt (中断)」から「Generate」に戻るまで待つ、など
            await WaitForGenerationCompleteAsync(PlaywrightContext.Page);

            task.Status = GenerationStatus.Completed;
        }

        private async Task WaitForGenerationCompleteAsync(IPage page)
        {
            // Stable Diffusion WebUI の場合、生成中はボタンの ID が変わるか、
            // プログレスバー（.progress）が表示されるので、それが消えるのを待ちます。
            // 以下は「生成ボタンが活性化(Enabled)するまで待つ」例
            await page.WaitForSelectorAsync("#txt2img_generate:not(.disabled)");
        }

        private async Task ProcessQueueAsync()
        {
            // キューからタスクが届くのを待ち続ける
            await foreach (var task in taskQueue.Reader.ReadAllAsync())
            {
                try
                {
                    _isProcessing = true;
                    await ExecuteGenerationAsync(task);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"生成エラー: {ex.Message}");
                }
                finally
                {
                    _isProcessing = false;
                }
            }
        }
    }
}