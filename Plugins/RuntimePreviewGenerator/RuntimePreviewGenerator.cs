//#define DEBUG_BOUNDS

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2018_2_OR_NEWER
using UnityEngine.Rendering;
#endif
using Object = UnityEngine.Object;

public static class RuntimePreviewGenerator
{
	private class CameraSetup
	{
		private Vector3 position;
		private Quaternion rotation;

		private Color backgroundColor;
		private bool orthographic;
		private float orthographicSize;
		private float nearClipPlane;
		private float farClipPlane;
		private float aspect;
		private int cullingMask;
		private CameraClearFlags clearFlags;

		private RenderTexture targetTexture;

		public void GetSetup( Camera camera )
		{
			position = camera.transform.position;
			rotation = camera.transform.rotation;

			backgroundColor = camera.backgroundColor;
			orthographic = camera.orthographic;
			orthographicSize = camera.orthographicSize;
			nearClipPlane = camera.nearClipPlane;
			farClipPlane = camera.farClipPlane;
			aspect = camera.aspect;
			cullingMask = camera.cullingMask;
			clearFlags = camera.clearFlags;

			targetTexture = camera.targetTexture;
		}

		public void ApplySetup( Camera camera )
		{
			camera.transform.position = position;
			camera.transform.rotation = rotation;

			camera.backgroundColor = backgroundColor;
			camera.orthographic = orthographic;
			camera.orthographicSize = orthographicSize;
			camera.aspect = aspect;
			camera.cullingMask = cullingMask;
			camera.clearFlags = clearFlags;

			// Assigning order or nearClipPlane and farClipPlane may matter because Unity clamps near to far and far to near
			if( nearClipPlane < camera.farClipPlane )
			{
				camera.nearClipPlane = nearClipPlane;
				camera.farClipPlane = farClipPlane;
			}
			else
			{
				camera.farClipPlane = farClipPlane;
				camera.nearClipPlane = nearClipPlane;
			}

			camera.targetTexture = targetTexture;
			targetTexture = null;
		}
	}

	private const int PREVIEW_LAYER = 22;
	private static Vector3 PREVIEW_POSITION = new Vector3( -250f, -250f, -250f );

	private static Camera renderCamera;
	private static readonly CameraSetup cameraSetup = new CameraSetup();

	private static readonly Vector3[] boundingBoxPoints = new Vector3[8];

	private static readonly List<Renderer> renderersList = new List<Renderer>( 64 );
	private static readonly List<int> layersList = new List<int>( 64 );

#if DEBUG_BOUNDS
	private static Material boundsDebugMaterial;
#endif

	private static Camera m_internalCamera = null;
	private static Camera InternalCamera
	{
		get
		{
			if( m_internalCamera == null )
			{
				m_internalCamera = new GameObject( "ModelPreviewGeneratorCamera" ).AddComponent<Camera>();
				m_internalCamera.enabled = false;
				m_internalCamera.nearClipPlane = 0.01f;
				m_internalCamera.cullingMask = 1 << PREVIEW_LAYER;
				m_internalCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
			}

			return m_internalCamera;
		}
	}

	private static Camera m_previewRenderCamera;
	public static Camera PreviewRenderCamera
	{
		get { return m_previewRenderCamera; }
		set { m_previewRenderCamera = value; }
	}

	private static Vector3 m_previewDirection = new Vector3( -0.57735f, -0.57735f, -0.57735f ); // Normalized (-1,-1,-1)
	public static Vector3 PreviewDirection
	{
		get { return m_previewDirection; }
		set { m_previewDirection = value.normalized; }
	}

	private static float m_padding;
	public static float Padding
	{
		get { return m_padding; }
		set { m_padding = Mathf.Clamp( value, -0.25f, 0.25f ); }
	}

	private static Color m_backgroundColor = new Color( 0.3f, 0.3f, 0.3f, 1f );
	public static Color BackgroundColor
	{
		get { return m_backgroundColor; }
		set { m_backgroundColor = value; }
	}

	private static bool m_orthographicMode = false;
	public static bool OrthographicMode
	{
		get { return m_orthographicMode; }
		set { m_orthographicMode = value; }
	}

