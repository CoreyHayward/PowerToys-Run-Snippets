# PowerToys Run: Snippets plugin

Simple [PowerToys Run](https://learn.microsoft.com/windows/powertoys/run) plugin for creating and inputting text snippets.

![Snippets Demonstration](/images/Snippets.gif)

## Requirements

- PowerToys minimum version 0.77.0

## Installation

- Download the [latest release](https://github.com/CoreyHayward/PowerToys-Run-Snippets/releases/) by selecting the architecture that matches your machine: `x64` (more common) or `ARM64`
- Close PowerToys
- Extract the archive to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`
- Open PowerToys

## Usage
### Creating a Snippet
1. Enter the activation command (default: '[')
2. Separate the snippet title and content via a '-' e.g. `Snippet Title - This is the content that will be input`
3. Press ENTER to save the snippet

### Using a Snippet
1. Enter the activation command (default: '[')
2. Search on either the title or contents
3. Press ENTER to paste the content

### Deleting a Snippet
1. Enter the activation command (default: '[')
2. Search on either the title or contents
3. Press SHIFT+ENTER to delete the snippet