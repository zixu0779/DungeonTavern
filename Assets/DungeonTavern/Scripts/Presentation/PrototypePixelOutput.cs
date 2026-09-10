using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [RequireComponent(typeof(Camera))]
    public sealed class PrototypePixelOutput : MonoBehaviour
    {
        [SerializeField, Min(64)] private int referenceWidth = 640;
        [SerializeField, Min(64)] private int referenceHeight = 360;

        private Camera targetCamera;
        private RenderTexture pixelTarget;
        private GameObject displayCameraObject;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            pixelTarget = new RenderTexture(referenceWidth, referenceHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "Rotation25D_PixelTarget",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            pixelTarget.Create();
            targetCamera.targetTexture = pixelTarget;
            CreateDisplayCamera();
            DungeonTavern.UI.TavernUI.Instance?.SetWorldTexture(pixelTarget);
        }

        private void OnDisable()
        {
            DungeonTavern.UI.TavernUI.Instance?.SetWorldTexture(null);
            if (targetCamera != null)
                targetCamera.targetTexture = null;

            if (displayCameraObject != null)
            {
                Destroy(displayCameraObject);
                displayCameraObject = null;
            }

            if (pixelTarget == null)
                return;

            pixelTarget.Release();
            Destroy(pixelTarget);
            pixelTarget = null;
        }



        private void CreateDisplayCamera()
        {
            displayCameraObject = new GameObject("PixelOutput_DisplayCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            displayCameraObject.transform.SetParent(transform, false);

            Camera displayCamera = displayCameraObject.AddComponent<Camera>();
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = Color.black;
            displayCamera.cullingMask = 0;
            displayCamera.depth = targetCamera.depth + 1f;
            displayCamera.targetDisplay = targetCamera.targetDisplay;
            displayCamera.orthographic = true;
            displayCamera.allowHDR = false;
            displayCamera.allowMSAA = false;
        }
    }
}
