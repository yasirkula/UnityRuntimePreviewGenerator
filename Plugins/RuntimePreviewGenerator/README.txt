= Runtime Preview Generator =

Online documentation available at: https://github.com/yasirkula/UnityRuntimePreviewGenerator
E-mail: yasirkula@gmail.com

1. ABOUT
This plugin helps you generate preview textures (thumbnails) for your GameObject's or materials on the fly.

2. HOW TO
Simply call the following functions to generate thumbnails for your GameObject's or materials:

public static Texture2D RuntimePreviewGenerator.GenerateMaterialPreview( Material material, PrimitiveType previewPrimitive, int width = 64, int height = 64 );
public static Texture2D RuntimePreviewGenerator.GenerateModelPreview( Transform model, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true );

Async versions use AsyncGPUReadback.Request so they won't work on all platforms or Graphics APIs:

public static void RuntimePreviewGenerator.GenerateMaterialPreviewAsync( Action<Texture2D> callback, Material material, PrimitiveType previewPrimitive, int width = 64, int height = 64 );
public static void RuntimePreviewGenerator.GenerateModelPreviewAsync( Action<Texture2D> callback, Transform model, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true );

The width and height parameters define the size of the thumbnail texture. To guarantee a clear shot from the model in GenerateModelPreview function, the model is moved far away from the origin of the world (if it is not static) and then returned to its original position after the thumbnail is generated (this effect won't be visible to any scene cameras).
If shouldCloneModel parameter in GenerateModelPreview function is set to true, the model is instantiated (cloned) and the clone is used to create the thumbnail texture. This parameter is automatically set to true for prefabs. Unless you absolutely do not want your scene object to be moved, keep it as false for increased performance.
When the model has a child Particle System, the returned thumbnail may look empty. That's because Particle Systems usually have ridiculously large bounds and the camera needs to zoom out a lot to compensate that. Therefore, by default, Particle Systems are ignored while capturing thumbnails. You can set shouldIgnoreParticleSystems to false to override this behaviour.

To customize the preview texture, use the following properties (before calling the generate functions):

- RuntimePreviewGenerator.PreviewRenderCamera : Camera
- RuntimePreviewGenerator.PreviewDirection : Vector3
- RuntimePreviewGenerator.Padding : float
- RuntimePreviewGenerator.BackgroundColor : Color
- RuntimePreviewGenerator.OrthographicMode : bool
- RuntimePreviewGenerator.RenderSupersampling : float
- RuntimePreviewGenerator.MarkTextureNonReadable : bool