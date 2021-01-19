# Unity Runtime Preview Generator

**Available on Asset Store:** https://assetstore.unity.com/packages/tools/camera/runtime-preview-generator-112860

**Forum Thread:** https://forum.unity.com/threads/runtime-preview-thumbnail-generator-open-source.500194/

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

There are 2 main functions:

```csharp
public static Texture2D GenerateMaterialPreview( Material material, PrimitiveType previewObject, int width = 64, int height = 64 );
public static Texture2D GenerateModelPreview( Transform model, int width = 64, int height = 64, bool shouldCloneModel = false );
```

**RuntimePreviewGenerator.GenerateMaterialPreview** function generates thumbnail for a *material* using a primitive object whereas **RuntimePreviewGenerator.GenerateModelPreview** function generates thumbnail for a *GameObject* (can be either a prefab or a scene object).

The **width** and **height** parameters define the size of the thumbnail texture. To guarantee a clear shot from the model in *GenerateModelPreview* function, the model is moved far away from the origin of the world (if it is not static) and then returned to its original position after the thumbnail is generated (this effect won't be visible to any scene cameras). If **shouldCloneModel** parameter in *GenerateModelPreview* function is set to *true*, the model is instantiated (cloned) and the clone is used to create the thumbnail texture. This parameter is automatically set to *true* for prefabs. Unless you absolutely do not want your scene object to be moved, keep it as *false* for increased performance.

There are also 2 variants for these functions that use **replacement shaders** while rendering the thumbnails (see: https://docs.unity3d.com/Manual/SL-ShaderReplacement.html). These functions are:

```csharp
public static Texture2D GenerateMaterialPreviewWithShader( Material material, PrimitiveType previewPrimitive, Shader shader, string replacementTag, int width = 64, int height = 64 );
public static Texture2D GenerateModelPreviewWithShader( Transform model, Shader shader, string replacementTag, int width = 64, int height = 64, bool shouldCloneModel = false );
```

There are a few options to customize the generated thumbnails:

- **RuntimePreviewGenerator.PreviewRenderCamera**: *(default: null)* lets you use your own camera for rendering the thumbnails instead of an internal camera that is automatically generated by *RuntimePreviewGenerator*. This is useful when you want to render thumbnails with some post-processing effects applied. You can set it to **null** to continue using the internal camera
- **RuntimePreviewGenerator.PreviewDirection**: *(default: Vector3(-1,-1,-1))* the direction that the camera will face relative to the previewed object's forward axis. So, a value of *Vector3(0,0,-1)* will create thumbnails from directly in front of the previewed object whereas a value of *Vector3(0,0,1)* will create thumbnails from behind the previewed object
- **RuntimePreviewGenerator.Padding**: *(default: 0)* additional % padding applied to the edges of the thumbnail texture. The accepted value range is (-0.25,0.25). Enter a negative value if there is too much space at the edges of the thumbnails by default
- **RuntimePreviewGenerator.BackgroundColor**: *(default: Color(0.3,0.3,0.3,1))* background color of the thumbnail texture. If alpha value is less than 1, generated textures will be semi-transparent (or completely transparent, if alpha=0). Note that transparent textures consume more memory than opaque textures
- **RuntimePreviewGenerator.OrthographicMode**: *(default: false)* determines whether orthographic projection (*true*) or perspective projection (*false*) will be used while rendering the thumbnails. Thumbnails rendered with orthographic projection usually fill more pixels and are more center-aligned when compared to perspective projection
- **RuntimePreviewGenerator.MarkTextureNonReadable**: *(default: true)* marks the generated texture as non-readable for better memory usage. If you plan to modify the texture later (e.g. *GetPixels/SetPixels*), set its value to *false*

Please note that procedurally generated textures are not automatically garbage collected (until scene changes), so you are recommended to call **Destroy(thumbnailTexture)** or **DestroyImmediate(thumbnailTexture)** after you no longer need a thumbnail.

Here are two example thumbnails, one with perspective projection and grey background and the other with orthographic projection and transparent background:

![perspective](Images/example1.png)
![orthographic](Images/example2.png)

**DEBUG_BOUNDS Mode**: the location of the preview render camera is calculated using the Renderer bounds of the target object. If you want, you can uncomment the first line of the *RuntimePreviewGenerator* script to see the corners of the bounds in the thumbnail textures

Well, that's it, I guess. Enjoy!