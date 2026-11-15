using UnityEngine;

// Used only by Medusa. With the Face ID path, FrontCameraTexture may be null.
public interface ICameraModeSwitcher
{
    void EnterFrontCameraMode();   // pause rear AR, start the front camera HIDDEN (feed never displayed)
    void ExitFrontCameraMode();    // stop the front camera, resume rear AR
    WebCamTexture FrontCameraTexture { get; }
}
