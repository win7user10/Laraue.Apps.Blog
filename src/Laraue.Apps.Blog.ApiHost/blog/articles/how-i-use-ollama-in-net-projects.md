---
title: How we use Ollama in .NET projects
type: article
projects: [real-estate, learn-language]
description: The clean way to use local hosted models available in Ollama with .NET
createdAt: 2025-12-26
updatedAt: 2025-12-26
---
## Why to Use Ollama

Nowadays, AI is used in many applications. And your next application also may require it.
There are a lot of providers can be used, such as ChatGPT, DeepSeek or Gemini. But some applications may have
features that don’t allow to use them. 

Some reasons can be:
1. The application may use AI for processing personal data. It can be dangerous or forbidden by the law to send it
to the external provider.
2. The application may have high requirements to availability and can't rely on a third-party API.
3. The customer can be located in the country where AI providers don’t work due to export regulations.
The list of reasons can be very long.

In such cases, there is an opportunity to use locally hosted models. It can be models that are taught from scratch or
retrained. But all these techniques take a lot of time, and this is critical for MVPs. I prefer to use ready Open Source models
to check hypothesis, it allows getting a result in a short time. In later stages, if MVP proves viable, models in the application 
can be replaced with specific fine-tuned.

[Ollama](https://ollama.com) is the project provides universal API to use different models from a code. It lets to
switch models in a few seconds with minimal code changes.

## Native Usage
When the Ollama application starts, it runs a service (on port 11434 as default) which can be used to make API calls to
different models. To generate the response call the endpoint `POST /api/generate` with payload:
```json
{
    "model": "qwen2.5vl:3b",
    "prompt": "What you see on photo?",
    "stream": false,
    "images": ["base64Image"],
    "format": {
        "properties": {
            "Tags": {
                "items": {
                    "type": ["string"]
                },
                "type": ["array"]
            }
        },
        "type": ["object"]
    }
}
```
The main properties you often will change are:
1. **Model:** the model should be used. The first call to the model will start it downloading, so the response can be
received in a time. Also, Ollama usually loads the called model in the memory for 5 minutes as default. It means the
second call to the model is usually faster, as soon as no time required to load the model.
2. **Prompt:** the question for the AI. 
3. **Format:** the [structured output](https://docs.ollama.com/capabilities/structured-outputs) feature allows returning 
response in the desired format.
4. **Images:** the image base64 encoded bytes. The property will only be used only with the models supporting images.
Information about that can be found in the model description. Despite array value supported, all models I used required 
only single value in this property.

The response value for the request:
```json
{
  "model": "qwen2.5vl:3b",
  "data": {
    "Tags": ["Empty room", "No furniture", "Unfinished walls", "No design solutions", "No color scheme"]
  }
}
```

## Laraue Adapter
The API direct usage has disadvantages that make development slow. One of the main problems is to describe structured 
outputs for the requests. It is straightforward to make a mistake here, so I made the
[adapter](https://github.com/win7user10/Laraue.Core/blob/master/src/Laraue.Core.Ollama/IOllamaPredictor.cs) that will generate the
schema based on the passed C# class.

|              |                                                                                                     |
|--------------|-----------------------------------------------------------------------------------------------------|
| Nuget        | ![latest version](https://img.shields.io/nuget/v/Laraue.Core.Ollama)                                |
| Downloads    | ![latest version](https://img.shields.io/nuget/dt/Laraue.Core.Ollama)                               |
| Github       | [Laraue.Core.Ollama](https://github.com/win7user10/Laraue.Core?tab=readme-ov-file#larauecoreollama) |

To use the library, the three steps are required.  
Register the predictor class first:
```csharp
services.AddHttpClient<IOllamaPredictor, OllamaPredictor>((serviceProvider, client) =>
{
    client.BaseAddress = new Uri("http://localhost:11434/"); // Or your local ollama address
});
```

Then you need to define a contract for the AI response:
```csharp
public record PredictionResult
{
    public required string[] Tags { get; set; }
}
```

The final step is to call Ollama in your code:
```csharp
var imageBytes = File.ReadAllBytes("C://my-image.jpg");
var base64EncodedImage = Convert.ToBase64String(imageBytes);
var predictionResult = await ollamaPredictor.PredictAsync<PredictionResult>(
    model: "gemma3:12b",
    prompt: "Return info about objects on the picture",
    base64EncodedImage,
    ct);
```

The process of schema creation, request and response serialization and deserialization will make the adapter. So,
any schema changes will be processed automatically.

## Real Usage
The usage of the adapter can be found in the [real estate](https://github.com/win7user10/Laraue.Apps.RealEstate/blob/main/src/Laraue.Apps.RealEstate.Prediction.AppServices/OllamaRealEstatePredictor.cs)
project where Ollama was used to make renovation predictions on the photos.