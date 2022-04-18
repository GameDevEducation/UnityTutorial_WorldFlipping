using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RealityManager : MonoBehaviour
{
    public enum EWorld
    {
        World1,
        World2
    }

    [SerializeField] CharacterMotor TrackedPlayer;
    [SerializeField] Camera TrackedPlayerCamera;

    [SerializeField] Transform ShadowPlayer;
    [SerializeField] Camera ShadowPlayerCamera;

    [SerializeField] Transform World1Anchor;
    [SerializeField] Transform World2Anchor;

    [SerializeField] float SafetyCheckHeightBuffer = 0.05f;
    [SerializeField] LayerMask SafetyCheckMask = ~0;
    [SerializeField] UnityEvent OnFailedToFlip = new UnityEvent();

    [field: SerializeField] public EWorld CurrentWorld { get; private set; } = EWorld.World1;

    public Vector3 CurrentAnchorPosition => CurrentWorld == EWorld.World1 ? World1Anchor.position : World2Anchor.position;
    public Vector3 OtherAnchorPosition => CurrentWorld == EWorld.World1 ? World2Anchor.position : World1Anchor.position;

    RenderTexture LookingGlassRT;
    Texture2D LookingGlassTexture;

    // Start is called before the first frame update
    void Start()
    {
        SetupRenderTexture();
    }

    // Update is called once per frame
    void Update()
    {
        SynchroniseShadow();
    }

    void SetupRenderTexture()
    {
        LookingGlassRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        ShadowPlayerCamera.targetTexture = LookingGlassRT;

        LookingGlassTexture = new Texture2D(LookingGlassRT.width, LookingGlassRT.height,
                                            LookingGlassRT.graphicsFormat,
                                            UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
    }

    void SynchroniseShadow()
    {
        // update the shadow player location
        ShadowPlayer.position = TrackedPlayer.transform.position - CurrentAnchorPosition + OtherAnchorPosition;
        ShadowPlayer.rotation = TrackedPlayer.transform.rotation;

        // update the shadow player camera location
        ShadowPlayerCamera.transform.position = TrackedPlayerCamera.transform.position - CurrentAnchorPosition + OtherAnchorPosition;
        ShadowPlayerCamera.transform.rotation = TrackedPlayerCamera.transform.rotation;

        // update the camera settings
        ShadowPlayerCamera.fieldOfView = TrackedPlayerCamera.fieldOfView;
        ShadowPlayerCamera.nearClipPlane = TrackedPlayerCamera.nearClipPlane;
        ShadowPlayerCamera.farClipPlane = TrackedPlayerCamera.farClipPlane;
        ShadowPlayerCamera.backgroundColor = TrackedPlayerCamera.backgroundColor;
        ShadowPlayerCamera.clearFlags = TrackedPlayerCamera.clearFlags;

        // update the looking glass texture
        Graphics.CopyTexture(LookingGlassRT, LookingGlassTexture);
        Shader.SetGlobalTexture("_LookingGlassTex", LookingGlassTexture);
    }

    public void FlipWorlds()
    {
        Vector3 startPosition = ShadowPlayer.transform.position +
                                TrackedPlayer.transform.up * (TrackedPlayer.CurrentRadius + SafetyCheckHeightBuffer);
        Vector3 endPosition = startPosition +
                                TrackedPlayer.transform.up * (TrackedPlayer.CurrentHeight - 2f * TrackedPlayer.CurrentRadius);

        // does the capsule overlap?
        if (Physics.CheckCapsule(startPosition, endPosition, TrackedPlayer.CurrentRadius, SafetyCheckMask))
        {
            OnFailedToFlip.Invoke();
            return;
        }

        TrackedPlayer.transform.position = ShadowPlayer.transform.position;
        CurrentWorld = CurrentWorld == EWorld.World1 ? EWorld.World2 : EWorld.World1;
        SynchroniseShadow();
    }
}
