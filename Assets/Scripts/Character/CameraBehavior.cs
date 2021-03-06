﻿using UnityEngine;

public class CameraBehavior : MonoBehaviour
{
    [SerializeField] public float sensitivityX = 10f;
    [SerializeField] public float sensitivityY = 10f;
    [SerializeField] public float minimumX = -85f;
    [SerializeField] public float maximumX = 85f;

    [SerializeField] public float interactionDistance = 6f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        UpdateCursor();
        UpdateCamera();
        UpdateInteraction();
    }

    protected void UpdateCursor()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            // Debug stuff
            if (Input.GetMouseButton(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    protected void UpdateCamera()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Transform camera = transform.Find("MainCamera");

        float rotationX = Input.GetAxis("Mouse X") * sensitivityX;
        float rotationY = Input.GetAxis("Mouse Y") * sensitivityY;

        Vector3 cameraRotation = camera.localRotation.eulerAngles;
        Vector3 playerRotation = transform.localRotation.eulerAngles;

        playerRotation.y += rotationX;

        if (cameraRotation.x > 180)
        {
            cameraRotation.x = Mathf.Max(cameraRotation.x - rotationY, 360F + minimumX);
        }
        else
        {
            cameraRotation.x = Mathf.Min(cameraRotation.x - rotationY, maximumX);
        }

        playerRotation.z = 0;

        camera.localRotation = Quaternion.Euler(cameraRotation);
        transform.localRotation = Quaternion.Euler(playerRotation);
    }

    protected void UpdateInteraction()
    {
        Transform camera = transform.Find("MainCamera");

        if (Input.GetMouseButton(0))
        {
            int layerMask = 1 << 9;
            layerMask = ~layerMask;
            RaycastHit hit;
            if (Physics.Raycast(camera.position, camera.TransformDirection(Vector3.forward), out hit, interactionDistance, layerMask))
            {
                if (hit.collider.tag.Equals("Voxel"))
                {
                    //Debug.Log("Clicked");

                    //hit.transform.parent.GetComponent<Chunk>().needsUpdate = true;
                }
            }
        }
    }
}