	private static float m_renderSupersampling = 1f;
	public static float RenderSupersampling
	{
		get { return m_renderSupersampling; }
		set { m_renderSupersampling = Mathf.Max( value, 0.1f ); }
	}

	private static bool m_markTextureNonReadable = true;
	public static bool MarkTextureNonReadable
	{
		get { return m_markTextureNonReadable; }
		set { m_markTextureNonReadable = value; }
	}

	public static Texture2D GenerateMaterialPreview( Material material, PrimitiveType previewPrimitive, int width = 64, int height = 64 )
	{
		return GenerateMaterialPreviewInternal( material, previewPrimitive, null, null, width, height );
	}

	public static Texture2D GenerateMaterialPreviewWithShader( Material material, PrimitiveType previewPrimitive, Shader shader, string replacementTag, int width = 64, int height = 64 )
	{
		return GenerateMaterialPreviewInternal( material, previewPrimitive, shader, replacementTag, width, height );
	}

#if UNITY_2018_2_OR_NEWER
	public static void GenerateMaterialPreviewAsync( Action<Texture2D> callback, Material material, PrimitiveType previewPrimitive, int width = 64, int height = 64 )
	{
		GenerateMaterialPreviewInternal( material, previewPrimitive, null, null, width, height, callback );
	}

	public static void GenerateMaterialPreviewWithShaderAsync( Action<Texture2D> callback, Material material, PrimitiveType previewPrimitive, Shader shader, string replacementTag, int width = 64, int height = 64 )
	{
		GenerateMaterialPreviewInternal( material, previewPrimitive, shader, replacementTag, width, height, callback );
	}
#endif

#if UNITY_2018_2_OR_NEWER
	private static Texture2D GenerateMaterialPreviewInternal( Material material, PrimitiveType previewPrimitive, Shader shader, string replacementTag, int width, int height, Action<Texture2D> asyncCallback = null )
#else
	private static Texture2D GenerateMaterialPreviewInternal( Material material, PrimitiveType previewPrimitive, Shader shader, string replacementTag, int width, int height )
#endif
	{
		GameObject previewModel = GameObject.CreatePrimitive( previewPrimitive );
		previewModel.gameObject.hideFlags = HideFlags.HideAndDontSave;
		previewModel.GetComponent<Renderer>().sharedMaterial = material;

		try
		{
#if UNITY_2018_2_OR_NEWER
			return GenerateModelPreviewInternal( previewModel.transform, shader, replacementTag, width, height, false, true, asyncCallback );
#else
			return GenerateModelPreviewInternal( previewModel.transform, shader, replacementTag, width, height, false, true );
#endif
		}
		catch( Exception e )
		{
			Debug.LogException( e );
		}
		finally
		{
			Object.DestroyImmediate( previewModel );
		}

		return null;
	}

	public static Texture2D GenerateModelPreview( Transform model, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true )
	{
		return GenerateModelPreviewInternal( model, null, null, width, height, shouldCloneModel, shouldIgnoreParticleSystems );
	}

	public static Texture2D GenerateModelPreviewWithShader( Transform model, Shader shader, string replacementTag, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true )
	{
		return GenerateModelPreviewInternal( model, shader, replacementTag, width, height, shouldCloneModel, shouldIgnoreParticleSystems );
	}

#if UNITY_2018_2_OR_NEWER
	public static void GenerateModelPreviewAsync( Action<Texture2D> callback, Transform model, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true )
	{
		GenerateModelPreviewInternal( model, null, null, width, height, shouldCloneModel, shouldIgnoreParticleSystems, callback );
	}

