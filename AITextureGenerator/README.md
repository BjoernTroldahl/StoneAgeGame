# AI Texture Generator

## Overview
The AI Texture Generator is a Unity in-editor tool designed to generate textures based on images that users can drag and drop into the custom editor window. This tool simplifies the process of creating textures for use in Unity projects.

## Features
- Drag and drop images into the editor window.
- Generate textures based on the input images.
- Apply transformations to the generated textures.

## Getting Started
1. **Installation**: Clone or download the project and place it in your Unity project's `Assets` folder.
2. **Open the Tool**: In Unity, navigate to `Window > AI Texture Generator` to open the custom editor window.
3. **Using the Tool**:
   - Drag and drop your images into the designated area in the editor window.
   - Configure any necessary settings for texture generation.
   - Click the "Generate" button to create your textures.

## Classes
- **AITextureGeneratorWindow**: Manages the custom editor window and user interactions.
- **AITextureGeneratorProcessor**: Processes the images and generates textures.
- **AITextureGenerator**: Main logic for the texture generation workflow.
- **TextureData**: Data structure for holding texture attributes.

## License
This project is licensed under the MIT License. See the LICENSE file for details.