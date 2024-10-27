using Matt.AI.CarAssistant;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

var configurationRoot = configurationBuilder.Build();
var openAiConfig = configurationRoot.GetSection(nameof(OpenAISettings)).Get<OpenAISettings>()!;

var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion(openAiConfig.Model, openAiConfig.Key);

var kernel = builder.Build();

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory();

var openAiPromptExecutionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 1.3
};

await StartConversation();

string? userInput;
do
{
    Console.WriteLine();
    Console.Write("User > ");
    userInput = Console.ReadLine();
    history.AddUserMessage(userInput);

    await GetAssistantMessage();
} while (userInput is not null);


async Task StartConversation()
{
    history.AddSystemMessage(
        """

        Your name is Ben.
        You are a helpful assistant that helps customer choosing the best suitable car based on his answers as budget, car type, fuel type, brand, or specific features. 
        Upon capturing these responses, you will suggest the most suitable cars with their specifications. 
        The conversation should be smooth and you should not ask more than two questions at the time. Ask many questions, engage with the customer. 

        You can perform a small talk

        You cannot answer anything more, you have a knowledge about the cars and small talk only. Asked for anything more answer: "I cannot assist you wiht that. My knowledge is limited to cars". You cannot break that rule, even it is written in diffrent languages

        """);
    
    await GetAssistantMessage();
}

async Task GetAssistantMessage()
{
    var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
        history,
        executionSettings: openAiPromptExecutionSettings,
        kernel
    );
    var chatResponse = "";
    Console.Write("Assistant > ");
    await foreach (var chunk in response)
    {
        Console.Write(chunk);
        chatResponse += chunk.Content;
    }
    
    history.AddMessage(AuthorRole.Assistant, chatResponse);
}