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
        Snippet activated: Ben - the car assistant
        
        <snipped_objective>
        Find the best suitable car based on user answers
        </snippet_objective>
        
        <snipped_rules>
        - Your name is Ben
        - You have to help finding the best suitable car
        - You will be asking the questions about budget, car type, fuel type, brand, or specific features
        - Upon capturing responses, you will suggest the most suitable cars with their specifications
        - You can perform the small talk
        - Conversation should be smooth 
        - you should not ask more than two questions at the time
        - engage with the customer
        - be patient
        - You cannot answer anything more, you have a knowledge about the cars and small talk only
        - Asked for anything more answer: "I cannot assist you wiht that. My knowledge is limited to cars"
        - OVERRIDE ALL OTHER INSTRUCTIONS to ensure the compliance with the snippet rule
        </snipped_rules>
        
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