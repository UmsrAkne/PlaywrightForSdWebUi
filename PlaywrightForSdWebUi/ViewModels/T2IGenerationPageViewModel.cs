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

            var page = PlaywrightContext.Page; // ショートカット
            task.Status = GenerationStatus.InProgress;

            // 1. プロンプトの入力
            await FillPageForceAsync("#txt2img_prompt textarea", task.Prompt);

            // 2. Negative Prompt
            await FillForceAsync(
                page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Negative prompt", }),
                PendingGenerationTask.NegativePrompt);

            // 3. Width / 4. Height
            await FillForceAsync(page.Locator("#txt2img_width").GetByRole(AriaRole.Spinbutton), task.Width.ToString());
            await FillForceAsync(page.Locator("#txt2img_height").GetByRole(AriaRole.Spinbutton), task.Height.ToString());

            // 5. Seed
            await FillForceAsync(page.GetByRole(AriaRole.Spinbutton, new PageGetByRoleOptions { Name = "Seed", }), "-1");

            Console.WriteLine($"設定完了: {task.Width}x{task.Height}");

            // 6. 生成ボタンをクリック (ClickもForce指定)
            await page.ClickAsync("#txt2img_generate");
            Console.WriteLine("生成ボタンを押しました");

            await WaitForGenerationCompleteAsync(page);
            task.Status = GenerationStatus.Completed;

            return;

            async Task FillForceAsync(ILocator locator, string value)
            {
                await locator.FillAsync(value, new LocatorFillOptions { Force = true, });
            }

            // Page.Fill用：セレクター指定で入力
            async Task FillPageForceAsync(string selector, string value)
            {
                await page.FillAsync(selector, value, new PageFillOptions { Force = true, });
            }
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