	public static void GenerateModelPreviewWithShaderAsync( Action<Texture2D> callback, Transform model, Shader shader, string replacementTag, int width = 64, int height = 64, bool shouldCloneModel = false, bool shouldIgnoreParticleSystems = true )
	{
		GenerateModelPreviewInternal( model, shader, replacementTag, width, height, shouldCloneModel, shouldIgnoreParticleSystems, callback );
	}
#endif

#if UNITY_2018_2_OR_NEWER
	private static Texture2D GenerateModelPreviewInternal( Transform model, Shader shader, string replacementTag, int width, int height, bool shouldCloneModel, bool shouldIgnoreParticleSystems, Action<Texture2D> asyncCallback = null )
#else
	private static Texture2D GenerateModelPreviewInternal( Transform model, Shader shader, string replacementTag, int width, int height, bool shouldCloneModel, bool shouldIgnoreParticleSystems )
#endif
	{
		if( !model )
		{
#if UNITY_2018_2_OR_NEWER
			if( asyncCallback != null )
				asyncCallback( null );
#endif

			return null;
		}

		Texture2D result = null;

		if( !model.gameObject.scene.IsValid() || !model.gameObject.scene.isLoaded )
			shouldCloneModel = true;

		Transform previewObject;
		if( shouldCloneModel )
		{
			previewObject = (Transform) Object.Instantiate( model, null, false );
			previewObject.gameObject.hideFlags = HideFlags.HideAndDontSave;
		}
		else
		{
			previewObject = model;

			layersList.Clear();
			GetLayerRecursively( previewObject );
		}

		bool isStatic = IsStatic( model );
		bool wasActive = previewObject.gameObject.activeSelf;
		Vector3 prevPos = previewObject.position;
		Quaternion prevRot = previewObject.rotation;

#if UNITY_2018_2_OR_NEWER
		bool asyncOperationStarted = false;
#endif

#if DEBUG_BOUNDS
		Transform boundsDebugCube = null;
#endif

		try
		{
			SetupCamera();
			SetLayerRecursively( previewObject );

			if( !isStatic )
			{
				previewObject.position = PREVIEW_POSITION;
				previewObject.rotation = Quaternion.identity;
			}

			if( !wasActive )
				previewObject.gameObject.SetActive( true );

			Bounds previewBounds = new Bounds();
			if( !CalculateBounds( previewObject, shouldIgnoreParticleSystems, out previewBounds ) )
			{
#if UNITY_2018_2_OR_NEWER
				if( asyncCallback != null )
					asyncCallback( null );
#endif

				return null;
			}

#if DEBUG_BOUNDS
			if( !boundsDebugMaterial )
			{
				boundsDebugMaterial = new Material( Shader.Find( "Sprites/Default" ) )
				{
					hideFlags = HideFlags.HideAndDontSave,
					color = new Color( 0.5f, 0.5f, 0.5f, 0.5f )
				};
			}

			boundsDebugCube = GameObject.CreatePrimitive( PrimitiveType.Cube ).transform;
			boundsDebugCube.localPosition = previewBounds.center;
			boundsDebugCube.localScale = previewBounds.size;
			boundsDebugCube.gameObject.layer = PREVIEW_LAYER;
			boundsDebugCube.gameObject.hideFlags = HideFlags.HideAndDontSave;

			boundsDebugCube.GetComponent<Renderer>().sharedMaterial = boundsDebugMaterial;
#endif

			renderCamera.aspect = (float) width / height;
			renderCamera.transform.rotation = Quaternion.LookRotation( previewObject.rotation * m_previewDirection, previewObject.up );

			CalculateCameraPosition( renderCamera, previewBounds, m_padding );

			renderCamera.farClipPlane = ( renderCamera.transform.position - previewBounds.center ).magnitude + previewBounds.size.magnitude;

			RenderTexture activeRT = RenderTexture.active;
			RenderTexture renderTexture = null;
			try
			{
				int supersampledWidth = Mathf.RoundToInt( width * m_renderSupersampling );
				int supersampledHeight = Mathf.RoundToInt( height * m_renderSupersampling );

				renderTexture = RenderTexture.GetTemporary( supersampledWidth, supersampledHeight, 16 );
				RenderTexture.active = renderTexture;
				if( m_backgroundColor.a < 1f )
					GL.Clear( true, true, m_backgroundColor );

				renderCamera.targetTexture = renderTexture;

				if( !shader )
					renderCamera.Render();
				else
					renderCamera.RenderWithShader( shader, replacementTag ?? string.Empty );

				renderCamera.targetTexture = null;

				if( supersampledWidth != width || supersampledHeight != height )
				{
					RenderTexture _renderTexture = null;
					try
					{
						_renderTexture = RenderTexture.GetTemporary( width, height, 16 );
						RenderTexture.active = _renderTexture;
						if( m_backgroundColor.a < 1f )
							GL.Clear( true, true, m_backgroundColor );

						Graphics.Blit( renderTexture, _renderTexture );
					}
					finally
					{
						if( _renderTexture )
						{
							RenderTexture.ReleaseTemporary( renderTexture );
							renderTexture = _renderTexture;
						}
					}
				}

#if UNITY_2018_2_OR_NEWER
				if( asyncCallback != null )
				{
					AsyncGPUReadback.Request( renderTexture, 0, m_backgroundColor.a < 1f ? TextureFormat.RGBA32 : TextureFormat.RGB24, ( asyncResult ) =>
					{
						try
						{
							result = new Texture2D( width, height, m_backgroundColor.a < 1f ? TextureFormat.RGBA32 : TextureFormat.RGB24, false );
							if( !asyncResult.hasError )
								result.LoadRawTextureData( asyncResult.GetData<byte>() );
							else
							{
								Debug.LogWarning( "Async thumbnail request failed, falling back to conventional method" );

								RenderTexture _activeRT = RenderTexture.active;
								try
								{
									RenderTexture.active = renderTexture;
									result.ReadPixels( new Rect( 0f, 0f, width, height ), 0, 0, false );
								}
								finally
								{
									RenderTexture.active = _activeRT;
								}
							}

							result.Apply( false, m_markTextureNonReadable );
							asyncCallback( result );
						}
						finally
						{
							if( renderTexture )
								RenderTexture.ReleaseTemporary( renderTexture );
						}
					} );

					asyncOperationStarted = true;
				}
				else
#endif
				{
					result = new Texture2D( width, height, m_backgroundColor.a < 1f ? TextureFormat.RGBA32 : TextureFormat.RGB24, false );
					result.ReadPixels( new Rect( 0f, 0f, width, height ), 0, 0, false );
					result.Apply( false, m_markTextureNonReadable );
				}
			}
			finally
			{
				RenderTexture.active = activeRT;

				if( renderTexture )
				{
#if UNITY_2018_2_OR_NEWER
					if( !asyncOperationStarted )
#endif
					{
						RenderTexture.ReleaseTemporary( renderTexture );
					}
				}
			}
		}
		catch( Exception e )
		{
			Debug.LogException( e );
		}
		finally
		{
#if DEBUG_BOUNDS
			if( boundsDebugCube )
				Object.DestroyImmediate( boundsDebugCube.gameObject );
#endif

			if( shouldCloneModel )
				Object.DestroyImmediate( previewObject.gameObject );
			else
			{
				if( !wasActive )
					previewObject.gameObject.SetActive( false );

				if( !isStatic )
				{
					previewObject.position = prevPos;
					previewObject.rotation = prevRot;
				}

				int index = 0;
				SetLayerRecursively( previewObject, ref index );
			}

			if( renderCamera == m_previewRenderCamera )
				cameraSetup.ApplySetup( renderCamera );
		}

#if UNITY_2018_2_OR_NEWER
		if( !asyncOperationStarted && asyncCallback != null )
			asyncCallback( null );
#endif

		return result;
	}

