using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;

public class ARVisibilityDebug : MonoBehaviour
{
    public GameObject target;   // Assign manifold or test cube manually

    void Start()
    {
        Debug.Log("========== AR VISIBILITY DEBUG START ==========");

        DebugCamera();
        DebugPipeline();
        DebugRenderer();
        DebugARBackground();
        DebugTarget();

        Debug.Log("========== AR VISIBILITY DEBUG END ==========");
    }

    void DebugCamera()
    {
        Debug.Log("\n--- CAMERA SETTINGS ---");
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("No Camera.main found!");
            return;
        }

        Debug.Log($"Camera Name: {cam.name}");
        Debug.Log($"Culling Mask: {cam.cullingMask}");
        Debug.Log($"Near/Far Clip: {cam.nearClipPlane} / {cam.farClipPlane}");

        if (cam.TryGetComponent(out UniversalAdditionalCameraData data))
        {
            Debug.Log($"Render Type: {data.renderType}");
            Debug.Log($"Post Processing: {data.renderPostProcessing}");
            Debug.Log($"Anti Aliasing: {data.antialiasing}");
            Debug.Log($"Render PostProcessing: {data.renderPostProcessing}");
        }
        else
        {
            Debug.LogWarning("Camera has NO UniversalAdditionalCameraData!");
        }
    }

    void DebugPipeline()
    {
        Debug.Log("\n--- URP PIPELINE SETTINGS ---");

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urp == null)
        {
            Debug.LogError("No URP pipeline found!");
            return;
        }

        Debug.Log($"URP Asset: {urp.name}");
        Debug.Log($"Opaque Texture: {urp.supportsCameraOpaqueTexture}");
        Debug.Log($"Depth Texture: {urp.supportsCameraDepthTexture}");
        Debug.Log($"MSAA: {urp.msaaSampleCount}");
        Debug.Log($"Shadow Distance: {urp.shadowDistance}");
    }

    void DebugRenderer()
    {
        Debug.Log("\n--- ACTIVE RENDERER DATA ---");

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;

        var renderer = urp.GetRenderer(0);

        if (renderer == null)
        {
            Debug.LogError("Active Renderer is NULL!");
            return;
        }

        Debug.Log($"Renderer Type: {renderer.GetType().Name}");
    }

    void DebugARBackground()
    {
        Debug.Log("\n--- AR BACKGROUND ---");

        Camera cam = Camera.main;
        if (cam == null) return;

        var bg = cam.GetComponent<ARCameraBackground>();
        Debug.Log(bg ? "ARCameraBackground FOUND" : "ARCameraBackground MISSING");
    }

    void DebugTarget()
    {
        Debug.Log("\n--- TARGET MESH & MATERIAL ---");

        if (!target)
        {
            target = GameObject.Find("ManifoldCoupling");
            if (!target)
            {
                Debug.LogError("Target not assigned and ManifoldCoupling not found.");
                return;
            }
        }

        Debug.Log($"Target: {target.name}");

        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogError("Target has NO MeshRenderers");
            return;
        }

        foreach (var r in renderers)
        {
            Debug.Log($"Renderer: {r.gameObject.name}");
            Debug.Log($"   Enabled: {r.enabled}");
            Debug.Log($"   ShadowCaster: {r.shadowCastingMode}");

            foreach (Material m in r.sharedMaterials)
            {
                if (m == null)
                {
                    Debug.Log("   Material: NULL");
                    continue;
                }

                Debug.Log($"   Material: {m.name}");
                Debug.Log($"      Shader: {m.shader.name}");

                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    Debug.Log($"      BaseColor: {c}  Alpha={c.a}");

                    if (c.a < 0.1f)
                        Debug.LogError("      MATERIAL ALPHA TOO LOW — OBJECT INVISIBLE.");
                }

                if (m.HasProperty("_Surface"))
                {
                    Debug.Log($"SurfaceType: {m.GetFloat("_Surface")}");
                }

                Debug.Log($"      RenderQueue: {m.renderQueue}");
                }
            }
        }
    }
