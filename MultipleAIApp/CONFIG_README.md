# Configuration System Setup

## Overview
This application now includes a configuration system that allows you to store API keys and settings in a JSON file instead of entering them manually each time.

## Setup Instructions

1. **Copy the Example Configuration File**
   - Locate `config.example.json` in the root directory
   - Copy it and rename to `config.json`
   - This file is automatically ignored by Git to keep your keys secure

2. **Add Your API Keys**
   Open `config.json` and add your API keys:
   ```json
   {
     "apiKeys": {
       "huggingFace": "YOUR_HUGGINGFACE_API_KEY_HERE",
       "openAi": "YOUR_OPENAI_API_KEY_HERE",
       "anthropic": "YOUR_ANTHROPIC_API_KEY_HERE",
       "google": "YOUR_GOOGLE_API_KEY_HERE",
       "azure": {
         "endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
         "apiKey": "YOUR_AZURE_API_KEY_HERE"
       }
     }
   }
   ```

3. **Configuration Options**
   - **apiKeys**: Store various provider API keys
   - **defaultSettings**: Configure default parameters like temperature, max tokens
   - **logging**: Set logging level and file output
   - **ui**: UI preferences like theme and auto-save interval
   - **models**: Define available AI models and their parameters

## Security Notes
- `config.json` is excluded from Git via `.gitignore`
- Never commit your actual API keys
- You can still override API keys via environment variables:
  - `HUGGINGFACE_API_KEY`
  - `OPENAI_API_KEY`
  - `ANTHROPIC_API_KEY`
  - `GOOGLE_API_KEY`
  - `AZURE_API_KEY`

## Usage
Once configured, the application will automatically:
- Load API keys from `config.json` on startup
- Use the appropriate key based on the selected model provider
- Fall back to environment variables if config file keys are not found
- Allow manual API key entry in the UI which overrides config settings

## Files Added
- `config.example.json` - Template configuration file (safe to commit)
- `Models/AppConfiguration.cs` - Configuration model classes
- `Services/ConfigurationService.cs` - Service to manage configuration loading

## Troubleshooting
- If the app can't find your config, it will log a warning and prompt you to create one
- Check the logs for configuration loading errors
- Ensure your JSON is valid (no trailing commas, proper quotes)