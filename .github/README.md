# Unity Runtime Preview Generator

**Available on Asset Store:** https://assetstore.unity.com/packages/tools/camera/runtime-preview-generator-112860

**Forum Thread:** https://forum.unity.com/threads/runtime-preview-thumbnail-generator-open-source.500194/

**Discord:** https://discord.gg/UJJt549AaV

**[Support the Developer ☕](https://yasirkula.itch.io/unity3d)**

This plugin helps you generate preview textures (thumbnails) for your **GameObjects** or **materials** in **Texture2D** format on the fly.

## FEATURES

- Supports perspective and orthographic projections
- Supports transparent background
- Customizable preview direction and background color

## INSTALLATION

There are 5 ways to install this plugin:

- import [RuntimePreviewGenerator.unitypackage](https://github.com/yasirkula/UnityRuntimePreviewGenerator/releases) via *Assets-Import Package*
- clone/[download](https://github.com/yasirkula/UnityRuntimePreviewGenerator/archive/master.zip) this repository and move the *Plugins* folder to your Unity project's *Assets* folder
- import it from [Asset Store](https://assetstore.unity.com/packages/tools/camera/runtime-preview-generator-112860)
- *(via Package Manager)* add the following line to *Packages/manifest.json*:
  - `"com.yasirkula.runtimepreviewgenerator": "https://github.com/yasirkula/UnityRuntimePreviewGenerator.git",`
- *(via [OpenUPM](https://openupm.com))* after installing [openupm-cli](https://github.com/openupm/openupm-cli), run the following command:
  - `openupm add com.yasirkula.runtimepreviewgenerator`

## HOW TO

There are 2 main functions and on Unity 2018.2 or newer, their async variants:

```csharp
public static Texture2D GenerateMaterialPreview( Material material, PrimitiveType previewPrimitive, int width = 64, int height = 64 );
public static Texture2D GenerateModelPreview( Transform model, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true );

// !!! Async versions use AsyncGPUReadback.Request so they won't work on all platforms or Graphics APIs !!!
public static void GenerateMaterialPreviewAsync( Action<Texture2D> callback, Material material, PrimitiveType previewPrimitive, int width = 64, int height = 64 );
public static void GenerateModelPreviewAsync( Action<Texture2D> callback, Transform model, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true );
```

**RuntimePreviewGenerator.GenerateMaterialPreview** function generates thumbnail for a *material* using a primitive object whereas **RuntimePreviewGenerator.GenerateModelPreview** function generates thumbnail for a *GameObject* (can be either a prefab or a scene object).

The **width** and **height** parameters define the size of the thumbnail texture. To guarantee a clear shot from the model in *GenerateModelPreview* function, the model is moved far away from the origin of the world (if it is not static) and then returned to its original position after the thumbnail is generated (this effect won't be visible to any scene cameras).

If **shouldCloneModel** parameter in *GenerateModelPreview* function is set to *true*, the model is instantiated (cloned) and the clone is used to create the thumbnail texture. This parameter is automatically set to *true* for prefabs. Unless you absolutely do not want your scene object to be moved, keep it as *false* for increased performance.

When the model has a child *Particle System*, the returned thumbnail may look empty. That's because Particle Systems usually have ridiculously large bounds and the camera needs to zoom out a lot to compensate that. Therefore, by default, Particle Systems are ignored while capturing thumbnails. You can set **shouldIgnoreParticleSystems** to *false* to override this behaviour.

There are also 2 variants for these functions that use **replacement shaders** while rendering the thumbnails (see: https://docs.unity3d.com/Manual/SL-ShaderReplacement.html) (URP and HDRP aren't supported). These functions are:

```csharp
public static Texture2D GenerateMaterialPreviewWithShader( Material material, PrimitiveType previewPrimitive, Shader shader, string replacementTag, int width = 64, int height = 64 );
public static Texture2D GenerateModelPreviewWithShader( Transform model, Shader shader, string replacementTag, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true );

// !!! Async versions use AsyncGPUReadback.Request so they won't work on all platforms or Graphics APIs !!!
public static void GenerateMaterialPreviewWithShaderAsync( Action<Texture2D> callback, Material material, PrimitiveType previewPrimitive, Shader shader, string replacementTag, int width = 64, int height = 64 );
public static void GenerateModelPreviewWithShaderAsync( Action<Texture2D> callback, Transform model, Shader shader, string replacementTag, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true );
```

There are a few options to customize the generated thumbnails:

- **RuntimePreviewGenerator.PreviewRenderCamera**: *(default: null)* lets you use your own camera for rendering the thumbnails instead of an internal camera that is automatically generated by *RuntimePreviewGenerator*. This is useful when you want to render thumbnails with some post-processing effects applied. You can set it to **null** to continue using the internal camera
- **RuntimePreviewGenerator.PreviewDirection**: *(default: Vector3(-1,-1,-1))* the direction that the camera will face relative to the previewed object's forward axis. So, a value of *Vector3(0,0,-1)* will create thumbnails from directly in front of the previewed object whereas a value of *Vector3(0,0,1)* will create thumbnails from behind the previewed object
- **RuntimePreviewGenerator.Padding**: *(default: 0)* additional % padding applied to the edges of the thumbnail texture. The accepted value range is (-0.25,0.25). If there is too much space at the edges of the thumbnails, you can enter a negative value
- **RuntimePreviewGenerator.BackgroundColor**: *(default: Color(0.3,0.3,0.3,1))* background color of the thumbnail texture. If alpha value is less than 1, generated textures will be semi-transparent (or completely transparent, if alpha=0). Note that transparent textures consume more memory than opaque textures. In addition, some built-in transparent shaders like "*Legacy Shaders/Transparent/Diffuse*" may not render correctly on transparent backgrounds. There doesn't seem to be a fix for this issue; you can search "*Unity Transparent Diffuse RenderTexture issue*" to learn more
- **RuntimePreviewGenerator.OrthographicMode**: *(default: false)* determines whether orthographic projection (*true*) or perspective projection (*false*) will be used while rendering the thumbnails. Thumbnails rendered with orthographic projection usually fill more pixels and are more center-aligned when compared to perspective projection
- **RuntimePreviewGenerator.RenderSupersampling**: *(default: 1)* when not set to 1, thumbnails will be supersampled. For example, a value of 2.0 will render the thumbnails at 2x resolution and then the results will be downscaled to the original resolution. Supersampling helps fix shadow issues and can help with anti-aliasing but it's more expensive to calculate and for values larger than 2.0, it can introduce more aliasing than it fixes
- **RuntimePreviewGenerator.MarkTextureNonReadable**: *(default: true)* marks the generated texture as non-readable for better memory usage. If you plan to modify the texture later (e.g. *GetPixels/SetPixels*), set its value to *false*

Please note that procedurally generated textures are not automatically garbage collected (until scene changes), so you are recommended to call **Destroy(thumbnailTexture)** or **DestroyImmediate(thumbnailTexture)** after you no longer need a thumbnail.

Here are two example thumbnails, one with perspective projection and grey background and the other with orthographic projection and transparent background:

![perspective](Images/example1.png)
![orthographic](Images/example2.png)

**DEBUG_BOUNDS Mode**: the location of the preview render camera is calculated using the Renderer bounds of the target object. If you want, you can uncomment the first line of the *RuntimePreviewGenerator* script to see the bounds in the preview Textures

Well, that's it, I guess. Enjoy!
