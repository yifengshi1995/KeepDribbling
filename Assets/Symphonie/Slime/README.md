# Demo Scenes
Demo scenes can be found in:
- 📂 Symphonie/Models/Slime/Demo/
    - 📄Demo.unity
    - 📄Demo_URP.unity - **URP Demo Scene**
    - 📄AnimDisplay.unity - **For showcasing animations**

# Model
A slime blob with 4 LODs.

# Shader
Shaders for the built-in pipeline and URP are provided. *No support for HDRP.*
- Blends smoothly into the environmental lighting.
- Or use the 'Fake Environment' option to completely decouple from the scene.

> 💡The shader is constructed using Amplify Shader Editor.
>
> ❗Please note that the built-in pipeline version is missing a required node, so we had to copy and modify the original "Indirect Specular Lighting" node. We can't share the source file because it's essentially a copy of ASE's source code. **THIS WILL NOT AFFECT THE SHADER'S FUNCTIONALITY**. It only matters if you want to modify the shader. The URP version works fine.

# Scripts
This package includes several useful scripts:
- 📂 Symphonie/Models/Slime/Scripts/
    - 📄 SlimeVisual.cs - **For correctly rendering the model with the provided shader**
    - 📂 Editor
        - 📄 CoreScatterLUTBakerWizard.cs - **A utility for baking the Lookup Table for the core scattering effect**
- 📂 Symphonie/Models/Slime/Demo/
    - 📄 SlimeController.cs - **For controlling the slime via AI or player input**
