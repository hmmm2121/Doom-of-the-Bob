using UnityEngine;
using UnityEngine.InputSystem;
using Sponge;

// Mouse look for the Rigidbody player rig. Yaws the body (orientation),
// pitches the camera, and composes wall-run roll from WallRide.
public class PlayerLook : MonoBehaviour
{
    public Transform playerBody;        // yawed left/right (the player root)
    public Transform cameraTransform;   // pitched up/down
    public float sensitivity = 0.08f;
    public float maxPitch = 85f;
    public float rollLerp = 8f;

    float _pitch;
    float _roll;
    WallRide _wallRide;

    void Start()
    {
        if (playerBody == null) playerBody = transform;
        _wallRide = playerBody.GetComponent<WallRide>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 look = mouse.delta.ReadValue() * sensitivity;
            playerBody.Rotate(0f, look.x, 0f);
            _pitch = Mathf.Clamp(_pitch - look.y, -maxPitch, maxPitch);
        }

        float targetRoll = _wallRide != null ? _wallRide.WallRunTilt : 0f;
        _roll = Mathf.Lerp(_roll, targetRoll, Time.deltaTime * rollLerp);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, _roll);

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
