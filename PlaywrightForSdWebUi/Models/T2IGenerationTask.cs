using Prism.Mvvm;

namespace PlaywrightForSdWebUi.Models
{
    public class T2IGenerationTask : BindableBase
    {
        private string prompt;
        private string negativePrompt;
        private int width;
        private int height;

        public string Prompt { get => prompt; set => SetProperty(ref prompt, value); }

        public string NegativePrompt { get => negativePrompt; set => SetProperty(ref negativePrompt, value); }

        public int Width { get => width; set => SetProperty(ref width, value); }

        public int Height { get => height; set => SetProperty(ref height, value); }

        public T2IGenerationTask Clone()
        {
            return new T2IGenerationTask
            {
                Prompt = Prompt,
                NegativePrompt = NegativePrompt,
                Width = Width,
                Height = Height,
            };
        }
    }
}