	// Calculates AABB bounds of the target object (AABB will include its child objects)
	public static bool CalculateBounds( Transform target, bool shouldIgnoreParticleSystems, out Bounds bounds )
	{
		renderersList.Clear();
		target.GetComponentsInChildren( renderersList );

		bounds = new Bounds();
		bool hasBounds = false;
		for( int i = 0; i < renderersList.Count; i++ )
		{
			if( !renderersList[i].enabled )
				continue;

			if( shouldIgnoreParticleSystems && renderersList[i] is ParticleSystemRenderer )
				continue;

			if( !hasBounds )
			{
				bounds = renderersList[i].bounds;
				hasBounds = true;
			}
			else
				bounds.Encapsulate( renderersList[i].bounds );
		}

		return hasBounds;
	}

	// Moves camera in a way such that it will encapsulate bounds perfectly
	public static void CalculateCameraPosition( Camera camera, Bounds bounds, float padding = 0f )
	{
		Transform cameraTR = camera.transform;

		Vector3 cameraDirection = cameraTR.forward;
		float aspect = camera.aspect;

		if( padding != 0f )
			bounds.size *= 1f + padding * 2f; // Padding applied to both edges, hence multiplied by 2

		Vector3 boundsCenter = bounds.center;
		Vector3 boundsExtents = bounds.extents;
		Vector3 boundsSize = 2f * boundsExtents;

		// Calculate corner points of the Bounds
		Vector3 point = boundsCenter + boundsExtents;
		boundingBoxPoints[0] = point;
		point.x -= boundsSize.x;
		boundingBoxPoints[1] = point;
		point.y -= boundsSize.y;
		boundingBoxPoints[2] = point;
		point.x += boundsSize.x;
		boundingBoxPoints[3] = point;
		point.z -= boundsSize.z;
		boundingBoxPoints[4] = point;
		point.x -= boundsSize.x;
		boundingBoxPoints[5] = point;
		point.y += boundsSize.y;
		boundingBoxPoints[6] = point;
		point.x += boundsSize.x;
		boundingBoxPoints[7] = point;

		if( camera.orthographic )
		{
			cameraTR.position = boundsCenter;

			float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
			float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

			for( int i = 0; i < boundingBoxPoints.Length; i++ )
			{
				Vector3 localPoint = cameraTR.InverseTransformPoint( boundingBoxPoints[i] );
				if( localPoint.x < minX )
					minX = localPoint.x;
				if( localPoint.x > maxX )
					maxX = localPoint.x;
				if( localPoint.y < minY )
					minY = localPoint.y;
				if( localPoint.y > maxY )
					maxY = localPoint.y;
			}

			float distance = boundsExtents.magnitude + 1f;
			camera.orthographicSize = Mathf.Max( maxY - minY, ( maxX - minX ) / aspect ) * 0.5f;
			cameraTR.position = boundsCenter - cameraDirection * distance;
		}
		else
		{
			Vector3 cameraUp = cameraTR.up, cameraRight = cameraTR.right;

			float verticalFOV = camera.fieldOfView * 0.5f;
			float horizontalFOV = Mathf.Atan( Mathf.Tan( verticalFOV * Mathf.Deg2Rad ) * aspect ) * Mathf.Rad2Deg;

			// Normals of the camera's frustum planes
			Vector3 topFrustumPlaneNormal = Quaternion.AngleAxis( 90f + verticalFOV, -cameraRight ) * cameraDirection;
			Vector3 bottomFrustumPlaneNormal = Quaternion.AngleAxis( 90f + verticalFOV, cameraRight ) * cameraDirection;
			Vector3 rightFrustumPlaneNormal = Quaternion.AngleAxis( 90f + horizontalFOV, cameraUp ) * cameraDirection;
			Vector3 leftFrustumPlaneNormal = Quaternion.AngleAxis( 90f + horizontalFOV, -cameraUp ) * cameraDirection;

			// Credit for algorithm: https://stackoverflow.com/a/66113254/2373034
			// 1. Find edge points of the bounds using the camera's frustum planes
			// 2. Create a plane for each edge point that goes through the point and has the corresponding frustum plane's normal
			// 3. Find the intersection line of horizontal edge points' planes (horizontalIntersection) and vertical edge points' planes (verticalIntersection)
			//    If we move the camera along horizontalIntersection, the bounds will always with the camera's width perfectly (similar effect goes for verticalIntersection)
			// 4. Find the closest line segment between these two lines (horizontalIntersection and verticalIntersection) and place the camera at the farthest point on that line
			int leftmostPoint = -1, rightmostPoint = -1, topmostPoint = -1, bottommostPoint = -1;
			for( int i = 0; i < boundingBoxPoints.Length; i++ )
			{
				if( leftmostPoint < 0 && IsOutermostPointInDirection( i, leftFrustumPlaneNormal ) )
					leftmostPoint = i;
				if( rightmostPoint < 0 && IsOutermostPointInDirection( i, rightFrustumPlaneNormal ) )
					rightmostPoint = i;
				if( topmostPoint < 0 && IsOutermostPointInDirection( i, topFrustumPlaneNormal ) )
					topmostPoint = i;
				if( bottommostPoint < 0 && IsOutermostPointInDirection( i, bottomFrustumPlaneNormal ) )
					bottommostPoint = i;
			}

			Ray horizontalIntersection = GetPlanesIntersection( new Plane( leftFrustumPlaneNormal, boundingBoxPoints[leftmostPoint] ), new Plane( rightFrustumPlaneNormal, boundingBoxPoints[rightmostPoint] ) );
			Ray verticalIntersection = GetPlanesIntersection( new Plane( topFrustumPlaneNormal, boundingBoxPoints[topmostPoint] ), new Plane( bottomFrustumPlaneNormal, boundingBoxPoints[bottommostPoint] ) );

			Vector3 closestPoint1, closestPoint2;
			FindClosestPointsOnTwoLines( horizontalIntersection, verticalIntersection, out closestPoint1, out closestPoint2 );

			cameraTR.position = Vector3.Dot( closestPoint1 - closestPoint2, cameraDirection ) < 0 ? closestPoint1 : closestPoint2;
		}
	}

