using LLama.Common;
using LLama;
using LLama.Sampling;

public class LlamaManager : IDisposable
{
    private readonly LLamaWeights _weights;
    private readonly ModelParams _params;

    public LlamaManager(string modelPath)
    {
        _params = new ModelParams(modelPath)
        {
            ContextSize = 1024,
            GpuLayerCount = 20,
        };

        // Загружаем веса. Это тяжелая операция, делается один раз.
        _weights = LLamaWeights.LoadFromFile(_params);
    }

    public async Task<string> GenerateCityDescriptionAsync(string cityName, string region)
    {
        // Создаем контекст для конкретной сессии генерации
        using var context = _weights.CreateContext(_params);
        var executor = new StatelessExecutor(_weights, _params);

        var prompt = $"### System: Ты — лаконичный справочник. Пиши строго на русском языке. Закончи описание точкой.\n" +
                     $"### User: Напиши 2 предложения о городе {cityName}. Обязательно закончи второе предложение.\n" +
                     $"### Assistant:";

        var result = "";
        var inferenceParams = new InferenceParams
        { 
            MaxTokens = 512,
            AntiPrompts = new[] { "###", "User:"} 
        };

        await foreach (var text in executor.InferAsync(prompt, inferenceParams))
        {
            result += text;
        }

        return result.Trim();
    }

    public void Dispose()
    {
        _weights?.Dispose();
    }
}