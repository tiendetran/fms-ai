using FAS.Core.Interfaces;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Threading;

namespace FAS.Api.Services;

public class OllamaService : IOllamaService
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _chatModel;
    private readonly string _embeddingModel;

    public OllamaService(IConfiguration configuration, ILogger<OllamaService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var ollamaSettings = _configuration.GetSection("OllamaSettings");
        var endpoint = ollamaSettings["Endpoint"] ?? "http://localhost:11434";
        _chatModel = ollamaSettings["ChatModel"] ?? "gpt-oss:120b-cloud";
        _embeddingModel = ollamaSettings["EmbeddingModel"] ?? "nomic-embed-text";

        _ollamaClient = new OllamaApiClient(endpoint);
        _logger.LogInformation("Ollama client initialized with endpoint: {Endpoint}", endpoint);
    }

    public async Task<string> ChatAsync(string prompt, List<Message>? conversationHistory = null)
    {
        try
        {
            var messages = conversationHistory ?? new List<Message>();
            messages.Add(new Message
            {
                Role = ChatRole.User,
                Content = prompt
            });

            var request = new ChatRequest
            {
                Model = _chatModel,
                Messages = messages,
                Stream = false
            };

            _logger.LogDebug("Sending chat request to Ollama with model: {Model}", _chatModel);

            var responseContent = new System.Text.StringBuilder();
            await foreach (var response in _ollamaClient.Chat(request))
            {
                if (response?.Message?.Content != null)
                {
                    responseContent.Append(response.Message.Content);
                }
            }

            var result = responseContent.ToString();
            if (string.IsNullOrEmpty(result))
            {
                throw new InvalidOperationException("Ollama returned empty response");
            }

            _logger.LogDebug("Received response from Ollama");
            return response.Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama chat API");
            throw;
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            _logger.LogDebug("Generating embedding for text of length: {Length}", text.Length);

            var request = new GenerateEmbeddingRequest
            {
                Model = _embeddingModel,
                Input = text
            };

            var response = await _ollamaClient.EmbedAsync(request);

            if (response?.Embeddings == null || response.Embeddings.Length == 0)
            {
                throw new InvalidOperationException("Ollama returned empty embedding");
            }

            _logger.LogDebug("Generated embedding with dimensions: {Dimensions}", response.Embeddings[0].Length);
            return response.Embeddings[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding from Ollama");
            throw;
        }
    }

    public async Task<bool> IsModelAvailableAsync()
    {
        try
        {
            var models = await _ollamaClient.ListLocalModels();
            var chatModelExists = models.Any(m => m.Name.Contains(_chatModel));
            var embeddingModelExists = models.Any(m => m.Name.Contains(_embeddingModel));

            _logger.LogInformation(
                "Model availability - Chat: {ChatAvailable}, Embedding: {EmbeddingAvailable}",
                chatModelExists, embeddingModelExists);

            return chatModelExists && embeddingModelExists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking model availability");
            return false;
        }
    }
}