	// Returns whether or not the given point is the outermost point in the given direction among all points of the bounds
	private static bool IsOutermostPointInDirection( int pointIndex, Vector3 direction )
	{
		Vector3 point = boundingBoxPoints[pointIndex];
		for( int i = 0; i < boundingBoxPoints.Length; i++ )
		{
			if( i != pointIndex && Vector3.Dot( direction, boundingBoxPoints[i] - point ) > 0 )
				return false;
		}

		return true;
	}

	// Credit: https://stackoverflow.com/a/32410473/2373034
	// Returns the intersection line of the 2 planes
	private static Ray GetPlanesIntersection( Plane p1, Plane p2 )
	{
		Vector3 p3Normal = Vector3.Cross( p1.normal, p2.normal );
		float det = p3Normal.sqrMagnitude;

		return new Ray( ( ( Vector3.Cross( p3Normal, p2.normal ) * p1.distance ) + ( Vector3.Cross( p1.normal, p3Normal ) * p2.distance ) ) / det, p3Normal );
	}

	// Credit: http://wiki.unity3d.com/index.php/3d_Math_functions
	// Returns the edge points of the closest line segment between 2 lines
	private static void FindClosestPointsOnTwoLines( Ray line1, Ray line2, out Vector3 closestPointLine1, out Vector3 closestPointLine2 )
	{
		Vector3 line1Direction = line1.direction;
		Vector3 line2Direction = line2.direction;

		float a = Vector3.Dot( line1Direction, line1Direction );
		float b = Vector3.Dot( line1Direction, line2Direction );
		float e = Vector3.Dot( line2Direction, line2Direction );

		float d = a * e - b * b;

		Vector3 r = line1.origin - line2.origin;
		float c = Vector3.Dot( line1Direction, r );
		float f = Vector3.Dot( line2Direction, r );

		float s = ( b * f - c * e ) / d;
		float t = ( a * f - c * b ) / d;

		closestPointLine1 = line1.origin + line1Direction * s;
		closestPointLine2 = line2.origin + line2Direction * t;
	}

