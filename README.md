# MindFlayer
A dotnet 6 C# OpenAI API chat client and general utility. Allows one to manipulate selected text *in any application* through global hot keys. Hooked up to the whisper api to transcribe of audio. 

## Features
* Chat interface, using streaming api
* Customisable system prompt
* Edit and replay of chat messages
* Selected text manipulation in any app (via global hotkeys/clipboard)
* Easily configurable prompts
* Global keyboard shortcut for quick access (eg. Ctrl + Alt + G)
* Audio transcription

## Installation
Simply run the app and you are good to go. God bless .NET.

## Usage
* Ctrl + Alt + P to choose operation
* Ctrl + Alt + G to replace clipboard
* Ctrl + Alt + T to generate more

## Configuration
Set API key as env variable.

    [Environment]::SetEnvironmentVariable('OPENAI_KEY', 'sk-here', 'Machine')

## Tech
Built in WPF as I wanted to play with the Material Design in XAML library - might migrate to Avalonia at some point. Pretty basic app really.

## References
The name "MindFlayer" is inspired by the powerful, intellect-devouring creature from the tabletop role-playing game Dungeons & Dragons. Just as the Mind Flayer can manipulate and control the minds of others, this tool gives you the power to manipulate and control your data with ease.

## Contributing
Feel free!

## License
Free as the wind.
