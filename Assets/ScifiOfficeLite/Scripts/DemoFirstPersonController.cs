using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScifiOfficeLite {
    public class DemoFirstPersonController : MonoBehaviour {

        Rigidbody rb;
        CapsuleCollider col;
        bool isCrouching;

        public Transform playerBody;

        [Header("Movement")]
        public float speed = 3f;
        public float accelerationRate = 12f, crouchFactor = 0.5f, decelerationFactor = 1f;
        public float mouseSensitivity = 50f;

        float xRot = 0f;

        private void Start() {
            rb = playerBody.GetComponent<Rigidbody>();
            col = playerBody.GetComponent<CapsuleCollider>();

            Cursor.lockState = CursorLockMode.Locked;
        }

        // Update is called once per frame
        void Update() {
            Look();
            Walk();
            Crouch();
        }

        void Look() {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRot -= mouseY;
            xRot = Mathf.Clamp(xRot, -90f, 90f);

            transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }

        void Walk() {

            float maxSpeed = speed, maxAcc = accelerationRate;

            // Lower the limits if we are crouching.
            if (isCrouching) {
                maxSpeed *= crouchFactor;
                maxAcc *= crouchFactor;
            }

            Vector3 displacement = playerBody.transform.forward * Input.GetAxis("Vertical") + playerBody.transform.right * Input.GetAxis("Horizontal");
            float len = displacement.magnitude;
            if(len > 0) {
                rb.velocity += displacement / len * Time.deltaTime * maxAcc;

                // Clamp velocity to the maximum speed.
                if(rb.velocity.magnitude > maxSpeed) {
                    rb.velocity = rb.velocity.normalized * speed;
                }
            } else {
                // If no buttons are pressed, decelerate.
                len = rb.velocity.magnitude;
                float decelRate = accelerationRate * decelerationFactor * Time.deltaTime;
                if(len < decelRate) rb.velocity = Vector3.zero;
                else {
                    rb.velocity -= rb.velocity.normalized * decelRate;
                }
            }
        }

        void Crouch() {
            if (Input.GetKey(KeyCode.LeftControl)) {
                col.height = .5f;
                isCrouching = true; 
            } else {
                col.height = 2;
                if (Input.GetKey(KeyCode.LeftShift)) {
                    isCrouching = true;
                    return;
                }
                isCrouching = false;
            }
        }
    }
}