	private static void SetupCamera()
	{
		if( m_previewRenderCamera )
		{
			cameraSetup.GetSetup( m_previewRenderCamera );

			renderCamera = m_previewRenderCamera;
			renderCamera.nearClipPlane = 0.01f;
			renderCamera.cullingMask = 1 << PREVIEW_LAYER;
		}
		else
			renderCamera = InternalCamera;

		renderCamera.backgroundColor = m_backgroundColor;
		renderCamera.orthographic = m_orthographicMode;
		renderCamera.clearFlags = m_backgroundColor.a < 1f ? CameraClearFlags.Depth : CameraClearFlags.Color;
	}

	private static bool IsStatic( Transform obj )
	{
		if( obj.gameObject.isStatic )
			return true;

		for( int i = 0; i < obj.childCount; i++ )
		{
			if( IsStatic( obj.GetChild( i ) ) )
				return true;
		}

		return false;
	}

	private static void SetLayerRecursively( Transform obj )
	{
		obj.gameObject.layer = PREVIEW_LAYER;
		for( int i = 0; i < obj.childCount; i++ )
			SetLayerRecursively( obj.GetChild( i ) );
	}

	private static void GetLayerRecursively( Transform obj )
	{
		layersList.Add( obj.gameObject.layer );
		for( int i = 0; i < obj.childCount; i++ )
			GetLayerRecursively( obj.GetChild( i ) );
	}

	private static void SetLayerRecursively( Transform obj, ref int index )
	{
		obj.gameObject.layer = layersList[index++];
		for( int i = 0; i < obj.childCount; i++ )
			SetLayerRecursively( obj.GetChild( i ), ref index );
	}
}